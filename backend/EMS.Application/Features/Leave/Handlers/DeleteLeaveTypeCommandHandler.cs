using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class DeleteLeaveTypeCommandHandler : IRequestHandler<Commands.DeleteLeaveTypeCommand>
    {
        private readonly ILeaveRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<DeleteLeaveTypeCommandHandler> _logger;

        public DeleteLeaveTypeCommandHandler(ILeaveRepository repo, ICurrentUserService currentUser, ILogger<DeleteLeaveTypeCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task Handle(Commands.DeleteLeaveTypeCommand request, CancellationToken cancellationToken)
        {
            var leaveType = await _repo.GetLeaveTypeByIdAsync(request.Id, _currentUser.CompanyId!.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Leave type {request.Id} not found.");

            await _repo.DeleteLeaveTypeAsync(leaveType, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) leave type {LeaveTypeId}", leaveType.Id);
        }
    }
}
