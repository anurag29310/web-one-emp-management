using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tasks.Handlers
{
    public class RejectTaskCommandHandler : IRequestHandler<RejectTaskCommand>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RejectTaskCommandHandler> _logger;

        public RejectTaskCommandHandler(ITaskRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<RejectTaskCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RejectTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Task {request.Id} not found.");

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != task.AssignedEmployeeId)
                    throw new UnauthorizedAccessException("You can only reject tasks assigned to you.");
            }

            if (task.Status != TaskItemStatus.Assigned)
                throw new InvalidOperationException($"Task {task.TaskNumber} must be in Assigned status to reject (currently {task.Status}).");

            task.Status = TaskItemStatus.Rejected;
            if (!string.IsNullOrWhiteSpace(request.Reason))
                task.Notes = string.IsNullOrWhiteSpace(task.Notes) ? request.Reason : $"{task.Notes}\n{request.Reason}";
            task.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(task, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Task {TaskId} rejected", task.Id);

            await _auditLogger.LogAsync("Task", task.Id, "Rejected", newValues: new { request.Reason }, ct: cancellationToken);
        }
    }
}
