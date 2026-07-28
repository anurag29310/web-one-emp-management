using EMS.Application.Features.Attendance.DTOs;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class ProcessPayrollCommandHandler : IRequestHandler<Commands.ProcessPayrollCommand, Guid>
    {
        private readonly IPayrollRepository _repo;
        private readonly IReimbursementRepository _reimbursementRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IPdfService _pdf;
        private readonly IFileStorageService _storage;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<ProcessPayrollCommandHandler> _logger;
        private readonly decimal _standardMonthlyHours;
        private readonly decimal _overtimeMultiplier;
        private readonly int _defaultDailyShiftMinutes;

        public ProcessPayrollCommandHandler(IPayrollRepository repo, IReimbursementRepository reimbursementRepo, IAttendanceRepository attendanceRepo, IPdfService pdf, IFileStorageService storage, IAuditLogger auditLogger, IConfiguration config, ILogger<ProcessPayrollCommandHandler> logger)
        {
            _repo = repo;
            _reimbursementRepo = reimbursementRepo;
            _attendanceRepo = attendanceRepo;
            _pdf = pdf;
            _storage = storage;
            _auditLogger = auditLogger;
            _logger = logger;
            _standardMonthlyHours = decimal.TryParse(config["Payroll:StandardMonthlyHours"], out var hours) ? hours : 208m;
            _overtimeMultiplier = decimal.TryParse(config["Payroll:OvertimeMultiplier"], out var multiplier) ? multiplier : 1.5m;
            _defaultDailyShiftMinutes = int.TryParse(config["Payroll:DefaultDailyShiftMinutes"], out var minutes) ? minutes : 480;
        }

        // Auto-calculates overtime for one employee's period from Attendance vs. their assigned
        // shift for each day worked; a per-employee Adjustments.OvertimeAmount override always wins
        // over this (see ProcessPayrollCommand.Adjustments — there's no attendance-independent way
        // to know when the auto-calculation is wrong, so Payroll always allows overriding it).
        private async Task<(decimal Amount, decimal Hours)> CalculateOvertimeAsync(Guid employeeId, decimal basicSalary, DateTime periodStart, DateTime periodEnd, CancellationToken ct)
        {
            var records = await _attendanceRepo.GetAllRecordsAsync(new AttendanceRecordFilter
            {
                EmployeeId = employeeId,
                DateFrom = periodStart,
                DateTo = periodEnd
            }, ct);

            var overtimeMinutes = 0;
            foreach (var record in records)
            {
                if (!record.TotalWorkMinutes.HasValue) continue;
                var shift = record.ShiftId.HasValue ? await _attendanceRepo.GetShiftByIdAsync(record.ShiftId.Value, ct) : null;
                var standardMinutes = OvertimeCalculator.StandardDailyMinutes(shift, _defaultDailyShiftMinutes);
                overtimeMinutes += OvertimeCalculator.OvertimeMinutesForDay(record.TotalWorkMinutes, standardMinutes);
            }

            var overtimeHours = Math.Round(overtimeMinutes / 60m, 2, MidpointRounding.AwayFromZero);
            var hourlyRate = OvertimeCalculator.HourlyRate(basicSalary, _standardMonthlyHours);
            var amount = OvertimeCalculator.OvertimeAmount(overtimeHours, hourlyRate, _overtimeMultiplier);
            return (amount, overtimeHours);
        }

        public async Task<Guid> Handle(Commands.ProcessPayrollCommand request, CancellationToken cancellationToken)
        {
            var run = new PayrollRun
            {
                Id = Guid.NewGuid(),
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                ProcessedAtUtc = DateTime.UtcNow,
                ProcessedBy = request.ProcessedBy,
                Status = "Processing"
            };

            await _repo.CreatePayrollRunAsync(run);

            _logger.LogInformation(
                "Payroll run {PayrollRunId} started for period {PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd} by user {ProcessedBy}.",
                run.Id, request.PeriodStart, request.PeriodEnd, request.ProcessedBy);

            var employees = await _repo.GetAllEmployeesAsync();
            foreach (var emp in employees)
            {
                var structure = await _repo.GetEffectiveSalaryStructureAsync(emp.Id, request.PeriodStart);
                if (structure == null) continue;

                var totalAllow = structure.Allowances?.Sum(a => a.Amount) ?? 0m;
                var totalDeduct = structure.Deductions?.Sum(d => d.Amount) ?? 0m;

                // Bonus is manual-only (discretionary — there's no basis to auto-calculate it).
                // Overtime auto-calculates from Attendance by default, but an Adjustments override
                // for this employee always wins over the auto-calculation.
                var adjustment = request.Adjustments?.FirstOrDefault(a => a.EmployeeId == emp.Id);
                var totalBonus = adjustment?.BonusAmount ?? 0m;
                decimal totalOvertime;
                decimal overtimeHours;
                if (adjustment?.OvertimeAmount.HasValue == true)
                {
                    totalOvertime = adjustment.OvertimeAmount.Value;
                    overtimeHours = 0m;
                }
                else
                {
                    (totalOvertime, overtimeHours) = await CalculateOvertimeAsync(emp.Id, structure.BasicSalary, request.PeriodStart, request.PeriodEnd, cancellationToken);
                }

                var gross = structure.BasicSalary + totalAllow + totalBonus + totalOvertime;

                // Approved, not-yet-processed reimbursements are added to NetPay (not GrossPay —
                // they're expense repayments, not taxable earnings) and then stamped Paid so a
                // later run can never pick them up again. See requirements.md's Payroll Integration
                // rule: "Payroll can process approved reimbursement only once."
                var approvedReimbursements = (await _reimbursementRepo.GetApprovedUnprocessedByEmployeeAsync(emp.Id, cancellationToken)).ToList();
                var totalReimbursements = approvedReimbursements.Sum(r => r.Amount);
                var net = gross - totalDeduct + totalReimbursements;

                var payslip = new Payslip
                {
                    Id = Guid.NewGuid(),
                    PayrollRunId = run.Id,
                    EmployeeId = emp.Id,
                    Basic = structure.BasicSalary,
                    TotalAllowances = totalAllow,
                    TotalDeductions = totalDeduct,
                    TotalReimbursements = totalReimbursements,
                    TotalBonus = totalBonus,
                    TotalOvertime = totalOvertime,
                    OvertimeHours = overtimeHours,
                    GrossPay = gross,
                    NetPay = net,
                    GeneratedAtUtc = DateTime.UtcNow
                };

                // persist payslip
                await _repo.SavePayslipAsync(payslip);

                foreach (var reimbursement in approvedReimbursements)
                {
                    reimbursement.Status = EMS.Domain.Enums.ReimbursementStatus.Paid;
                    reimbursement.PayrollProcessed = true;
                    reimbursement.PayrollRunId = run.Id;
                    reimbursement.PayrollDate = payslip.GeneratedAtUtc;
                    reimbursement.UpdatedAtUtc = DateTime.UtcNow;
                    await _reimbursementRepo.UpdateAsync(reimbursement, cancellationToken);
                }

                // generate PDF and store
                try
                {
                    var document = new PayslipDocument
                    {
                        EmployeeName = $"{emp.FirstName} {emp.LastName}".Trim(),
                        EmployeeCode = emp.EmployeeCode,
                        Designation = emp.Designation?.Name,
                        Department = emp.Department?.Name,
                        PeriodStart = run.PeriodStart,
                        PeriodEnd = run.PeriodEnd,
                        GeneratedAtUtc = payslip.GeneratedAtUtc,
                        Basic = payslip.Basic,
                        Allowances = (structure.Allowances ?? new List<Allowance>())
                            .Select(a => new PayslipLineItem { Name = a.Name, Amount = a.Amount })
                            .ToList(),
                        Deductions = (structure.Deductions ?? new List<Deduction>())
                            .Select(d => new PayslipLineItem { Name = d.Name, Amount = d.Amount })
                            .ToList(),
                        Reimbursements = approvedReimbursements
                            .Select(r => new PayslipLineItem { Name = r.ExpenseTitle, Amount = r.Amount })
                            .ToList(),
                        TotalBonus = payslip.TotalBonus,
                        TotalOvertime = payslip.TotalOvertime,
                        OvertimeHours = payslip.OvertimeHours,
                        GrossPay = payslip.GrossPay,
                        NetPay = payslip.NetPay
                    };

                    var pdfBytes = await _pdf.GeneratePayslipPdfAsync(document);
                    var container = "payslips";
                    var path = $"{run.Id}/{payslip.Id}.pdf";
                    await _storage.SaveFileAsync(container, path, pdfBytes, "application/pdf");
                    payslip.BlobContainer = container;
                    payslip.BlobPath = path;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate payslip for {EmployeeId}", emp.Id);
                }
            }

            run.Status = "Completed";
            await _repo.SaveChangesAsync();

            _logger.LogInformation("Payroll run {PayrollRunId} completed for {EmployeeCount} employees.", run.Id, employees.Count());

            await _auditLogger.LogAsync("PayrollRun", run.Id, "Processed", newValues: new
            {
                request.PeriodStart,
                request.PeriodEnd,
                request.ProcessedBy,
                EmployeeCount = employees.Count()
            }, ct: cancellationToken);

            return run.Id;
        }
    }
}
