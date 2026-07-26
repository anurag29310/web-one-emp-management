using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tasks.Handlers
{
    public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskItem>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CreateTaskCommandHandler> _logger;

        public CreateTaskCommandHandler(ITaskRepository repo, IAuditLogger auditLogger, ILogger<CreateTaskCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<TaskItem> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            var task = new TaskItem
            {
                Id = id,
                TaskNumber = $"TSK-{id:N}".Substring(0, 12).ToUpperInvariant(),
                Title = request.Title,
                Description = request.Description,
                ClientId = request.ClientId,
                AssignedEmployeeId = request.AssignedEmployeeId,
                AssignedByUserId = request.RequestingUserId,
                AssignedDate = DateTime.UtcNow,
                DueDate = request.DueDate,
                Priority = request.Priority,
                Status = EMS.Domain.Enums.TaskItemStatus.Assigned,
                Notes = request.Notes,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = request.RequestingUserId
            };

            await _repo.AddAsync(task, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created task {TaskNumber} ({TaskId}) assigned to employee {EmployeeId}", task.TaskNumber, task.Id, task.AssignedEmployeeId);

            await _auditLogger.LogAsync("Task", task.Id, "Created", ct: cancellationToken);

            return await _repo.GetByIdAsync(task.Id, cancellationToken) ?? task;
        }
    }
}
