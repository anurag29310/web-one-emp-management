using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tasks.Handlers
{
    public class UpdateTaskProgressCommandHandler : IRequestHandler<UpdateTaskProgressCommand>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateTaskProgressCommandHandler> _logger;

        public UpdateTaskProgressCommandHandler(ITaskRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<UpdateTaskProgressCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(UpdateTaskProgressCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Task {request.Id} not found.");

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != task.AssignedEmployeeId)
                    throw new UnauthorizedAccessException("You can only update progress on tasks assigned to you.");
            }

            if (task.Status is not (TaskItemStatus.InProgress or TaskItemStatus.OnHold))
                throw new InvalidOperationException($"Task {task.TaskNumber} must be InProgress or OnHold to update progress (currently {task.Status}).");

            task.Status = request.Status;
            task.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(task, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Task {TaskId} progress updated to {Status}", task.Id, request.Status);

            await _auditLogger.LogAsync("Task", task.Id, "ProgressUpdated", newValues: new { task.Status }, ct: cancellationToken);
        }
    }
}
