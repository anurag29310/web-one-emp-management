using EMS.Application.Features.Recruitment.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Recruitment.Queries
{
    public class GetOffersByCandidateQuery : IRequest<IEnumerable<OfferDto>>
    {
        public Guid CandidateId { get; set; }
    }
}
