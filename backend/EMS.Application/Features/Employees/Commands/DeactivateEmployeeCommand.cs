using MediatR;
using System;

namespace EMS.Application.Features.Employees.Commands
{
    public class DeactivateEmployeeCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
