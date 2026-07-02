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
        private readonly ILogger<CreateLeaveRequestCommandHandler> _logger;

        public CreateLeaveRequestCommandHandler(ILeaveRepository repo, ILogger<CreateLeaveRequestCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<LeaveRequest> Handle(Commands.CreateLeaveRequestCommand request, CancellationToken cancellationToken)
        {
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
