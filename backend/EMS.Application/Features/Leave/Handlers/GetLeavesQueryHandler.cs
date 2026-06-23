using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class GetLeavesQueryHandler : IRequestHandler<Queries.GetLeavesQuery, IEnumerable<EMS.Domain.Entities.LeaveRequest>>
    {
        private readonly ILeaveRepository _repo;

        public GetLeavesQueryHandler(ILeaveRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<EMS.Domain.Entities.LeaveRequest>> Handle(Queries.GetLeavesQuery request, CancellationToken cancellationToken)
            => await _repo.GetLeavesAsync(request.Page, request.PageSize, request.EmployeeId, request.LeaveTypeId, request.Year, request.Status);
    }
}
