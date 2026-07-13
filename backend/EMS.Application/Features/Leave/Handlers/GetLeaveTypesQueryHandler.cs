using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class GetLeaveTypesQueryHandler : IRequestHandler<Queries.GetLeaveTypesQuery, IEnumerable<LeaveType>>
    {
        private readonly ILeaveRepository _repo;

        public GetLeaveTypesQueryHandler(ILeaveRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<LeaveType>> Handle(Queries.GetLeaveTypesQuery request, CancellationToken cancellationToken)
            => await _repo.GetLeaveTypesAsync(cancellationToken);
    }
}
