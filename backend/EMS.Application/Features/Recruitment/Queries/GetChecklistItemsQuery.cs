using EMS.Application.Features.Recruitment.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Recruitment.Queries
{
    public class GetChecklistItemsQuery : IRequest<IEnumerable<OnboardingChecklistItemDto>>
    {
        public Guid CandidateId { get; set; }
    }
}
