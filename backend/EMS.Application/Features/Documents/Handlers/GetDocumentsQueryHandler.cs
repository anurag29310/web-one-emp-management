using EMS.Application.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Documents.Handlers
{
    public class GetDocumentsQueryHandler : IRequestHandler<Queries.GetDocumentsQuery, IEnumerable<DocumentDto>>
    {
        private readonly IDocumentRepository _repo;

        public GetDocumentsQueryHandler(IDocumentRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<DocumentDto>> Handle(Queries.GetDocumentsQuery request, CancellationToken cancellationToken)
        {
            var docs = await _repo.GetForEmployeeAsync(request.EmployeeId, request.Page, request.PageSize, request.Search, request.DocumentType);
            return docs.Select(d => new DocumentDto
            {
                Id = d.Id,
                EmployeeId = d.EmployeeId,
                DocumentType = d.DocumentType,
                OriginalFileName = d.OriginalFileName,
                ContentType = d.ContentType,
                FileSizeBytes = d.FileSizeBytes,
                UploadedAtUtc = d.UploadedAtUtc,
                ExpiresAtUtc = d.ExpiresAtUtc
            });
        }
    }
}
