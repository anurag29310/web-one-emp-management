using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class GetLeaveByIdQueryHandler : IRequestHandler<Queries.GetLeaveByIdQuery, EMS.Domain.Entities.LeaveRequest?>
    {
        private readonly ILeaveRepository _repo;

        public GetLeaveByIdQueryHandler(ILeaveRepository repo)
        {
            _repo = repo;
        }

        public async Task<EMS.Domain.Entities.LeaveRequest?> Handle(Queries.GetLeaveByIdQuery request, CancellationToken cancellationToken)
            => await _repo.GetLeaveByIdAsync(request.Id);
    }
}
