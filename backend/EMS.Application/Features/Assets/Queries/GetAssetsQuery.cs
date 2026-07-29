using EMS.Application.Common.DTOs;
using EMS.Application.Features.Assets.DTOs;
using EMS.Domain.Enums;
using MediatR;

namespace EMS.Application.Features.Assets.Queries
{
    public class GetAssetsQuery : IRequest<PagedResult<AssetDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public AssetStatus? Status { get; set; }
        public string? Category { get; set; }
        public string? Search { get; set; }
    }
}
