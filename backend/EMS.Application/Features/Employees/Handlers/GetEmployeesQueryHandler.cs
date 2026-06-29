using EMS.Application.Common.DTOs;
using EMS.Application.Features.Employees.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class GetEmployeesQueryHandler : IRequestHandler<Queries.GetEmployeesQuery, PagedResult<EmployeeDto>>
    {
        private readonly IEmployeeRepository _repo;

        public GetEmployeesQueryHandler(IEmployeeRepository repo) => _repo = repo;

        public async Task<PagedResult<EmployeeDto>> Handle(Queries.GetEmployeesQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var items = await _repo.GetAllAsync(page, pageSize, request.Search, request.SortBy, request.SortDir, request.DepartmentId, request.Status, cancellationToken);
            var total = await _repo.CountAsync(request.Search, request.DepartmentId, request.Status, cancellationToken);

            return PagedResult<EmployeeDto>.Create(
                items.Select(EmployeeDto.FromEntity),
                page,
                pageSize,
                total);
        }
    }
}
