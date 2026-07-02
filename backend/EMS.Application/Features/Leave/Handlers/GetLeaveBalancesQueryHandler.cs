using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class GetLeaveBalancesQueryHandler : IRequestHandler<Queries.GetLeaveBalancesQuery, IEnumerable<EMS.Domain.Entities.LeaveBalance>>
    {
        private readonly ILeaveRepository _repo;

        public GetLeaveBalancesQueryHandler(ILeaveRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<EMS.Domain.Entities.LeaveBalance>> Handle(Queries.GetLeaveBalancesQuery request, CancellationToken cancellationToken)
            => await _repo.GetLeaveBalancesForEmployeeAsync(request.EmployeeId, cancellationToken);
    }
}
