using EMS.Application.Common.DTOs;
using EMS.Application.Features.Exports.Queries;
using EMS.Application.Features.Reimbursements.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Exports.Handlers
{
    public class ExportReimbursementsQueryHandler : IRequestHandler<ExportReimbursementsQuery, ExportFileResult>
    {
        private static readonly string[] Headers =
        {
            "Reimbursement Number", "Employee Code", "Employee Name", "Expense Title", "Expense Category",
            "Expense Date", "Amount", "Currency", "Status", "Submitted At", "Approved At",
            "Payroll Processed", "Payroll Date"
        };

        private readonly IReimbursementRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IExcelExportService _excelService;

        public ExportReimbursementsQueryHandler(IReimbursementRepository repo, IAuthRepository authRepo, IExcelExportService excelService)
        {
            _repo = repo;
            _authRepo = authRepo;
            _excelService = excelService;
        }

        public async Task<ExportFileResult> Handle(ExportReimbursementsQuery request, CancellationToken cancellationToken)
        {
            // Non-privileged callers are always scoped to their own reimbursements, regardless of
            // any employeeId filter supplied — same rule as the list endpoint.
            var employeeId = request.EmployeeId;
            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                employeeId = requester?.EmployeeId ?? Guid.Empty;
            }

            var reimbursements = await _repo.GetAllForExportAsync(employeeId, request.Status, cancellationToken);

            var rows = new List<IReadOnlyList<object?>>();
            foreach (var r in reimbursements)
            {
                var dto = ReimbursementDto.FromEntity(r);
                rows.Add(new List<object?>
                {
                    dto.ReimbursementNumber, r.Employee?.EmployeeCode, dto.EmployeeName, dto.ExpenseTitle, dto.ExpenseCategory,
                    dto.ExpenseDate, dto.Amount, dto.Currency, dto.Status, dto.SubmittedAtUtc, dto.ApprovedAtUtc,
                    dto.PayrollProcessed, dto.PayrollDate
                });
            }

            var bytes = await _excelService.GenerateAsync("Reimbursements", Headers, rows);
            var fileName = $"reimbursements_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

            return new ExportFileResult
            {
                Content = bytes,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = fileName
            };
        }
    }
}
