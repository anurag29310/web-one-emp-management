using EMS.Application.Features.Performance.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class GetGoalByIdQueryHandler : IRequestHandler<Queries.GetGoalByIdQuery, PerformanceGoalDto?>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;

        public GetGoalByIdQueryHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
        }

        public async Task<PerformanceGoalDto?> Handle(Queries.GetGoalByIdQuery request, CancellationToken cancellationToken)
        {
            var goal = await _repo.GetGoalByIdAsync(request.Id, cancellationToken);
            if (goal == null) return null;

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (requesterEmployeeId == null) return null;

                if (goal.EmployeeId != requesterEmployeeId.Value)
                {
                    if (!await PerformanceScopeHelper.IsManagerOfAsync(_employeeRepo, requesterEmployeeId, goal.EmployeeId, request.IsManager, cancellationToken))
                        return null;
                }
            }

            return PerformanceGoalDto.FromEntity(goal);
        }
    }
}
