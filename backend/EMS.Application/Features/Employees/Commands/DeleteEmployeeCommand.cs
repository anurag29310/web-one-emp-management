using MediatR;
using System;

namespace EMS.Application.Features.Employees.Commands
{
    public class DeleteEmployeeCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
