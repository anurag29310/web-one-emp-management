using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class CreateGoalCommandHandler : IRequestHandler<CreateGoalCommand, Guid>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CreateGoalCommandHandler> _logger;

        public CreateGoalCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo, IAuditLogger auditLogger, ILogger<CreateGoalCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateGoalCommand request, CancellationToken cancellationToken)
        {
            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (!await PerformanceScopeHelper.IsManagerOfAsync(_employeeRepo, requesterEmployeeId, request.EmployeeId, request.IsManager, cancellationToken))
                    throw new UnauthorizedAccessException("You can only create goals for your own direct reports.");
            }

            var id = Guid.NewGuid();
            var goal = new PerformanceGoal
            {
                Id = id,
                GoalNumber = $"GOL-{id:N}".Substring(0, 12).ToUpperInvariant(),
                EmployeeId = request.EmployeeId,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                StartDate = request.StartDate,
                TargetDate = request.TargetDate,
                Weight = request.Weight,
                Status = GoalStatus.NotStarted,
                ProgressPercent = 0,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = request.RequestingUserId,
                IsDeleted = false
            };

            await _repo.AddGoalAsync(goal, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created performance goal {GoalNumber} ({GoalId}) for employee {EmployeeId}", goal.GoalNumber, goal.Id, goal.EmployeeId);

            await _auditLogger.LogAsync("PerformanceGoal", goal.Id, "Created", ct: cancellationToken);

            return goal.Id;
        }
    }
}
