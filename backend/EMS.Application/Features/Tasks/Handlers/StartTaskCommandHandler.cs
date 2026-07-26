using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tasks.Handlers
{
    public class StartTaskCommandHandler : IRequestHandler<StartTaskCommand>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<StartTaskCommandHandler> _logger;

        public StartTaskCommandHandler(ITaskRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<StartTaskCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(StartTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Task {request.Id} not found.");

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != task.AssignedEmployeeId)
                    throw new UnauthorizedAccessException("You can only start tasks assigned to you.");
            }

            if (task.Status != TaskItemStatus.Accepted)
                throw new InvalidOperationException($"Task {task.TaskNumber} must be Accepted before it can be started (currently {task.Status}).");

            task.Status = TaskItemStatus.InProgress;
            task.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(task, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Task {TaskId} started", task.Id);

            await _auditLogger.LogAsync("Task", task.Id, "Started", ct: cancellationToken);
        }
    }
}
