using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tasks.Handlers
{
    public class CancelTaskCommandHandler : IRequestHandler<CancelTaskCommand>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CancelTaskCommandHandler> _logger;

        public CancelTaskCommandHandler(ITaskRepository repo, IAuditLogger auditLogger, ILogger<CancelTaskCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(CancelTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Task {request.Id} not found.");

            if (task.Status == TaskItemStatus.Completed)
                throw new InvalidOperationException($"Task {task.TaskNumber} is already completed and cannot be cancelled.");

            task.Status = TaskItemStatus.Cancelled;
            task.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(task, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cancelled task {TaskId}", task.Id);

            await _auditLogger.LogAsync("Task", task.Id, "Cancelled", ct: cancellationToken);
        }
    }
}
