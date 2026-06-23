using MediatR;
using System;

namespace EMS.Application.Features.Payroll.Commands
{
    public class DeleteSalaryStructureCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
