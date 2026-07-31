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
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CreateLeaveTypeCommandHandler> _logger;

        public CreateLeaveTypeCommandHandler(ILeaveRepository repo, ICurrentUserService currentUser, ILogger<CreateLeaveTypeCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<LeaveType> Handle(Commands.CreateLeaveTypeCommand request, CancellationToken cancellationToken)
        {
            var companyId = _currentUser.CompanyId!.Value;
            if (!string.IsNullOrWhiteSpace(request.Code) && await _repo.LeaveTypeCodeExistsAsync(request.Code, companyId, ct: cancellationToken))
                throw new InvalidOperationException("Leave type code already exists.");

            var leaveType = new LeaveType
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
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
