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
    public class GetChecklistItemsQueryHandler : IRequestHandler<GetChecklistItemsQuery, IEnumerable<OnboardingChecklistItemDto>>
    {
        private readonly IRecruitmentRepository _repo;

        public GetChecklistItemsQueryHandler(IRecruitmentRepository repo) => _repo = repo;

        public async Task<IEnumerable<OnboardingChecklistItemDto>> Handle(GetChecklistItemsQuery request, CancellationToken cancellationToken)
        {
            var items = await _repo.GetChecklistItemsAsync(request.CandidateId, cancellationToken);
            return items.Select(OnboardingChecklistItemDto.FromEntity);
        }
    }
}
