using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Reimbursements.DTOs
{
    public class ReimbursementDto
    {
        public Guid Id { get; set; }
        public string ReimbursementNumber { get; set; } = null!;
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string ExpenseTitle { get; set; } = null!;
        public string ExpenseCategory { get; set; } = null!;
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string? Description { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? SubmittedAtUtc { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public Guid? ApprovedBy { get; set; }
        public string? ReviewRemarks { get; set; }
        public bool PayrollProcessed { get; set; }
        public Guid? PayrollRunId { get; set; }
        public DateTime? PayrollDate { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static ReimbursementDto FromEntity(Reimbursement r) => new()
        {
            Id = r.Id,
            ReimbursementNumber = r.ReimbursementNumber,
            EmployeeId = r.EmployeeId,
            EmployeeName = r.Employee == null ? null : $"{r.Employee.FirstName} {r.Employee.LastName}".Trim(),
            ExpenseTitle = r.ExpenseTitle,
            ExpenseCategory = r.ExpenseCategory,
            ExpenseDate = r.ExpenseDate,
            Amount = r.Amount,
            Currency = r.Currency,
            Description = r.Description,
            Notes = r.Notes,
            Status = r.Status.ToString(),
            SubmittedAtUtc = r.SubmittedAtUtc,
            ApprovedAtUtc = r.ApprovedAtUtc,
            ApprovedBy = r.ApprovedBy,
            ReviewRemarks = r.ReviewRemarks,
            PayrollProcessed = r.PayrollProcessed,
            PayrollRunId = r.PayrollRunId,
            PayrollDate = r.PayrollDate,
            CreatedAtUtc = r.CreatedAtUtc,
            UpdatedAtUtc = r.UpdatedAtUtc
        };
    }
}
