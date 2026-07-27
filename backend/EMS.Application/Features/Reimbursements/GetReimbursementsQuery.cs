using EMS.Application.Common.DTOs;
using EMS.Application.Features.Reimbursements.DTOs;
using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Reimbursements
{
    public class GetReimbursementsQuery : IRequest<PagedResult<ReimbursementDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? EmployeeId { get; set; }
        public ReimbursementStatus? Status { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin role and may see every reimbursement regardless of EmployeeId.</summary>
        public bool IsPrivileged { get; set; }
    }
}
