using EMS.Application.Features.Assets.Queries;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assets.Handlers
{
    public class GetAssetByIdQueryHandler : IRequestHandler<GetAssetByIdQuery, Asset?>
    {
        private readonly IAssetRepository _repo;

        public GetAssetByIdQueryHandler(IAssetRepository repo) => _repo = repo;

        public async Task<Asset?> Handle(GetAssetByIdQuery request, CancellationToken cancellationToken) =>
            await _repo.GetByIdAsync(request.Id, cancellationToken);
    }
}
