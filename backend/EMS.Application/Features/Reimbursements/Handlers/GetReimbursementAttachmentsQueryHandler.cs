using EMS.Application.Features.Reimbursements.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reimbursements.Handlers
{
    public class GetReimbursementAttachmentsQueryHandler : IRequestHandler<GetReimbursementAttachmentsQuery, IEnumerable<ReimbursementAttachmentDto>>
    {
        private readonly IReimbursementRepository _repo;
        private readonly IAuthRepository _authRepo;

        public GetReimbursementAttachmentsQueryHandler(IReimbursementRepository repo, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        public async Task<IEnumerable<ReimbursementAttachmentDto>> Handle(GetReimbursementAttachmentsQuery request, CancellationToken cancellationToken)
        {
            var reimbursement = await _repo.GetByIdAsync(request.ReimbursementId, cancellationToken)
                ?? throw new InvalidOperationException($"Reimbursement {request.ReimbursementId} not found.");

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != reimbursement.EmployeeId)
                    throw new UnauthorizedAccessException("You can only view attachments on your own reimbursements.");
            }

            var attachments = await _repo.GetAttachmentsAsync(request.ReimbursementId, cancellationToken);
            return attachments.Select(ReimbursementAttachmentDto.FromEntity);
        }
    }
}
