using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class AddGoalKpiCommandHandler : IRequestHandler<AddGoalKpiCommand, Guid>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<AddGoalKpiCommandHandler> _logger;

        public AddGoalKpiCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo, IAuditLogger auditLogger, ILogger<AddGoalKpiCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Guid> Handle(AddGoalKpiCommand request, CancellationToken cancellationToken)
        {
            var goal = await _repo.GetGoalByIdAsync(request.GoalId, cancellationToken)
                ?? throw new InvalidOperationException($"Goal {request.GoalId} not found.");

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (!await PerformanceScopeHelper.IsManagerOfAsync(_employeeRepo, requesterEmployeeId, goal.EmployeeId, request.IsManager, cancellationToken))
                    throw new UnauthorizedAccessException("You can only add KPIs to goals for your own direct reports.");
            }

            var kpi = new PerformanceGoalKpi
            {
                Id = Guid.NewGuid(),
                GoalId = goal.Id,
                Name = request.Name,
                TargetValue = request.TargetValue,
                CurrentValue = 0,
                Unit = request.Unit,
                Notes = request.Notes,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repo.AddKpiAsync(kpi, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Added KPI {KpiId} to goal {GoalId}", kpi.Id, goal.Id);

            await _auditLogger.LogAsync("PerformanceGoal", goal.Id, "KpiAdded", newValues: new { kpi.Name, kpi.TargetValue }, ct: cancellationToken);

            return kpi.Id;
        }
    }
}
