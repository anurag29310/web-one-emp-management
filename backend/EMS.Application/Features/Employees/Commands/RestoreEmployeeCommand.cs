using MediatR;
using System;

namespace EMS.Application.Features.Employees.Commands
{
    public class RestoreEmployeeCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
