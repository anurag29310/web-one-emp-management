using EMS.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Employees.Queries
{
    public class GetDirectReportsQuery : IRequest<IEnumerable<Employee>>
    {
        public Guid ManagerId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
