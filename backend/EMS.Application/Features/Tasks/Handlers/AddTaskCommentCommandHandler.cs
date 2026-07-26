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
    public class AddTaskCommentCommandHandler : IRequestHandler<AddTaskCommentCommand, TaskComment>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly ILogger<AddTaskCommentCommandHandler> _logger;

        public AddTaskCommentCommandHandler(ITaskRepository repo, IAuthRepository authRepo, ILogger<AddTaskCommentCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _logger = logger;
        }

        public async Task<TaskComment> Handle(AddTaskCommentCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.TaskId, cancellationToken)
                ?? throw new InvalidOperationException($"Task {request.TaskId} not found.");

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != task.AssignedEmployeeId)
                    throw new UnauthorizedAccessException("You can only comment on tasks assigned to you.");
            }

            if (task.Status is TaskItemStatus.Completed or TaskItemStatus.Cancelled)
                throw new InvalidOperationException($"Task {task.TaskNumber} is {task.Status} and is read-only.");

            var comment = new TaskComment
            {
                Id = Guid.NewGuid(),
                TaskId = request.TaskId,
                AuthorUserId = request.RequestingUserId,
                Comment = request.Comment,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repo.AddCommentAsync(comment, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Added comment to task {TaskId}", task.Id);

            return comment;
        }
    }
}
