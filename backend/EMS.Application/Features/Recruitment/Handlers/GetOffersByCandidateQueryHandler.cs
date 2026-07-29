using EMS.Application.Features.Recruitment.DTOs;
using EMS.Application.Features.Recruitment.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class GetOffersByCandidateQueryHandler : IRequestHandler<GetOffersByCandidateQuery, IEnumerable<OfferDto>>
    {
        private readonly IRecruitmentRepository _repo;

        public GetOffersByCandidateQueryHandler(IRecruitmentRepository repo) => _repo = repo;

        public async Task<IEnumerable<OfferDto>> Handle(GetOffersByCandidateQuery request, CancellationToken cancellationToken)
        {
            var offers = await _repo.GetOffersByCandidateAsync(request.CandidateId, cancellationToken);
            return offers.Select(OfferDto.FromEntity);
        }
    }
}
