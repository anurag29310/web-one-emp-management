using EMS.Application.Common.DTOs;
using EMS.Application.Features.Recruitment.DTOs;
using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Queries
{
    public class GetCandidatesQuery : IRequest<PagedResult<CandidateDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public CandidateStatus? Status { get; set; }
        public Guid? DesignationId { get; set; }
        public string? Search { get; set; }
    }
}
