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
        private readonly IAuthRepository _authRepo;
        private readonly ILogger<ApproveLeaveCommandHandler> _logger;

        public ApproveLeaveCommandHandler(ILeaveRepository repo, IAuthRepository authRepo, ILogger<ApproveLeaveCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _logger = logger;
        }

        public async Task Handle(Commands.ApproveLeaveCommand request, CancellationToken cancellationToken)
        {
            var lr = await _repo.GetLeaveByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Leave request {request.Id} not found.");

            if (lr.Status != Domain.Enums.LeaveStatus.Pending)
                throw new InvalidOperationException("Only pending leave requests can be approved.");

            // ApproverId identifies the logged-in User; resolve it to the linked Employee (if any)
            // so ApproverEmployeeId records who approved it, not the raw auth account id.
            var approver = await _authRepo.GetByIdAsync(request.ApproverId, cancellationToken);
            var approverEmployeeId = approver?.EmployeeId;

            if (approverEmployeeId.HasValue && approverEmployeeId.Value == lr.EmployeeId)
                throw new InvalidOperationException("You cannot approve your own leave request.");

            lr.ApproverEmployeeId = approverEmployeeId;
            lr.DecisionComments = request.Comments;
            await _repo.ApproveLeaveAsync(lr, cancellationToken);

            var balance = await _repo.GetLeaveBalanceAsync(lr.EmployeeId, lr.LeaveTypeId, lr.StartDate.Year, cancellationToken);
            if (balance != null)
            {
                balance.Used += lr.TotalDays;
                balance.Available = balance.OpeningBalance + balance.Accrued + balance.Adjusted - balance.Used;
                await _repo.UpdateLeaveBalanceAsync(balance, cancellationToken);
            }

            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Leave request {LeaveId} approved by {ApproverId}", lr.Id, request.ApproverId);
        }
    }
}
