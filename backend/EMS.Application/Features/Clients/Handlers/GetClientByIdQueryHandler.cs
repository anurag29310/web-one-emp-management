using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Clients.Handlers
{
    public class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, Client?>
    {
        private readonly IClientRepository _repo;

        public GetClientByIdQueryHandler(IClientRepository repo) => _repo = repo;

        public async Task<Client?> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
            => await _repo.GetByIdAsync(request.Id, cancellationToken);
    }
}
