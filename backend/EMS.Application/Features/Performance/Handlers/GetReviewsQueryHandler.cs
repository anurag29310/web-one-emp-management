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
    public class GetReviewsQueryHandler : IRequestHandler<Queries.GetReviewsQuery, PagedResult<PerformanceReviewDto>>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;

        public GetReviewsQueryHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
        }

        public async Task<PagedResult<PerformanceReviewDto>> Handle(Queries.GetReviewsQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var employeeId = request.EmployeeId;
            var reviewerEmployeeId = request.ReviewerEmployeeId;
            IEnumerable<Guid>? scope = null;
            Guid? participant = null;

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (requesterEmployeeId == null)
                    return PagedResult<PerformanceReviewDto>.Create(Enumerable.Empty<PerformanceReviewDto>(), page, pageSize, 0);

                var scopeList = new List<Guid> { requesterEmployeeId.Value };
                if (request.IsManager)
                    scopeList.AddRange(await _employeeRepo.GetDirectReportIdsAsync(requesterEmployeeId.Value, cancellationToken));

                if (employeeId.HasValue && !scopeList.Contains(employeeId.Value))
                    throw new UnauthorizedAccessException("You can only view reviews for your own team.");
                if (reviewerEmployeeId.HasValue && reviewerEmployeeId.Value != requesterEmployeeId.Value)
                    throw new UnauthorizedAccessException("You can only view reviews where you are the reviewer.");

                if (!employeeId.HasValue && !reviewerEmployeeId.HasValue)
                {
                    scope = scopeList;
                    participant = requesterEmployeeId.Value;
                }
            }

            var items = await _repo.GetReviewsAsync(page, pageSize, employeeId, reviewerEmployeeId, request.Status, scope, participant, cancellationToken);
            var total = await _repo.CountReviewsAsync(employeeId, reviewerEmployeeId, request.Status, scope, participant, cancellationToken);

            return PagedResult<PerformanceReviewDto>.Create(items.Select(PerformanceReviewDto.FromEntity), page, pageSize, total);
        }
    }
}
