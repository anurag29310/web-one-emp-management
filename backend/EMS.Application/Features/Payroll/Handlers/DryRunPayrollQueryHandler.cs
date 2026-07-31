using EMS.Application.Features.Attendance.DTOs;
using EMS.Application.Features.Payroll.Queries;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class DryRunPayrollQueryHandler : IRequestHandler<DryRunPayrollQuery, IEnumerable<PayslipPreview>>
    {
        private readonly IPayrollRepository _repo;
        private readonly IReimbursementRepository _reimbursementRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly ICurrentUserService _currentUser;
        private readonly decimal _standardMonthlyHours;
        private readonly decimal _overtimeMultiplier;
        private readonly int _defaultDailyShiftMinutes;

        public DryRunPayrollQueryHandler(IPayrollRepository repo, IReimbursementRepository reimbursementRepo, IAttendanceRepository attendanceRepo, ICurrentUserService currentUser, IConfiguration config)
        {
            _repo = repo;
            _reimbursementRepo = reimbursementRepo;
            _attendanceRepo = attendanceRepo;
            _currentUser = currentUser;
            _standardMonthlyHours = decimal.TryParse(config["Payroll:StandardMonthlyHours"], out var hours) ? hours : 208m;
            _overtimeMultiplier = decimal.TryParse(config["Payroll:OvertimeMultiplier"], out var multiplier) ? multiplier : 1.5m;
            _defaultDailyShiftMinutes = int.TryParse(config["Payroll:DefaultDailyShiftMinutes"], out var minutes) ? minutes : 480;
        }

        // Mirrors ProcessPayrollCommandHandler.CalculateOvertimeAsync exactly, so a preview matches
        // what Process would actually produce.
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
                var shift = record.ShiftId.HasValue ? await _attendanceRepo.GetShiftByIdAsync(record.ShiftId.Value, _currentUser.CompanyId!.Value, ct) : null;
                var standardMinutes = OvertimeCalculator.StandardDailyMinutes(shift, _defaultDailyShiftMinutes);
                overtimeMinutes += OvertimeCalculator.OvertimeMinutesForDay(record.TotalWorkMinutes, standardMinutes);
            }

            var overtimeHours = Math.Round(overtimeMinutes / 60m, 2, MidpointRounding.AwayFromZero);
            var hourlyRate = OvertimeCalculator.HourlyRate(basicSalary, _standardMonthlyHours);
            var amount = OvertimeCalculator.OvertimeAmount(overtimeHours, hourlyRate, _overtimeMultiplier);
            return (amount, overtimeHours);
        }

        public async Task<IEnumerable<PayslipPreview>> Handle(DryRunPayrollQuery request, CancellationToken cancellationToken)
        {
            var employees = await _repo.GetAllEmployeesAsync();
            var previews = new List<PayslipPreview>();
            foreach (var emp in employees)
            {
                var structure = await _repo.GetEffectiveSalaryStructureAsync(emp.Id, request.PeriodStart);
                if (structure == null) continue;
                var totalAllow = structure.Allowances?.Sum(a => a.Amount) ?? 0m;
                var totalDeduct = structure.Deductions?.Sum(d => d.Amount) ?? 0m;

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
                var totalReimbursements = (await _reimbursementRepo.GetApprovedUnprocessedByEmployeeAsync(emp.Id, cancellationToken)).Sum(r => r.Amount);
                var net = gross - totalDeduct + totalReimbursements;
                previews.Add(new PayslipPreview
                {
                    EmployeeId = emp.Id,
                    Basic = structure.BasicSalary,
                    TotalAllowances = totalAllow,
                    TotalDeductions = totalDeduct,
                    TotalReimbursements = totalReimbursements,
                    TotalBonus = totalBonus,
                    TotalOvertime = totalOvertime,
                    OvertimeHours = overtimeHours,
                    GrossPay = gross,
                    NetPay = net
                });
            }
            return previews;
        }
    }
}
