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

        public GetLeavesQueryHandler(ILeaveRepository repo) => _repo = repo;

        public async Task<PagedResult<LeaveRequestDto>> Handle(Queries.GetLeavesQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var items = await _repo.GetLeavesAsync(page, pageSize, request.EmployeeId, request.LeaveTypeId, request.Year, request.Status, cancellationToken);
            var total = await _repo.CountLeavesAsync(request.EmployeeId, request.LeaveTypeId, request.Year, request.Status, cancellationToken);

            return PagedResult<LeaveRequestDto>.Create(
                items.Select(LeaveRequestDto.FromEntity),
                page, pageSize, total);
        }
    }
}
