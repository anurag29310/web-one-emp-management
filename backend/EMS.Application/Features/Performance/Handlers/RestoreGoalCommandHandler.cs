using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class RestoreGoalCommandHandler : IRequestHandler<RestoreGoalCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RestoreGoalCommandHandler> _logger;

        public RestoreGoalCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo, IAuditLogger auditLogger, ILogger<RestoreGoalCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RestoreGoalCommand request, CancellationToken cancellationToken)
        {
            var goal = await _repo.GetGoalByIdIncludingDeletedAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Goal {request.Id} not found.");

            if (!goal.IsDeleted)
                throw new InvalidOperationException($"Goal {goal.GoalNumber} is not deleted.");

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (!await PerformanceScopeHelper.IsManagerOfAsync(_employeeRepo, requesterEmployeeId, goal.EmployeeId, request.IsManager, cancellationToken))
                    throw new UnauthorizedAccessException("You can only restore goals for your own direct reports.");
            }

            goal.DeletedAtUtc = null;
            goal.DeletedBy = null;
            goal.UpdatedAtUtc = DateTime.UtcNow;
            goal.UpdatedBy = request.RequestingUserId;

            await _repo.RestoreGoalAsync(goal, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Restored performance goal {GoalId}", goal.Id);

            await _auditLogger.LogAsync("PerformanceGoal", goal.Id, "Restored", ct: cancellationToken);
        }
    }
}
