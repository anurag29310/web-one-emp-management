using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class DeleteGoalCommandHandler : IRequestHandler<DeleteGoalCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<DeleteGoalCommandHandler> _logger;

        public DeleteGoalCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo, IAuditLogger auditLogger, ILogger<DeleteGoalCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(DeleteGoalCommand request, CancellationToken cancellationToken)
        {
            var goal = await _repo.GetGoalByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Goal {request.Id} not found.");

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (!await PerformanceScopeHelper.IsManagerOfAsync(_employeeRepo, requesterEmployeeId, goal.EmployeeId, request.IsManager, cancellationToken))
                    throw new UnauthorizedAccessException("You can only delete goals for your own direct reports.");
            }

            goal.DeletedAtUtc = DateTime.UtcNow;
            goal.DeletedBy = request.RequestingUserId;
            await _repo.DeleteGoalAsync(goal, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted performance goal {GoalId}", goal.Id);

            await _auditLogger.LogAsync("PerformanceGoal", goal.Id, "Deleted", ct: cancellationToken);
        }
    }
}
