using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Reimbursements
{
    /// <summary>Always creates a Draft — "Save reimbursement as Draft" is the only create outcome; use SubmitReimbursementCommand to move it forward.</summary>
    public class CreateReimbursementCommand : IRequest<Reimbursement>
    {
        public string ExpenseTitle { get; set; } = null!;
        public string ExpenseCategory { get; set; } = null!;
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string? Description { get; set; }
        public string? Notes { get; set; }

        /// <summary>Set by the controller from the caller's identity — the employee is always the caller, never client-supplied.</summary>
        public Guid RequestingUserId { get; set; }
    }
}
