using EMS.Application.Common.DTOs;
using EMS.Application.Features.Employees.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Employees.Queries
{
    public class GetEmployeesQuery : IRequest<PagedResult<EmployeeDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string? SortDir { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? Status { get; set; }
    }
}
