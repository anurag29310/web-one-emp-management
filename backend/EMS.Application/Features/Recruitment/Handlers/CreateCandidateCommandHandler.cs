using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class CreateCandidateCommandHandler : IRequestHandler<CreateCandidateCommand, Guid>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CreateCandidateCommandHandler> _logger;

        public CreateCandidateCommandHandler(IRecruitmentRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<CreateCandidateCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateCandidateCommand request, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            var candidate = new Candidate
            {
                Id = id,
                CandidateNumber = $"CAN-{id:N}".Substring(0, 12).ToUpperInvariant(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DesignationId = request.DesignationId,
                DepartmentId = request.DepartmentId,
                Source = request.Source,
                AppliedDate = request.AppliedDate,
                Notes = request.Notes,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId,
                IsDeleted = false
            };

            await _repo.AddAsync(candidate, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created candidate {CandidateNumber} ({CandidateId})", candidate.CandidateNumber, candidate.Id);

            await _auditLogger.LogAsync("Candidate", candidate.Id, "Created", ct: cancellationToken);

            return candidate.Id;
        }
    }
}
