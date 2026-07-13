using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class CancelLeaveRequestCommandHandler : IRequestHandler<Commands.CancelLeaveRequestCommand>
    {
        private readonly ILeaveRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly ILogger<CancelLeaveRequestCommandHandler> _logger;

        public CancelLeaveRequestCommandHandler(ILeaveRepository repo, IAuthRepository authRepo, ILogger<CancelLeaveRequestCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _logger = logger;
        }

        public async Task Handle(Commands.CancelLeaveRequestCommand request, CancellationToken cancellationToken)
        {
            var lr = await _repo.GetLeaveByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Leave request {request.Id} not found.");

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestedByUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != lr.EmployeeId)
                    throw new UnauthorizedAccessException("You can only cancel your own leave requests.");
            }

            if (lr.Status != Domain.Enums.LeaveStatus.Pending)
                throw new InvalidOperationException("Only pending leave requests can be cancelled.");

            await _repo.CancelLeaveAsync(lr, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Leave request {LeaveId} cancelled by {UserId}", lr.Id, request.RequestedByUserId);
        }
    }
}
