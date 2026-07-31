using EMS.Application.Common.DTOs;
using EMS.Application.Features.Companies.DTOs;
using EMS.Domain.Enums;
using MediatR;

namespace EMS.Application.Features.Companies.Queries
{
    public class GetCompaniesQuery : IRequest<PagedResult<CompanyDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public CompanyStatus? Status { get; set; }
        public string? Search { get; set; }
    }
}
