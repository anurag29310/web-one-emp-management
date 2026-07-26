using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tasks.Handlers
{
    public class CompleteTaskCommandHandler : IRequestHandler<CompleteTaskCommand>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CompleteTaskCommandHandler> _logger;

        public CompleteTaskCommandHandler(ITaskRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<CompleteTaskCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Task {request.Id} not found.");

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != task.AssignedEmployeeId)
                    throw new UnauthorizedAccessException("You can only complete tasks assigned to you.");
            }

            if (task.Status is not (TaskItemStatus.InProgress or TaskItemStatus.OnHold))
                throw new InvalidOperationException($"Task {task.TaskNumber} must be InProgress or OnHold to complete (currently {task.Status}).");

            task.Status = TaskItemStatus.Completed;
            task.CompletedAtUtc = DateTime.UtcNow;
            task.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(task, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Task {TaskId} completed", task.Id);

            await _auditLogger.LogAsync("Task", task.Id, "Completed", ct: cancellationToken);
        }
    }
}
