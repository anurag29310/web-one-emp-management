using EMS.Application.Features.Recruitment.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class DownloadOfferQueryHandler : IRequestHandler<DownloadOfferQuery, DownloadOfferResult?>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly IFileStorageService _storage;

        public DownloadOfferQueryHandler(IRecruitmentRepository repo, IFileStorageService storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public async Task<DownloadOfferResult?> Handle(DownloadOfferQuery request, CancellationToken cancellationToken)
        {
            var offer = await _repo.GetOfferByIdAsync(request.Id, cancellationToken);
            if (offer?.BlobContainer == null || offer.BlobPath == null) return null;

            var content = await _storage.GetFileAsync(offer.BlobContainer, offer.BlobPath);
            if (content == null) return null;

            return new DownloadOfferResult
            {
                Content = content,
                ContentType = "application/pdf",
                FileName = $"{offer.OfferNumber}.pdf"
            };
        }
    }
}
