using MediatR;
using System;

namespace EMS.Application.Features.Payroll.Commands
{
    public class ApprovePayrollRunCommand : IRequest
    {
        public Guid PayrollRunId { get; set; }
        public Guid ApprovedBy { get; set; }
    }
}
