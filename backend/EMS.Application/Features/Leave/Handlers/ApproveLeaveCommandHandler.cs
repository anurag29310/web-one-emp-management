using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EMS.Application.Features.Leave.Handlers
{
    public class ApproveLeaveCommandHandler 
    {
        private readonly ILeaveRepository _repo;
        private readonly ILogger<ApproveLeaveCommandHandler> _logger;

        public ApproveLeaveCommandHandler(ILeaveRepository repo, ILogger<ApproveLeaveCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Unit> Handle(Commands.ApproveLeaveCommand request, CancellationToken cancellationToken)
        {
            var lr = await _repo.GetLeaveByIdAsync(request.Id) ?? throw new System.InvalidOperationException("Leave request not found");
            lr.ApproverEmployeeId = request.ApproverId;
            lr.DecisionComments = request.Comments;
            await _repo.ApproveLeaveAsync(lr);
            await _repo.SaveChangesAsync();
            _logger.LogInformation("Leave request {LeaveId} approved by {Approver}", lr.Id, request.ApproverId);
            return Unit.Value;
        }
    }
}
