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
    public class AcceptOfferCommandHandler : IRequestHandler<AcceptOfferCommand>
    {
        // Default "Joining Checklist" seeded when an offer is accepted — requirements.md names the
        // feature but not its items, so this is a reasonable default set; Admin/HR can add more via
        // AddChecklistItemCommand.
        private static readonly string[] DefaultChecklistItems =
        {
            "Offer Letter Signed",
            "ID Proof Submitted",
            "Bank Details Collected",
            "Laptop/Asset Allocated",
            "Induction Completed"
        };

        private readonly IRecruitmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<AcceptOfferCommandHandler> _logger;

        public AcceptOfferCommandHandler(IRecruitmentRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<AcceptOfferCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(AcceptOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _repo.GetOfferByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Offer {request.Id} not found.");

            if (offer.Status != OfferStatus.Sent)
                throw new InvalidOperationException($"Only a Sent offer can be accepted (currently {offer.Status}).");

            var now = DateTime.UtcNow;
            offer.Status = OfferStatus.Accepted;
            offer.RespondedAtUtc = now;
            offer.UpdatedAtUtc = now;
            offer.UpdatedBy = _currentUser.UserId;
            await _repo.UpdateOfferAsync(offer, cancellationToken);

            foreach (var itemName in DefaultChecklistItems)
            {
                await _repo.AddChecklistItemAsync(new OnboardingChecklistItem
                {
                    Id = Guid.NewGuid(),
                    CandidateId = offer.CandidateId,
                    ItemName = itemName,
                    IsCompleted = false,
                    CreatedAtUtc = now,
                    CreatedBy = _currentUser.UserId
                }, cancellationToken);
            }

            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Offer {OfferId} accepted; seeded {Count} onboarding checklist items for candidate {CandidateId}", offer.Id, DefaultChecklistItems.Length, offer.CandidateId);

            await _auditLogger.LogAsync("Offer", offer.Id, "Accepted", ct: cancellationToken);
        }
    }
}
