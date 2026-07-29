using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class UploadCandidateAttachmentCommand : IRequest<Guid>
    {
        public Guid CandidateId { get; set; }
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }
    }
}
