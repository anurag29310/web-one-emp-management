using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class CreateLeaveTypeCommandHandler : IRequestHandler<Commands.CreateLeaveTypeCommand, LeaveType>
    {
        private readonly ILeaveRepository _repo;
        private readonly ILogger<CreateLeaveTypeCommandHandler> _logger;

        public CreateLeaveTypeCommandHandler(ILeaveRepository repo, ILogger<CreateLeaveTypeCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<LeaveType> Handle(Commands.CreateLeaveTypeCommand request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.Code) && await _repo.LeaveTypeCodeExistsAsync(request.Code, ct: cancellationToken))
                throw new InvalidOperationException("Leave type code already exists.");

            var leaveType = new LeaveType
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Code = request.Code,
                IsPaid = request.IsPaid,
                RequiresApproval = request.RequiresApproval,
                AnnualEntitlementDays = request.AnnualEntitlementDays,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            await _repo.AddLeaveTypeAsync(leaveType, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created leave type {LeaveTypeName} ({LeaveTypeId})", leaveType.Name, leaveType.Id);
            return leaveType;
        }
    }
}
