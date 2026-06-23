using EMS.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Documents.Queries
{
    public class GetDocumentsQuery : IRequest<IEnumerable<DocumentDto>>
    {
        public Guid EmployeeId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public string? DocumentType { get; set; }
    }
}
