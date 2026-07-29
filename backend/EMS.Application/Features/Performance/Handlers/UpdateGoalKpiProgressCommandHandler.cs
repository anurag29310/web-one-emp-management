using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class UpdateGoalKpiProgressCommandHandler : IRequestHandler<UpdateGoalKpiProgressCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateGoalKpiProgressCommandHandler> _logger;

        public UpdateGoalKpiProgressCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo, IAuditLogger auditLogger, ILogger<UpdateGoalKpiProgressCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(UpdateGoalKpiProgressCommand request, CancellationToken cancellationToken)
        {
            var kpi = await _repo.GetKpiByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"KPI {request.Id} not found.");
            var goal = kpi.Goal
                ?? throw new InvalidOperationException($"Goal for KPI {request.Id} not found.");

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                var isOwner = requesterEmployeeId.HasValue && requesterEmployeeId.Value == goal.EmployeeId;
                var isManagerOfOwner = await PerformanceScopeHelper.IsManagerOfAsync(_employeeRepo, requesterEmployeeId, goal.EmployeeId, request.IsManager, cancellationToken);
                if (!isOwner && !isManagerOfOwner)
                    throw new UnauthorizedAccessException("You can only update KPI progress on your own goals.");
            }

            kpi.CurrentValue = request.CurrentValue;
            if (request.Notes != null) kpi.Notes = request.Notes;
            kpi.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateKpiAsync(kpi, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("KPI {KpiId} progress updated to {CurrentValue}", kpi.Id, kpi.CurrentValue);

            await _auditLogger.LogAsync("PerformanceGoal", goal.Id, "KpiProgressUpdated", newValues: new { kpi.Id, kpi.CurrentValue }, ct: cancellationToken);
        }
    }
}
