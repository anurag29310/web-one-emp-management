using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class ProcessPayrollCommandHandler : IRequestHandler<Commands.ProcessPayrollCommand, Guid>
    {
        private readonly IPayrollRepository _repo;
        private readonly IPdfService _pdf;
        private readonly IFileStorageService _storage;
        private readonly ILogger<ProcessPayrollCommandHandler> _logger;

        public ProcessPayrollCommandHandler(IPayrollRepository repo, IPdfService pdf, IFileStorageService storage, ILogger<ProcessPayrollCommandHandler> logger)
        {
            _repo = repo;
            _pdf = pdf;
            _storage = storage;
            _logger = logger;
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

            var employees = await _repo.GetAllEmployeesAsync();
            foreach (var emp in employees)
            {
                var structure = await _repo.GetEffectiveSalaryStructureAsync(emp.Id, request.PeriodStart);
                if (structure == null) continue;

                var totalAllow = structure.Allowances?.Sum(a => a.Amount) ?? 0m;
                var totalDeduct = structure.Deductions?.Sum(d => d.Amount) ?? 0m;
                var gross = structure.BasicSalary + totalAllow;
                var net = gross - totalDeduct;

                var payslip = new Payslip
                {
                    Id = Guid.NewGuid(),
                    PayrollRunId = run.Id,
                    EmployeeId = emp.Id,
                    Basic = structure.BasicSalary,
                    TotalAllowances = totalAllow,
                    TotalDeductions = totalDeduct,
                    GrossPay = gross,
                    NetPay = net,
                    GeneratedAtUtc = DateTime.UtcNow
                };

                // persist payslip
                await _repo.SavePayslipAsync(payslip);

                // generate PDF and store
                try
                {
                    var pdfBytes = await _pdf.GeneratePayslipPdfAsync(payslip);
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

            return run.Id;
        }
    }
}
