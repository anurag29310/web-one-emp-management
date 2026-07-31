using EMS.Application.Common.DTOs;
using EMS.Application.Features.Companies.DTOs;
using EMS.Application.Features.Companies.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Companies.Handlers
{
    public class GetCompaniesQueryHandler : IRequestHandler<GetCompaniesQuery, PagedResult<CompanyDto>>
    {
        private readonly ICompanyRepository _repo;

        public GetCompaniesQueryHandler(ICompanyRepository repo) => _repo = repo;

        public async Task<PagedResult<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var items = await _repo.GetAllAsync(page, pageSize, request.Status, request.Search, cancellationToken);
            var total = await _repo.CountAsync(request.Status, request.Search, cancellationToken);

            return PagedResult<CompanyDto>.Create(items.Select(CompanyDto.FromEntity), page, pageSize, total);
        }
    }
}
