using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class CreateShiftCommandHandler : IRequestHandler<Commands.CreateShiftCommand, Shift>
    {
        private readonly IAttendanceRepository _repo;
        private readonly ILogger<CreateShiftCommandHandler> _logger;

        public CreateShiftCommandHandler(IAttendanceRepository repo, ILogger<CreateShiftCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Shift> Handle(Commands.CreateShiftCommand request, CancellationToken cancellationToken)
        {
            var shift = new Shift
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                GraceMinutes = request.GraceMinutes,
                IsNightShift = request.IsNightShift,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repo.AddShiftAsync(shift, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created shift {ShiftName} ({ShiftId})", shift.Name, shift.Id);
            return shift;
        }
    }
}
