using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class UpdateLeaveRequestCommandHandler : IRequestHandler<Commands.UpdateLeaveRequestCommand>
    {
        private readonly ILeaveRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly ILogger<UpdateLeaveRequestCommandHandler> _logger;

        public UpdateLeaveRequestCommandHandler(ILeaveRepository repo, IAuthRepository authRepo, ILogger<UpdateLeaveRequestCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _logger = logger;
        }

        public async Task Handle(Commands.UpdateLeaveRequestCommand request, CancellationToken cancellationToken)
        {
            var lr = await _repo.GetLeaveByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Leave request {request.Id} not found.");

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != lr.EmployeeId)
                    throw new UnauthorizedAccessException("You can only update your own leave requests.");
            }

            if (lr.Status != Domain.Enums.LeaveStatus.Pending)
                throw new InvalidOperationException("Only pending leave requests can be updated.");

            if (request.EndDate.Date < request.StartDate.Date)
                throw new InvalidOperationException("End date must be on or after start date.");

            lr.StartDate = request.StartDate.Date;
            lr.EndDate = request.EndDate.Date;
            lr.TotalDays = request.TotalDays;
            lr.Reason = request.Reason;

            await _repo.UpdateLeaveRequestAsync(lr, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Leave request {LeaveId} updated", lr.Id);
        }
    }
}
