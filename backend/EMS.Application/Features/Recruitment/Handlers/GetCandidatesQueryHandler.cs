using EMS.Application.Common.DTOs;
using EMS.Application.Features.Recruitment.DTOs;
using EMS.Application.Features.Recruitment.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class GetCandidatesQueryHandler : IRequestHandler<GetCandidatesQuery, PagedResult<CandidateDto>>
    {
        private readonly IRecruitmentRepository _repo;

        public GetCandidatesQueryHandler(IRecruitmentRepository repo) => _repo = repo;

        public async Task<PagedResult<CandidateDto>> Handle(GetCandidatesQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var items = await _repo.GetAllAsync(page, pageSize, request.Status, request.DesignationId, request.Search, cancellationToken);
            var total = await _repo.CountAsync(request.Status, request.DesignationId, request.Search, cancellationToken);

            return PagedResult<CandidateDto>.Create(
                items.Select(CandidateDto.FromEntity),
                page,
                pageSize,
                total);
        }
    }
}
