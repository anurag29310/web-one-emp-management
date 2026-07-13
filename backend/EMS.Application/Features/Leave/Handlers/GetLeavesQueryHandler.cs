using EMS.Application.Common.DTOs;
using EMS.Application.Features.Leave.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class GetLeavesQueryHandler : IRequestHandler<Queries.GetLeavesQuery, PagedResult<LeaveRequestDto>>
    {
        private readonly ILeaveRepository _repo;
        private readonly IAuthRepository _authRepo;

        public GetLeavesQueryHandler(ILeaveRepository repo, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        public async Task<PagedResult<LeaveRequestDto>> Handle(Queries.GetLeavesQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var employeeId = request.EmployeeId;
            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null)
                    return PagedResult<LeaveRequestDto>.Create(Enumerable.Empty<LeaveRequestDto>(), page, pageSize, 0);

                // Non-privileged callers are always scoped to their own employee record,
                // regardless of any employeeId filter supplied on the query string.
                employeeId = requester.EmployeeId;
            }

            var items = await _repo.GetLeavesAsync(page, pageSize, employeeId, request.LeaveTypeId, request.Year, request.Status, cancellationToken);
            var total = await _repo.CountLeavesAsync(employeeId, request.LeaveTypeId, request.Year, request.Status, cancellationToken);

            return PagedResult<LeaveRequestDto>.Create(
                items.Select(LeaveRequestDto.FromEntity),
                page, pageSize, total);
        }
    }
}
