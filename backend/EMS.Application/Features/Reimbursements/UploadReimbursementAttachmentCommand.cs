using MediatR;
using System;

namespace EMS.Application.Features.Reimbursements
{
    public class UploadReimbursementAttachmentCommand : IRequest<Guid>
    {
        public Guid ReimbursementId { get; set; }
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }
    }
}
