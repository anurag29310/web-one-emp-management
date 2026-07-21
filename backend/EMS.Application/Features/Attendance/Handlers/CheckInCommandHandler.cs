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
    public class CheckInCommandHandler : IRequestHandler<Commands.CheckInCommand, AttendanceRecord>
    {
        private readonly IAttendanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly ILogger<CheckInCommandHandler> _logger;

        public CheckInCommandHandler(IAttendanceRepository repo, IAuthRepository authRepo, ILogger<CheckInCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _logger = logger;
        }

        public async Task<AttendanceRecord> Handle(Commands.CheckInCommand request, CancellationToken cancellationToken)
        {
            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != request.EmployeeId)
                    throw new UnauthorizedAccessException("You can only check in on your own behalf.");
            }

            var date = request.CheckInAtUtc.Date;
            var existing = await _repo.GetRecordByEmployeeAndDateAsync(request.EmployeeId, date, cancellationToken);
            if (existing?.CheckInAtUtc != null)
                throw new InvalidOperationException("Already checked in for this date.");

            var activeShift = await _repo.GetActiveEmployeeShiftAsync(request.EmployeeId, date, cancellationToken);
            var shift = activeShift != null ? await _repo.GetShiftByIdAsync(activeShift.ShiftId, cancellationToken) : null;
            var isLate = AttendanceCalculator.IsLateArrival(request.CheckInAtUtc, shift);
            var status = isLate ? AttendanceStatus.Late : AttendanceStatus.Present;

            if (existing != null)
            {
                existing.ShiftId = shift?.Id;
                existing.CheckInAtUtc = request.CheckInAtUtc;
                existing.IsLateArrival = isLate;
                existing.Status = status;
                existing.Notes = request.Notes ?? existing.Notes;
                existing.UpdatedAtUtc = DateTime.UtcNow;

                await _repo.UpdateRecordAsync(existing, cancellationToken);
                await _repo.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Employee {EmployeeId} checked in for {Date}", request.EmployeeId, date);
                return existing;
            }

            var record = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId,
                ShiftId = shift?.Id,
                AttendanceDate = date,
                CheckInAtUtc = request.CheckInAtUtc,
                Status = status,
                IsLateArrival = isLate,
                IsEarlyLeave = false,
                Notes = request.Notes,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repo.AddRecordAsync(record, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Employee {EmployeeId} checked in for {Date}", request.EmployeeId, date);
            return record;
        }
    }
}
