using MediatR;
using System;

namespace EMS.Application.Features.Documents.Commands
{
    public class UploadDocumentCommand : IRequest<Guid>
    {
        public Guid EmployeeId { get; set; }
        public string DocumentType { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public byte[] Content { get; set; } = null!;
        public DateTime? ExpiresAtUtc { get; set; }
        public Guid? UploadedBy { get; set; }
    }
}
