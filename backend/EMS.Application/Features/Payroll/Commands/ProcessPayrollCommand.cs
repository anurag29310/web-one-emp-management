using MediatR;
using System;

namespace EMS.Application.Features.Payroll.Commands
{
    public class ProcessPayrollCommand : IRequest<Guid>
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public Guid ProcessedBy { get; set; }
    }
}
