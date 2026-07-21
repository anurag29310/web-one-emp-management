using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class RejectAttendanceCorrectionCommandHandler : IRequestHandler<Commands.RejectAttendanceCorrectionCommand>
    {
        private readonly IAttendanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly ILogger<RejectAttendanceCorrectionCommandHandler> _logger;

        public RejectAttendanceCorrectionCommandHandler(IAttendanceRepository repo, IAuthRepository authRepo, ILogger<RejectAttendanceCorrectionCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _logger = logger;
        }

        public async Task Handle(Commands.RejectAttendanceCorrectionCommand request, CancellationToken cancellationToken)
        {
            var correction = await _repo.GetCorrectionByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Attendance correction {request.Id} not found.");

            if (correction.Status != CorrectionStatus.Pending)
                throw new InvalidOperationException("Only pending corrections can be rejected.");

            var approver = await _authRepo.GetByIdAsync(request.ApproverUserId, cancellationToken);

            correction.Status = CorrectionStatus.Rejected;
            correction.ApprovedByEmployeeId = approver?.EmployeeId;
            correction.DecisionAtUtc = DateTime.UtcNow;
            correction.DecisionComments = request.Comments;

            await _repo.UpdateCorrectionAsync(correction, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Attendance correction {CorrectionId} rejected by {ApproverId}", correction.Id, request.ApproverUserId);
        }
    }
}
