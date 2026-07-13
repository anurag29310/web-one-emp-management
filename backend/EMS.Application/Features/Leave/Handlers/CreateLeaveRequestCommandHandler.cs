using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class CreateLeaveRequestCommandHandler : IRequestHandler<Commands.CreateLeaveRequestCommand, LeaveRequest>
    {
        private readonly ILeaveRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly ILogger<CreateLeaveRequestCommandHandler> _logger;

        public CreateLeaveRequestCommandHandler(ILeaveRepository repo, IAuthRepository authRepo, ILogger<CreateLeaveRequestCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _logger = logger;
        }

        public async Task<LeaveRequest> Handle(Commands.CreateLeaveRequestCommand request, CancellationToken cancellationToken)
        {
            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != request.EmployeeId)
                    throw new UnauthorizedAccessException("You can only apply for leave on your own behalf.");
            }

            var lr = new LeaveRequest
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId,
                LeaveTypeId = request.LeaveTypeId,
                StartDate = request.StartDate.Date,
                EndDate = request.EndDate.Date,
                TotalDays = request.TotalDays,
                Reason = request.Reason,
                Status = Domain.Enums.LeaveStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repo.AddLeaveRequestAsync(lr, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Leave request created {LeaveRequestId} for Employee {EmployeeId}", lr.Id, lr.EmployeeId);
            return lr;
        }
    }
}
