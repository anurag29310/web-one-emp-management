using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Reimbursements
{
    public class UpdateReimbursementCommand : IRequest<Reimbursement>
    {
        public Guid Id { get; set; }
        public string ExpenseTitle { get; set; } = null!;
        public string ExpenseCategory { get; set; } = null!;
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string? Description { get; set; }
        public string? Notes { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }
    }
}
