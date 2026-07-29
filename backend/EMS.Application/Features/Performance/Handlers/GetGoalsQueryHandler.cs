using EMS.Application.Common.DTOs;
using EMS.Application.Features.Performance.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class GetGoalsQueryHandler : IRequestHandler<Queries.GetGoalsQuery, PagedResult<PerformanceGoalDto>>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;

        public GetGoalsQueryHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
        }

        public async Task<PagedResult<PerformanceGoalDto>> Handle(Queries.GetGoalsQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var employeeId = request.EmployeeId;
            IEnumerable<System.Guid>? scope = null;

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (requesterEmployeeId == null)
                    return PagedResult<PerformanceGoalDto>.Create(Enumerable.Empty<PerformanceGoalDto>(), page, pageSize, 0);

                var scopeList = new List<System.Guid> { requesterEmployeeId.Value };
                if (request.IsManager)
                    scopeList.AddRange(await _employeeRepo.GetDirectReportIdsAsync(requesterEmployeeId.Value, cancellationToken));

                if (employeeId.HasValue)
                {
                    if (!scopeList.Contains(employeeId.Value))
                        throw new System.UnauthorizedAccessException("You can only view goals for your own team.");
                }
                else
                {
                    scope = scopeList;
                }
            }

            var items = await _repo.GetGoalsAsync(page, pageSize, employeeId, request.Status, request.Category, scope, cancellationToken);
            var total = await _repo.CountGoalsAsync(employeeId, request.Status, request.Category, scope, cancellationToken);

            return PagedResult<PerformanceGoalDto>.Create(items.Select(PerformanceGoalDto.FromEntity), page, pageSize, total);
        }
    }
}
