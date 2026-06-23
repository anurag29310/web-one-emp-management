using MediatR;
using System;

namespace EMS.Application.Features.Documents.Queries
{
    public class DownloadDocumentQuery : IRequest<DownloadDocumentResult>
    {
        public Guid DocumentId { get; set; }
    }

    public class DownloadDocumentResult
    {
        public byte[] Content { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public string FileName { get; set; } = null!;
    }
}
