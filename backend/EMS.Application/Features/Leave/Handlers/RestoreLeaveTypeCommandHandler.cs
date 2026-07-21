using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class RestoreLeaveTypeCommandHandler : IRequestHandler<Commands.RestoreLeaveTypeCommand>
    {
        private readonly ILeaveRepository _repo;
        private readonly ILogger<RestoreLeaveTypeCommandHandler> _logger;

        public RestoreLeaveTypeCommandHandler(ILeaveRepository repo, ILogger<RestoreLeaveTypeCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Handle(Commands.RestoreLeaveTypeCommand request, CancellationToken cancellationToken)
        {
            var leaveType = await _repo.GetLeaveTypeByIdIncludingDeletedAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Leave type {request.Id} not found.");

            if (!leaveType.IsDeleted)
                throw new InvalidOperationException("Leave type is not deleted and cannot be restored.");

            await _repo.RestoreLeaveTypeAsync(leaveType, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Restored leave type {LeaveTypeId}", leaveType.Id);
        }
    }
}
