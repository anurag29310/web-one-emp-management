using EMS.Application.Common.DTOs;
using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Exports.Queries
{
    /// <summary>Export reimbursements matching the same filters as <see cref="Reimbursements.GetReimbursementsQuery"/> to Excel.
    /// A non-Admin caller is always scoped to their own reimbursements, regardless of any employeeId filter supplied.</summary>
    public class ExportReimbursementsQuery : IRequest<ExportFileResult>
    {
        public Guid? EmployeeId { get; set; }
        public ReimbursementStatus? Status { get; set; }

        /// <summary>Set by the controller from the caller's identity, never bound from the query string.</summary>
        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }
}
