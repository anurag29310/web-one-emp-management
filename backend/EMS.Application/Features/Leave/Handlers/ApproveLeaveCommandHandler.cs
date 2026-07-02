using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class ApproveLeaveCommandHandler : IRequestHandler<Commands.ApproveLeaveCommand>
    {
        private readonly ILeaveRepository _repo;
        private readonly ILogger<ApproveLeaveCommandHandler> _logger;

        public ApproveLeaveCommandHandler(ILeaveRepository repo, ILogger<ApproveLeaveCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Handle(Commands.ApproveLeaveCommand request, CancellationToken cancellationToken)
        {
            var lr = await _repo.GetLeaveByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Leave request {request.Id} not found.");

            if (lr.Status != Domain.Enums.LeaveStatus.Pending)
                throw new InvalidOperationException("Only pending leave requests can be approved.");

            lr.ApproverEmployeeId = request.ApproverId;
            lr.DecisionComments = request.Comments;
            await _repo.ApproveLeaveAsync(lr, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Leave request {LeaveId} approved by {ApproverId}", lr.Id, request.ApproverId);
        }
    }
}
