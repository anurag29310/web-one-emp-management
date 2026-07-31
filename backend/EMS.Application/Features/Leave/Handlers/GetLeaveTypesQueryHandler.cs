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
        private readonly ICurrentUserService _currentUser;

        public GetLeaveTypesQueryHandler(ILeaveRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<LeaveType>> Handle(Queries.GetLeaveTypesQuery request, CancellationToken cancellationToken)
            => await _repo.GetLeaveTypesAsync(_currentUser.CompanyId!.Value, cancellationToken);
    }
}
