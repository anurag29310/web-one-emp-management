using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class UpdateGoalProgressCommandHandler : IRequestHandler<UpdateGoalProgressCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateGoalProgressCommandHandler> _logger;

        public UpdateGoalProgressCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo, IAuditLogger auditLogger, ILogger<UpdateGoalProgressCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(UpdateGoalProgressCommand request, CancellationToken cancellationToken)
        {
            var goal = await _repo.GetGoalByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Goal {request.Id} not found.");

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                var isOwner = requesterEmployeeId.HasValue && requesterEmployeeId.Value == goal.EmployeeId;
                var isManagerOfOwner = await PerformanceScopeHelper.IsManagerOfAsync(_employeeRepo, requesterEmployeeId, goal.EmployeeId, request.IsManager, cancellationToken);
                if (!isOwner && !isManagerOfOwner)
                    throw new UnauthorizedAccessException("You can only update progress on your own goals.");
            }

            goal.ProgressPercent = request.ProgressPercent;
            goal.UpdatedAtUtc = DateTime.UtcNow;
            goal.UpdatedBy = request.RequestingUserId;

            await _repo.UpdateGoalAsync(goal, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Goal {GoalId} progress updated to {ProgressPercent}%", goal.Id, goal.ProgressPercent);

            await _auditLogger.LogAsync("PerformanceGoal", goal.Id, "ProgressUpdated", newValues: new { goal.ProgressPercent }, ct: cancellationToken);
        }
    }
}
