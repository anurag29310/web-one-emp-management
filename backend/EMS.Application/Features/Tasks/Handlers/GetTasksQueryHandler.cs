using EMS.Application.Common.DTOs;
using EMS.Application.Features.Tasks.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tasks.Handlers
{
    public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, PagedResult<TaskItemDto>>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuthRepository _authRepo;

        public GetTasksQueryHandler(ITaskRepository repo, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        public async Task<PagedResult<TaskItemDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            // Non-privileged callers are always scoped to their own tasks, regardless of any
            // assignedEmployeeId filter supplied — matches the Manager scoping rule already
            // established for attendance/leave exports (api-specification.md §13.2).
            var assignedEmployeeId = request.AssignedEmployeeId;
            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                assignedEmployeeId = requester?.EmployeeId ?? System.Guid.Empty;
            }

            var items = await _repo.GetAllAsync(page, pageSize, assignedEmployeeId, request.ClientId, request.Status, request.Priority, cancellationToken);
            var total = await _repo.CountAsync(assignedEmployeeId, request.ClientId, request.Status, request.Priority, cancellationToken);

            return PagedResult<TaskItemDto>.Create(
                items.Select(TaskItemDto.FromEntity),
                page,
                pageSize,
                total);
        }
    }
}
