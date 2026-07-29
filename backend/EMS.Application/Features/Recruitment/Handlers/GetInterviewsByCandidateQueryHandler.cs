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
    public class GetInterviewsByCandidateQueryHandler : IRequestHandler<GetInterviewsByCandidateQuery, IEnumerable<InterviewDto>>
    {
        private readonly IRecruitmentRepository _repo;

        public GetInterviewsByCandidateQueryHandler(IRecruitmentRepository repo) => _repo = repo;

        public async Task<IEnumerable<InterviewDto>> Handle(GetInterviewsByCandidateQuery request, CancellationToken cancellationToken)
        {
            var interviews = await _repo.GetInterviewsByCandidateAsync(request.CandidateId, cancellationToken);
            return interviews.Select(InterviewDto.FromEntity);
        }
    }
}
