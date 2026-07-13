using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class RejectLeaveCommandHandler : IRequestHandler<Commands.RejectLeaveCommand>
    {
        private readonly ILeaveRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly ILogger<RejectLeaveCommandHandler> _logger;

        public RejectLeaveCommandHandler(ILeaveRepository repo, IAuthRepository authRepo, ILogger<RejectLeaveCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _logger = logger;
        }

        public async Task Handle(Commands.RejectLeaveCommand request, CancellationToken cancellationToken)
        {
            var lr = await _repo.GetLeaveByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Leave request {request.Id} not found.");

            if (lr.Status != Domain.Enums.LeaveStatus.Pending)
                throw new InvalidOperationException("Only pending leave requests can be rejected.");

            var approver = await _authRepo.GetByIdAsync(request.ApproverId, cancellationToken);
            var approverEmployeeId = approver?.EmployeeId;

            if (approverEmployeeId.HasValue && approverEmployeeId.Value == lr.EmployeeId)
                throw new InvalidOperationException("You cannot reject your own leave request.");

            lr.ApproverEmployeeId = approverEmployeeId;
            lr.DecisionComments = request.Comments;
            await _repo.RejectLeaveAsync(lr, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Leave request {LeaveId} rejected by {ApproverId}", lr.Id, request.ApproverId);
        }
    }
}
