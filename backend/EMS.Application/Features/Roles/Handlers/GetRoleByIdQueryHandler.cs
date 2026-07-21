using EMS.Application.Features.Roles.Queries;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Roles.Handlers
{
    public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, Role?>
    {
        private readonly IRoleRepository _repo;

        public GetRoleByIdQueryHandler(IRoleRepository repo)
        {
            _repo = repo;
        }

        public async Task<Role?> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken) =>
            await _repo.GetByIdAsync(request.Id, cancellationToken);
    }
}
