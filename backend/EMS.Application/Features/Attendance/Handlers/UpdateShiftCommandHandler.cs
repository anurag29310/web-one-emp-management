using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class UpdateShiftCommandHandler : IRequestHandler<Commands.UpdateShiftCommand, Shift>
    {
        private readonly IAttendanceRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<UpdateShiftCommandHandler> _logger;

        public UpdateShiftCommandHandler(IAttendanceRepository repo, ICurrentUserService currentUser, ILogger<UpdateShiftCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Shift> Handle(Commands.UpdateShiftCommand request, CancellationToken cancellationToken)
        {
            var shift = await _repo.GetShiftByIdAsync(request.Id, _currentUser.CompanyId!.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Shift {request.Id} not found.");

            shift.Name = request.Name;
            shift.StartTime = request.StartTime;
            shift.EndTime = request.EndTime;
            shift.GraceMinutes = request.GraceMinutes;
            shift.IsNightShift = request.IsNightShift;
            shift.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateShiftAsync(shift, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated shift {ShiftId}", shift.Id);
            return shift;
        }
    }
}
