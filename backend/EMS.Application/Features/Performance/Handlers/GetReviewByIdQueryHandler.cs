using EMS.Application.Features.Performance.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class GetReviewByIdQueryHandler : IRequestHandler<Queries.GetReviewByIdQuery, PerformanceReviewDto?>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;

        public GetReviewByIdQueryHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
        }

        public async Task<PerformanceReviewDto?> Handle(Queries.GetReviewByIdQuery request, CancellationToken cancellationToken)
        {
            var review = await _repo.GetReviewByIdAsync(request.Id, cancellationToken);
            if (review == null) return null;

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (requesterEmployeeId == null) return null;

                var isParticipant = review.EmployeeId == requesterEmployeeId.Value || review.ReviewerEmployeeId == requesterEmployeeId.Value;
                if (!isParticipant)
                {
                    if (!await PerformanceScopeHelper.IsManagerOfAsync(_employeeRepo, requesterEmployeeId, review.EmployeeId, request.IsManager, cancellationToken))
                        return null;
                }
            }

            return PerformanceReviewDto.FromEntity(review);
        }
    }
}
