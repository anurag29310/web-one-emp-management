using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class CreateOfferCommandHandler : IRequestHandler<CreateOfferCommand, Guid>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CreateOfferCommandHandler> _logger;

        public CreateOfferCommandHandler(IRecruitmentRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<CreateOfferCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateOfferCommand request, CancellationToken cancellationToken)
        {
            var candidate = await _repo.GetByIdAsync(request.CandidateId, cancellationToken)
                ?? throw new InvalidOperationException($"Candidate {request.CandidateId} not found.");

            if (candidate.Status is CandidateStatus.Hired or CandidateStatus.Rejected or CandidateStatus.Withdrawn)
                throw new InvalidOperationException($"Candidate {candidate.CandidateNumber} is in a terminal state ({candidate.Status}) and cannot receive an offer.");

            var id = Guid.NewGuid();
            var offer = new Offer
            {
                Id = id,
                OfferNumber = $"OFR-{id:N}".Substring(0, 12).ToUpperInvariant(),
                CandidateId = request.CandidateId,
                DesignationId = request.DesignationId,
                DepartmentId = request.DepartmentId,
                OfferedSalary = request.OfferedSalary,
                JoiningDate = request.JoiningDate,
                ExpiresAtUtc = request.ExpiresAtUtc,
                Notes = request.Notes,
                Status = OfferStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            await _repo.AddOfferAsync(offer, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created offer {OfferNumber} for candidate {CandidateId}", offer.OfferNumber, candidate.Id);

            await _auditLogger.LogAsync("Offer", offer.Id, "Created", ct: cancellationToken);

            return offer.Id;
        }
    }
}
