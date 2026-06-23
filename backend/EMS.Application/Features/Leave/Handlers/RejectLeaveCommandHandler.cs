using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EMS.Application.Features.Leave.Handlers
{
    public class RejectLeaveCommandHandler 
    {
        private readonly ILeaveRepository _repo;
        private readonly ILogger<RejectLeaveCommandHandler> _logger;

        public RejectLeaveCommandHandler(ILeaveRepository repo, ILogger<RejectLeaveCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Unit> Handle(Commands.RejectLeaveCommand request, CancellationToken cancellationToken)
        {
            var lr = await _repo.GetLeaveByIdAsync(request.Id) ?? throw new System.InvalidOperationException("Leave request not found");
            lr.ApproverEmployeeId = request.ApproverId;
            lr.DecisionComments = request.Comments;
            await _repo.RejectLeaveAsync(lr);
            await _repo.SaveChangesAsync();
            _logger.LogInformation("Leave request {LeaveId} rejected by {Approver}", lr.Id, request.ApproverId);
            return Unit.Value;
        }
    }
}
