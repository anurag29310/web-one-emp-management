using MediatR;
using System;

namespace EMS.Application.Features.Employees.Queries
{
    public class GetEmployeesByTeamQuery : IRequest<System.Collections.Generic.IEnumerable<EMS.Domain.Entities.Employee>>
    {
        public Guid TeamId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
