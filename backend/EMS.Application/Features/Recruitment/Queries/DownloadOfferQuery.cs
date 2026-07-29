using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Queries
{
    public class DownloadOfferQuery : IRequest<DownloadOfferResult?>
    {
        public Guid Id { get; set; }
    }

    public class DownloadOfferResult
    {
        public byte[] Content { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public string FileName { get; set; } = null!;
    }
}
