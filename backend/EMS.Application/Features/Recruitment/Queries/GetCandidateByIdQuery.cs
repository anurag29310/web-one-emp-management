using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Queries
{
    public class GetCandidateByIdQuery : IRequest<Candidate?>
    {
        public Guid Id { get; set; }
    }
}
