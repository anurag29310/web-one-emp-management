using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Queries
{
    public class DownloadCandidateAttachmentQuery : IRequest<DownloadCandidateAttachmentResult?>
    {
        public Guid AttachmentId { get; set; }
    }

    public class DownloadCandidateAttachmentResult
    {
        public byte[] Content { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public string FileName { get; set; } = null!;
    }
}
