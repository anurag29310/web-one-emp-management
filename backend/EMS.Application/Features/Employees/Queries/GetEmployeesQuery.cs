using MediatR;

namespace EMS.Application.Features.Employees.Queries
{
    public class GetEmployeesQuery : IRequest<System.Collections.Generic.IEnumerable<EMS.Domain.Entities.Employee>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string? SortDir { get; set; }
        public System.Guid? DepartmentId { get; set; }
        public string? Status { get; set; }
    }
}
