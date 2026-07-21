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
    public class UpdateAttendanceRecordCommandHandler : IRequestHandler<Commands.UpdateAttendanceRecordCommand, AttendanceRecord>
    {
        private readonly IAttendanceRepository _repo;
        private readonly ILogger<UpdateAttendanceRecordCommandHandler> _logger;

        public UpdateAttendanceRecordCommandHandler(IAttendanceRepository repo, ILogger<UpdateAttendanceRecordCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<AttendanceRecord> Handle(Commands.UpdateAttendanceRecordCommand request, CancellationToken cancellationToken)
        {
            var record = await _repo.GetRecordByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Attendance record {request.Id} not found.");

            Shift? shift = request.ShiftId.HasValue ? await _repo.GetShiftByIdAsync(request.ShiftId.Value, cancellationToken) : null;

            record.ShiftId = shift?.Id;
            record.CheckInAtUtc = request.CheckInAtUtc;
            record.CheckOutAtUtc = request.CheckOutAtUtc;
            record.Status = Enum.Parse<AttendanceStatus>(request.Status, true);
            record.IsLateArrival = request.CheckInAtUtc.HasValue && AttendanceCalculator.IsLateArrival(request.CheckInAtUtc.Value, shift);
            record.IsEarlyLeave = request.CheckOutAtUtc.HasValue && AttendanceCalculator.IsEarlyLeave(request.CheckOutAtUtc.Value, shift);
            record.TotalWorkMinutes = AttendanceCalculator.WorkMinutes(request.CheckInAtUtc, request.CheckOutAtUtc);
            record.Notes = request.Notes;
            record.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateRecordAsync(record, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated attendance record {RecordId}", record.Id);
            return record;
        }
    }
}
