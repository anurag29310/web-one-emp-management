using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class UpdateLeaveTypeCommandHandler : IRequestHandler<Commands.UpdateLeaveTypeCommand, LeaveType>
    {
        private readonly ILeaveRepository _repo;
        private readonly ILogger<UpdateLeaveTypeCommandHandler> _logger;

        public UpdateLeaveTypeCommandHandler(ILeaveRepository repo, ILogger<UpdateLeaveTypeCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<LeaveType> Handle(Commands.UpdateLeaveTypeCommand request, CancellationToken cancellationToken)
        {
            var leaveType = await _repo.GetLeaveTypeByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Leave type {request.Id} not found.");

            if (!string.IsNullOrWhiteSpace(request.Code) && await _repo.LeaveTypeCodeExistsAsync(request.Code, request.Id, cancellationToken))
                throw new InvalidOperationException("Leave type code already exists.");

            leaveType.Name = request.Name;
            leaveType.Code = request.Code;
            leaveType.IsPaid = request.IsPaid;
            leaveType.RequiresApproval = request.RequiresApproval;
            leaveType.AnnualEntitlementDays = request.AnnualEntitlementDays;
            leaveType.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateLeaveTypeAsync(leaveType, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated leave type {LeaveTypeId}", leaveType.Id);
            return leaveType;
        }
    }
}
