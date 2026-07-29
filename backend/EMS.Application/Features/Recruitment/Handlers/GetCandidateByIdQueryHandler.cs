using EMS.Application.Features.Recruitment.Queries;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class GetCandidateByIdQueryHandler : IRequestHandler<GetCandidateByIdQuery, Candidate?>
    {
        private readonly IRecruitmentRepository _repo;

        public GetCandidateByIdQueryHandler(IRecruitmentRepository repo) => _repo = repo;

        public async Task<Candidate?> Handle(GetCandidateByIdQuery request, CancellationToken cancellationToken) =>
            await _repo.GetByIdAsync(request.Id, cancellationToken);
    }
}
