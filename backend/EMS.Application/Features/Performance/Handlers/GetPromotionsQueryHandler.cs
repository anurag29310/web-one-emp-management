using EMS.Application.Common.DTOs;
using EMS.Application.Features.Performance.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class GetPromotionsQueryHandler : IRequestHandler<Queries.GetPromotionsQuery, PagedResult<PromotionDto>>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;

        public GetPromotionsQueryHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
        }

        public async Task<PagedResult<PromotionDto>> Handle(Queries.GetPromotionsQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var employeeId = request.EmployeeId;
            IEnumerable<Guid>? scope = null;

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (requesterEmployeeId == null)
                    return PagedResult<PromotionDto>.Create(Enumerable.Empty<PromotionDto>(), page, pageSize, 0);

                var scopeList = new List<Guid> { requesterEmployeeId.Value };
                if (request.IsManager)
                    scopeList.AddRange(await _employeeRepo.GetDirectReportIdsAsync(requesterEmployeeId.Value, cancellationToken));

                if (employeeId.HasValue)
                {
                    if (!scopeList.Contains(employeeId.Value))
                        throw new UnauthorizedAccessException("You can only view promotions for your own team.");
                }
                else
                {
                    scope = scopeList;
                }
            }

            var items = await _repo.GetPromotionsAsync(page, pageSize, employeeId, request.Status, scope, cancellationToken);
            var total = await _repo.CountPromotionsAsync(employeeId, request.Status, scope, cancellationToken);

            return PagedResult<PromotionDto>.Create(items.Select(PromotionDto.FromEntity), page, pageSize, total);
        }
    }
}
