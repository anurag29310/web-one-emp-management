using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class CreateAttendanceRecordCommandHandler : IRequestHandler<Commands.CreateAttendanceRecordCommand, AttendanceRecord>
    {
        private readonly IAttendanceRepository _repo;
        private readonly ILogger<CreateAttendanceRecordCommandHandler> _logger;

        public CreateAttendanceRecordCommandHandler(IAttendanceRepository repo, ILogger<CreateAttendanceRecordCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<AttendanceRecord> Handle(Commands.CreateAttendanceRecordCommand request, CancellationToken cancellationToken)
        {
            var date = request.AttendanceDate.Date;
            var existing = await _repo.GetRecordByEmployeeAndDateAsync(request.EmployeeId, date, cancellationToken);
            if (existing != null)
                throw new InvalidOperationException("An attendance record already exists for this employee on this date.");

            Shift? shift = request.ShiftId.HasValue ? await _repo.GetShiftByIdAsync(request.ShiftId.Value, cancellationToken) : null;
            var status = Enum.Parse<AttendanceStatus>(request.Status, true);

            var record = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId,
                ShiftId = shift?.Id,
                AttendanceDate = date,
                CheckInAtUtc = request.CheckInAtUtc,
                CheckOutAtUtc = request.CheckOutAtUtc,
                Status = status,
                IsLateArrival = request.CheckInAtUtc.HasValue && AttendanceCalculator.IsLateArrival(request.CheckInAtUtc.Value, shift),
                IsEarlyLeave = request.CheckOutAtUtc.HasValue && AttendanceCalculator.IsEarlyLeave(request.CheckOutAtUtc.Value, shift),
                TotalWorkMinutes = AttendanceCalculator.WorkMinutes(request.CheckInAtUtc, request.CheckOutAtUtc),
                Notes = request.Notes,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repo.AddRecordAsync(record, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created manual attendance record {RecordId} for Employee {EmployeeId}", record.Id, record.EmployeeId);
            return record;
        }
    }
}
