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
    public class WithdrawOfferCommandHandler : IRequestHandler<WithdrawOfferCommand>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<WithdrawOfferCommandHandler> _logger;

        public WithdrawOfferCommandHandler(IRecruitmentRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<WithdrawOfferCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(WithdrawOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _repo.GetOfferByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Offer {request.Id} not found.");

            if (offer.Status is not (OfferStatus.Draft or OfferStatus.Sent))
                throw new InvalidOperationException($"Only a Draft or Sent offer can be withdrawn (currently {offer.Status}).");

            offer.Status = OfferStatus.Withdrawn;
            offer.UpdatedAtUtc = DateTime.UtcNow;
            offer.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateOfferAsync(offer, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Withdrew offer {OfferId}", offer.Id);

            await _auditLogger.LogAsync("Offer", offer.Id, "Withdrawn", ct: cancellationToken);
        }
    }
}
