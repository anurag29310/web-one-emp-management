using MediatR;
using System;

namespace EMS.Application.Features.Employees.Queries
{
    public class GetEmployeeByIdQuery : IRequest<EMS.Domain.Entities.Employee?>
    {
        public Guid Id { get; set; }
    }
}
