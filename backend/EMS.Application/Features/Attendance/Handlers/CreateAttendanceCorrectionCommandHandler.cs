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
    public class CreateAttendanceCorrectionCommandHandler : IRequestHandler<Commands.CreateAttendanceCorrectionCommand, AttendanceCorrection>
    {
        private readonly IAttendanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly ILogger<CreateAttendanceCorrectionCommandHandler> _logger;

        public CreateAttendanceCorrectionCommandHandler(IAttendanceRepository repo, IAuthRepository authRepo, ILogger<CreateAttendanceCorrectionCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _logger = logger;
        }

        public async Task<AttendanceCorrection> Handle(Commands.CreateAttendanceCorrectionCommand request, CancellationToken cancellationToken)
        {
            var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
            if (requester?.EmployeeId == null)
                throw new UnauthorizedAccessException("Only employees linked to a user account can request attendance corrections.");

            var record = await _repo.GetRecordByIdAsync(request.AttendanceRecordId, cancellationToken)
                ?? throw new InvalidOperationException("Attendance record not found.");

            if (record.EmployeeId != requester.EmployeeId.Value)
                throw new UnauthorizedAccessException("You can only request corrections for your own attendance records.");

            var correction = new AttendanceCorrection
            {
                Id = Guid.NewGuid(),
                AttendanceRecordId = request.AttendanceRecordId,
                RequestedByEmployeeId = requester.EmployeeId.Value,
                RequestedCheckInAtUtc = request.RequestedCheckInAtUtc,
                RequestedCheckOutAtUtc = request.RequestedCheckOutAtUtc,
                Reason = request.Reason,
                Status = CorrectionStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repo.AddCorrectionAsync(correction, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Attendance correction {CorrectionId} requested for record {RecordId}", correction.Id, record.Id);
            return correction;
        }
    }
}
