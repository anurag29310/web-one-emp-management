using EMS.Application.Common.DTOs;
using EMS.Application.Features.Reimbursements.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reimbursements.Handlers
{
    public class GetReimbursementsQueryHandler : IRequestHandler<GetReimbursementsQuery, PagedResult<ReimbursementDto>>
    {
        private readonly IReimbursementRepository _repo;
        private readonly IAuthRepository _authRepo;

        public GetReimbursementsQueryHandler(IReimbursementRepository repo, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        public async Task<PagedResult<ReimbursementDto>> Handle(GetReimbursementsQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            // Non-privileged callers are always scoped to their own reimbursements, regardless of
            // any employeeId filter supplied — same rule as Task Management's list endpoint.
            var employeeId = request.EmployeeId;
            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                employeeId = requester?.EmployeeId ?? System.Guid.Empty;
            }

            var items = await _repo.GetAllAsync(page, pageSize, employeeId, request.Status, cancellationToken);
            var total = await _repo.CountAsync(employeeId, request.Status, cancellationToken);

            return PagedResult<ReimbursementDto>.Create(
                items.Select(ReimbursementDto.FromEntity),
                page,
                pageSize,
                total);
        }
    }
}
