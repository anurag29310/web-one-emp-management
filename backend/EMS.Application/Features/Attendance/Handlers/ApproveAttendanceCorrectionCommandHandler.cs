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
    public class ApproveAttendanceCorrectionCommandHandler : IRequestHandler<Commands.ApproveAttendanceCorrectionCommand>
    {
        private readonly IAttendanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly ILogger<ApproveAttendanceCorrectionCommandHandler> _logger;

        public ApproveAttendanceCorrectionCommandHandler(IAttendanceRepository repo, IAuthRepository authRepo, ILogger<ApproveAttendanceCorrectionCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _logger = logger;
        }

        public async Task Handle(Commands.ApproveAttendanceCorrectionCommand request, CancellationToken cancellationToken)
        {
            var correction = await _repo.GetCorrectionByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Attendance correction {request.Id} not found.");

            if (correction.Status != CorrectionStatus.Pending)
                throw new InvalidOperationException("Only pending corrections can be approved.");

            var approver = await _authRepo.GetByIdAsync(request.ApproverUserId, cancellationToken);
            var approverEmployeeId = approver?.EmployeeId;

            if (approverEmployeeId.HasValue && approverEmployeeId.Value == correction.RequestedByEmployeeId)
                throw new InvalidOperationException("You cannot approve your own attendance correction.");

            var record = correction.AttendanceRecord
                ?? await _repo.GetRecordByIdAsync(correction.AttendanceRecordId, cancellationToken)
                ?? throw new InvalidOperationException("Linked attendance record not found.");

            if (correction.RequestedCheckInAtUtc.HasValue) record.CheckInAtUtc = correction.RequestedCheckInAtUtc;
            if (correction.RequestedCheckOutAtUtc.HasValue) record.CheckOutAtUtc = correction.RequestedCheckOutAtUtc;

            Shift? shift = record.ShiftId.HasValue ? await _repo.GetShiftByIdAsync(record.ShiftId.Value, cancellationToken) : null;
            if (record.CheckInAtUtc.HasValue) record.IsLateArrival = AttendanceCalculator.IsLateArrival(record.CheckInAtUtc.Value, shift);
            if (record.CheckOutAtUtc.HasValue) record.IsEarlyLeave = AttendanceCalculator.IsEarlyLeave(record.CheckOutAtUtc.Value, shift);
            record.TotalWorkMinutes = AttendanceCalculator.WorkMinutes(record.CheckInAtUtc, record.CheckOutAtUtc);
            record.UpdatedAtUtc = DateTime.UtcNow;
            await _repo.UpdateRecordAsync(record, cancellationToken);

            correction.Status = CorrectionStatus.Approved;
            correction.ApprovedByEmployeeId = approverEmployeeId;
            correction.DecisionAtUtc = DateTime.UtcNow;
            correction.DecisionComments = request.Comments;
            await _repo.UpdateCorrectionAsync(correction, cancellationToken);

            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Attendance correction {CorrectionId} approved by {ApproverId}", correction.Id, request.ApproverUserId);
        }
    }
}
