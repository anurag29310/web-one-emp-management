using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class GetLeaveTypeByIdQueryHandler : IRequestHandler<Queries.GetLeaveTypeByIdQuery, LeaveType?>
    {
        private readonly ILeaveRepository _repo;

        public GetLeaveTypeByIdQueryHandler(ILeaveRepository repo)
        {
            _repo = repo;
        }

        public async Task<LeaveType?> Handle(Queries.GetLeaveTypeByIdQuery request, CancellationToken cancellationToken)
            => await _repo.GetLeaveTypeByIdAsync(request.Id, cancellationToken);
    }
}
