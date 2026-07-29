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
    public class SendOfferCommandHandler : IRequestHandler<SendOfferCommand>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly IPdfService _pdf;
        private readonly IFileStorageService _storage;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<SendOfferCommandHandler> _logger;

        public SendOfferCommandHandler(IRecruitmentRepository repo, IPdfService pdf, IFileStorageService storage, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<SendOfferCommandHandler> logger)
        {
            _repo = repo;
            _pdf = pdf;
            _storage = storage;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(SendOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _repo.GetOfferByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Offer {request.Id} not found.");

            if (offer.Status != OfferStatus.Draft)
                throw new InvalidOperationException($"Only a Draft offer can be sent (currently {offer.Status}).");

            var now = DateTime.UtcNow;

            var document = new OfferLetterDocument
            {
                OfferNumber = offer.OfferNumber,
                CandidateName = $"{offer.Candidate!.FirstName} {offer.Candidate.LastName}".Trim(),
                DesignationName = offer.Designation?.Name ?? "-",
                DepartmentName = offer.Department?.Name,
                OfferedSalary = offer.OfferedSalary,
                JoiningDate = offer.JoiningDate,
                IssuedAtUtc = now,
                Notes = offer.Notes
            };

            var pdfBytes = await _pdf.GenerateOfferLetterPdfAsync(document);
            var container = "offer-letters";
            var path = $"{offer.CandidateId}/{offer.Id}.pdf";
            await _storage.SaveFileAsync(container, path, pdfBytes, "application/pdf");

            offer.Status = OfferStatus.Sent;
            offer.IssuedAtUtc = now;
            offer.BlobContainer = container;
            offer.BlobPath = path;
            offer.UpdatedAtUtc = now;
            offer.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateOfferAsync(offer, cancellationToken);

            // Candidate moves to Offered once an offer is actually sent (not just drafted).
            var candidate = offer.Candidate!;
            candidate.Status = EMS.Domain.Enums.CandidateStatus.Offered;
            candidate.UpdatedAtUtc = now;
            candidate.UpdatedBy = _currentUser.UserId;
            await _repo.UpdateAsync(candidate, cancellationToken);

            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Sent offer {OfferId} to candidate {CandidateId}", offer.Id, offer.CandidateId);

            await _auditLogger.LogAsync("Offer", offer.Id, "Sent", ct: cancellationToken);
        }
    }
}
