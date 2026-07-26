using EMS.Application.Features.Tasks.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tasks.Handlers
{
    public class GetTaskAttachmentsQueryHandler : IRequestHandler<GetTaskAttachmentsQuery, IEnumerable<TaskAttachmentDto>>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuthRepository _authRepo;

        public GetTaskAttachmentsQueryHandler(ITaskRepository repo, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        public async Task<IEnumerable<TaskAttachmentDto>> Handle(GetTaskAttachmentsQuery request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.TaskId, cancellationToken)
                ?? throw new InvalidOperationException($"Task {request.TaskId} not found.");

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != task.AssignedEmployeeId)
                    throw new UnauthorizedAccessException("You can only view attachments on tasks assigned to you.");
            }

            var attachments = await _repo.GetAttachmentsAsync(request.TaskId, cancellationToken);
            return attachments.Select(TaskAttachmentDto.FromEntity);
        }
    }
}
