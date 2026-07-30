using EMS.Application.Common.DTOs;
using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Exports.Queries
{
    /// <summary>Export candidates matching the same filters as <see cref="Recruitment.Queries.GetCandidatesQuery"/> to Excel.</summary>
    public class ExportCandidatesQuery : IRequest<ExportFileResult>
    {
        public CandidateStatus? Status { get; set; }
        public Guid? DesignationId { get; set; }
        public string? Search { get; set; }
    }
}
