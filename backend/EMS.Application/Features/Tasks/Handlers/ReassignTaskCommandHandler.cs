using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tasks.Handlers
{
    public class ReassignTaskCommandHandler : IRequestHandler<ReassignTaskCommand, TaskItem>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<ReassignTaskCommandHandler> _logger;

        public ReassignTaskCommandHandler(ITaskRepository repo, IAuditLogger auditLogger, ILogger<ReassignTaskCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<TaskItem> Handle(ReassignTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Task {request.Id} not found.");

            if (task.Status is TaskItemStatus.Completed or TaskItemStatus.Cancelled)
                throw new InvalidOperationException($"Task {task.TaskNumber} is {task.Status} and is read-only.");

            var previousEmployeeId = task.AssignedEmployeeId;
            task.AssignedEmployeeId = request.AssignedEmployeeId;
            // The new assignee hasn't accepted or started anything yet, regardless of where the
            // previous assignee had gotten to.
            task.Status = TaskItemStatus.Assigned;
            task.UpdatedAtUtc = DateTime.UtcNow;
            task.UpdatedBy = request.RequestingUserId;

            await _repo.UpdateAsync(task, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Reassigned task {TaskId} from employee {PreviousEmployeeId} to {NewEmployeeId}", task.Id, previousEmployeeId, request.AssignedEmployeeId);

            await _auditLogger.LogAsync("Task", task.Id, "Reassigned", oldValues: new { AssignedEmployeeId = previousEmployeeId }, newValues: new { task.AssignedEmployeeId }, ct: cancellationToken);

            return await _repo.GetByIdAsync(task.Id, cancellationToken) ?? task;
        }
    }
}
