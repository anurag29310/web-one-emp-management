using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class DeleteShiftCommandHandler : IRequestHandler<Commands.DeleteShiftCommand>
    {
        private readonly IAttendanceRepository _repo;
        private readonly ILogger<DeleteShiftCommandHandler> _logger;

        public DeleteShiftCommandHandler(IAttendanceRepository repo, ILogger<DeleteShiftCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Handle(Commands.DeleteShiftCommand request, CancellationToken cancellationToken)
        {
            var shift = await _repo.GetShiftByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Shift {request.Id} not found.");

            await _repo.DeleteShiftAsync(shift, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) shift {ShiftId}", shift.Id);
        }
    }
}
