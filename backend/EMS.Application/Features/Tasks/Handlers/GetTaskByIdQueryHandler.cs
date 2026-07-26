using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tasks.Handlers
{
    public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskItem?>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuthRepository _authRepo;

        public GetTaskByIdQueryHandler(ITaskRepository repo, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        public async Task<TaskItem?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id, cancellationToken);
            if (task == null) return null;

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != task.AssignedEmployeeId)
                    return null; // hide existence from callers who aren't the assignee, matching a 404 rather than a 403
            }

            return task;
        }
    }
}
