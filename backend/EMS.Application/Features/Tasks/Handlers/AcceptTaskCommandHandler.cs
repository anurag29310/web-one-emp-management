using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tasks.Handlers
{
    public class AcceptTaskCommandHandler : IRequestHandler<AcceptTaskCommand>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<AcceptTaskCommandHandler> _logger;

        public AcceptTaskCommandHandler(ITaskRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<AcceptTaskCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(AcceptTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Task {request.Id} not found.");

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != task.AssignedEmployeeId)
                    throw new UnauthorizedAccessException("You can only accept tasks assigned to you.");
            }

            if (task.Status != TaskItemStatus.Assigned)
                throw new InvalidOperationException($"Task {task.TaskNumber} must be in Assigned status to accept (currently {task.Status}).");

            task.Status = TaskItemStatus.Accepted;
            task.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(task, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Task {TaskId} accepted", task.Id);

            await _auditLogger.LogAsync("Task", task.Id, "Accepted", ct: cancellationToken);
        }
    }
}
