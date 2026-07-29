using EMS.Application.Common.DTOs;
using EMS.Application.Features.Assets.DTOs;
using EMS.Application.Features.Assets.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assets.Handlers
{
    public class GetAssetsQueryHandler : IRequestHandler<GetAssetsQuery, PagedResult<AssetDto>>
    {
        private readonly IAssetRepository _repo;

        public GetAssetsQueryHandler(IAssetRepository repo) => _repo = repo;

        public async Task<PagedResult<AssetDto>> Handle(GetAssetsQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var items = await _repo.GetAllAsync(page, pageSize, request.Status, request.Category, request.Search, cancellationToken);
            var total = await _repo.CountAsync(request.Status, request.Category, request.Search, cancellationToken);

            return PagedResult<AssetDto>.Create(
                items.Select(AssetDto.FromEntity),
                page,
                pageSize,
                total);
        }
    }
}
