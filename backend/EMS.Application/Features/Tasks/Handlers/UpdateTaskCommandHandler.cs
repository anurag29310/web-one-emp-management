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
    public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, TaskItem>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateTaskCommandHandler> _logger;

        public UpdateTaskCommandHandler(ITaskRepository repo, IAuditLogger auditLogger, ILogger<UpdateTaskCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<TaskItem> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Task {request.Id} not found.");

            if (task.Status is TaskItemStatus.Completed or TaskItemStatus.Cancelled)
                throw new InvalidOperationException($"Task {task.TaskNumber} is {task.Status} and is read-only.");

            task.Title = request.Title;
            task.Description = request.Description;
            task.ClientId = request.ClientId;
            task.DueDate = request.DueDate;
            task.Priority = request.Priority;
            task.Notes = request.Notes;
            task.UpdatedAtUtc = DateTime.UtcNow;
            task.UpdatedBy = request.RequestingUserId;

            await _repo.UpdateAsync(task, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated task {TaskId}", task.Id);

            await _auditLogger.LogAsync("Task", task.Id, "Updated", ct: cancellationToken);

            return task;
        }
    }
}
