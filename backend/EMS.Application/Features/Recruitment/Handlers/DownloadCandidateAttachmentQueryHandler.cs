using EMS.Application.Features.Recruitment.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class DownloadCandidateAttachmentQueryHandler : IRequestHandler<DownloadCandidateAttachmentQuery, DownloadCandidateAttachmentResult?>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly IFileStorageService _storage;

        public DownloadCandidateAttachmentQueryHandler(IRecruitmentRepository repo, IFileStorageService storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public async Task<DownloadCandidateAttachmentResult?> Handle(DownloadCandidateAttachmentQuery request, CancellationToken cancellationToken)
        {
            var attachment = await _repo.GetAttachmentByIdAsync(request.AttachmentId, cancellationToken);
            if (attachment == null) return null;

            var content = await _storage.GetFileAsync(attachment.BlobContainer, attachment.BlobPath);
            if (content == null) return null;

            return new DownloadCandidateAttachmentResult
            {
                Content = content,
                ContentType = attachment.ContentType,
                FileName = attachment.OriginalFileName
            };
        }
    }
}
