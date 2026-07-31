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
        private readonly ICurrentUserService _currentUser;

        public GetEmployeesQueryHandler(IEmployeeRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<PagedResult<EmployeeDto>> Handle(Queries.GetEmployeesQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;
            var companyId = _currentUser.CompanyId!.Value;

            var items = await _repo.GetAllAsync(companyId, page, pageSize, request.Search, request.SortBy, request.SortDir, request.DepartmentId, request.Status, request.TeamId, request.DesignationId, request.OfficeLocationId, cancellationToken);
            var total = await _repo.CountAsync(companyId, request.Search, request.DepartmentId, request.Status, request.TeamId, request.DesignationId, request.OfficeLocationId, cancellationToken);

            return PagedResult<EmployeeDto>.Create(
                items.Select(EmployeeDto.FromEntity),
                page,
                pageSize,
                total);
        }
    }
}
