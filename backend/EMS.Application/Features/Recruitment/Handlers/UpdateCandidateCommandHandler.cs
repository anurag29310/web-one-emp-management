using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class UpdateCandidateCommandHandler : IRequestHandler<UpdateCandidateCommand>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateCandidateCommandHandler> _logger;

        public UpdateCandidateCommandHandler(IRecruitmentRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<UpdateCandidateCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(UpdateCandidateCommand request, CancellationToken cancellationToken)
        {
            var candidate = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Candidate {request.Id} not found.");

            candidate.FirstName = request.FirstName;
            candidate.LastName = request.LastName;
            candidate.Email = request.Email;
            candidate.PhoneNumber = request.PhoneNumber;
            candidate.DesignationId = request.DesignationId;
            candidate.DepartmentId = request.DepartmentId;
            candidate.Source = request.Source;
            candidate.Notes = request.Notes;
            candidate.UpdatedAtUtc = DateTime.UtcNow;
            candidate.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateAsync(candidate, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated candidate {CandidateId}", candidate.Id);

            await _auditLogger.LogAsync("Candidate", candidate.Id, "Updated", ct: cancellationToken);
        }
    }
}
