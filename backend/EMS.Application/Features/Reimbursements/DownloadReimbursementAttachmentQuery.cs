using MediatR;
using System;

namespace EMS.Application.Features.Reimbursements
{
    public class DownloadReimbursementAttachmentQuery : IRequest<DownloadReimbursementAttachmentResult?>
    {
        public Guid AttachmentId { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin role and may download any attachment.</summary>
        public bool IsPrivileged { get; set; }
    }

    public class DownloadReimbursementAttachmentResult
    {
        public byte[] Content { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public string FileName { get; set; } = null!;
    }
}
