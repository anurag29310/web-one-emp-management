using EMS.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Employees.Queries
{
    public class GetReportingHierarchyQuery : IRequest<IEnumerable<Employee>>
    {
        public Guid EmployeeId { get; set; }
    }
}
