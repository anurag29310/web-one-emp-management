using MediatR;
using System;

namespace EMS.Application.Features.Employees.Queries
{
    public class GetReportingEmployeesQuery : IRequest<System.Collections.Generic.IEnumerable<EMS.Domain.Entities.Employee>>
    {
        public Guid ManagerId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
