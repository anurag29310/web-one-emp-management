using MediatR;
using System;

namespace EMS.Application.Features.Tasks
{
    public class UploadTaskAttachmentCommand : IRequest<Guid>
    {
        public Guid TaskId { get; set; }
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin role and may act on behalf of the assignee.</summary>
        public bool IsPrivileged { get; set; }
    }
}
