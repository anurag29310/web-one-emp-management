using EMS.Application.Features.Payroll.Dtos;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Payroll.Queries
{
    public class GetPayslipsForEmployeeQuery : IRequest<IEnumerable<PayslipDto>>
    {
        /// <summary>Target employee. Optional for non-privileged callers, who are always scoped to their own record regardless.</summary>
        public Guid? EmployeeId { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }
}
