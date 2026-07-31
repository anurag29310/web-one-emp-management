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
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<UpdateLeaveTypeCommandHandler> _logger;

        public UpdateLeaveTypeCommandHandler(ILeaveRepository repo, ICurrentUserService currentUser, ILogger<UpdateLeaveTypeCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<LeaveType> Handle(Commands.UpdateLeaveTypeCommand request, CancellationToken cancellationToken)
        {
            var companyId = _currentUser.CompanyId!.Value;
            var leaveType = await _repo.GetLeaveTypeByIdAsync(request.Id, companyId, cancellationToken)
                ?? throw new InvalidOperationException($"Leave type {request.Id} not found.");

            if (!string.IsNullOrWhiteSpace(request.Code) && await _repo.LeaveTypeCodeExistsAsync(request.Code, companyId, request.Id, cancellationToken))
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
