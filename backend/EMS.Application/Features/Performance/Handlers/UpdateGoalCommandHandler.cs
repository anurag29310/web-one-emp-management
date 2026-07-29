using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class UpdateGoalCommandHandler : IRequestHandler<UpdateGoalCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateGoalCommandHandler> _logger;

        public UpdateGoalCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo, IAuditLogger auditLogger, ILogger<UpdateGoalCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(UpdateGoalCommand request, CancellationToken cancellationToken)
        {
            var goal = await _repo.GetGoalByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Goal {request.Id} not found.");

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (!await PerformanceScopeHelper.IsManagerOfAsync(_employeeRepo, requesterEmployeeId, goal.EmployeeId, request.IsManager, cancellationToken))
                    throw new UnauthorizedAccessException("You can only update goals for your own direct reports.");
            }

            goal.Title = request.Title;
            goal.Description = request.Description;
            goal.Category = request.Category;
            goal.TargetDate = request.TargetDate;
            goal.Weight = request.Weight;
            goal.Status = request.Status;
            goal.UpdatedAtUtc = DateTime.UtcNow;
            goal.UpdatedBy = request.RequestingUserId;

            await _repo.UpdateGoalAsync(goal, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated performance goal {GoalId}", goal.Id);

            await _auditLogger.LogAsync("PerformanceGoal", goal.Id, "Updated", ct: cancellationToken);
        }
    }
}
