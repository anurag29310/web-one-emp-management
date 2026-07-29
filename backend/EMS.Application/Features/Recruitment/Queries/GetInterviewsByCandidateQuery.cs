using EMS.Application.Features.Recruitment.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Recruitment.Queries
{
    public class GetInterviewsByCandidateQuery : IRequest<IEnumerable<InterviewDto>>
    {
        public Guid CandidateId { get; set; }
    }
}
