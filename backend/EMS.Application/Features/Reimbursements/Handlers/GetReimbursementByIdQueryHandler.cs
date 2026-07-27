using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reimbursements.Handlers
{
    public class GetReimbursementByIdQueryHandler : IRequestHandler<GetReimbursementByIdQuery, Reimbursement?>
    {
        private readonly IReimbursementRepository _repo;
        private readonly IAuthRepository _authRepo;

        public GetReimbursementByIdQueryHandler(IReimbursementRepository repo, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        public async Task<Reimbursement?> Handle(GetReimbursementByIdQuery request, CancellationToken cancellationToken)
        {
            var reimbursement = await _repo.GetByIdAsync(request.Id, cancellationToken);
            if (reimbursement == null) return null;

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != reimbursement.EmployeeId)
                    return null;
            }

            return reimbursement;
        }
    }
}
