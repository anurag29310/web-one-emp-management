using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class CheckOutCommandHandler : IRequestHandler<Commands.CheckOutCommand, AttendanceRecord>
    {
        private readonly IAttendanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly ILogger<CheckOutCommandHandler> _logger;

        public CheckOutCommandHandler(IAttendanceRepository repo, IAuthRepository authRepo, ILogger<CheckOutCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _logger = logger;
        }

        public async Task<AttendanceRecord> Handle(Commands.CheckOutCommand request, CancellationToken cancellationToken)
        {
            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != request.EmployeeId)
                    throw new UnauthorizedAccessException("You can only check out on your own behalf.");
            }

            var date = request.CheckOutAtUtc.Date;
            var existing = await _repo.GetRecordByEmployeeAndDateAsync(request.EmployeeId, date, cancellationToken)
                ?? throw new InvalidOperationException("Attendance record not found for this date; check in first.");

            if (existing.CheckOutAtUtc != null)
                throw new InvalidOperationException("Already checked out for this date.");

            var shift = existing.ShiftId.HasValue ? await _repo.GetShiftByIdAsync(existing.ShiftId.Value, cancellationToken) : null;

            existing.CheckOutAtUtc = request.CheckOutAtUtc;
            existing.IsEarlyLeave = AttendanceCalculator.IsEarlyLeave(request.CheckOutAtUtc, shift);
            existing.TotalWorkMinutes = AttendanceCalculator.WorkMinutes(existing.CheckInAtUtc, existing.CheckOutAtUtc);
            existing.Notes = request.Notes ?? existing.Notes;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateRecordAsync(existing, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Employee {EmployeeId} checked out for {Date}", request.EmployeeId, date);
            return existing;
        }
    }
}
