using MediatR;
using System;

namespace EMS.Application.Features.Documents.Commands
{
    public class DeleteDocumentCommand : IRequest
    {
        public Guid DocumentId { get; set; }
        public Guid? DeletedBy { get; set; }
    }
}
