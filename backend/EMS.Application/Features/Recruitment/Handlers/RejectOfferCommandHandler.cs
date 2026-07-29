using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class RejectOfferCommandHandler : IRequestHandler<RejectOfferCommand>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RejectOfferCommandHandler> _logger;

        public RejectOfferCommandHandler(IRecruitmentRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<RejectOfferCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RejectOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _repo.GetOfferByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Offer {request.Id} not found.");

            if (offer.Status != OfferStatus.Sent)
                throw new InvalidOperationException($"Only a Sent offer can be rejected (currently {offer.Status}).");

            var now = DateTime.UtcNow;
            offer.Status = OfferStatus.Rejected;
            offer.RespondedAtUtc = now;
            offer.Notes = string.IsNullOrWhiteSpace(request.Reason) ? offer.Notes : $"{offer.Notes}\nRejected: {request.Reason}".Trim();
            offer.UpdatedAtUtc = now;
            offer.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateOfferAsync(offer, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Offer {OfferId} rejected by candidate", offer.Id);

            await _auditLogger.LogAsync("Offer", offer.Id, "Rejected", ct: cancellationToken);
        }
    }
}
