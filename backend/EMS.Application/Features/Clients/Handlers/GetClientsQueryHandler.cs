using EMS.Application.Common.DTOs;
using EMS.Application.Features.Clients.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Clients.Handlers
{
    public class GetClientsQueryHandler : IRequestHandler<GetClientsQuery, PagedResult<ClientDto>>
    {
        private readonly IClientRepository _repo;

        public GetClientsQueryHandler(IClientRepository repo) => _repo = repo;

        public async Task<PagedResult<ClientDto>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var items = await _repo.GetAllAsync(page, pageSize, request.Search, request.IsActive, cancellationToken);
            var total = await _repo.CountAsync(request.Search, request.IsActive, cancellationToken);

            return PagedResult<ClientDto>.Create(
                items.Select(ClientDto.FromEntity),
                page,
                pageSize,
                total);
        }
    }
}
