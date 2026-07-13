using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class GetLeaveByIdQueryHandler : IRequestHandler<Queries.GetLeaveByIdQuery, EMS.Domain.Entities.LeaveRequest?>
    {
        private readonly ILeaveRepository _repo;
        private readonly IAuthRepository _authRepo;

        public GetLeaveByIdQueryHandler(ILeaveRepository repo, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        public async Task<EMS.Domain.Entities.LeaveRequest?> Handle(Queries.GetLeaveByIdQuery request, CancellationToken cancellationToken)
        {
            var lr = await _repo.GetLeaveByIdAsync(request.Id, cancellationToken);
            if (lr == null) return null;

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != lr.EmployeeId)
                    return null;
            }

            return lr;
        }
    }
}
