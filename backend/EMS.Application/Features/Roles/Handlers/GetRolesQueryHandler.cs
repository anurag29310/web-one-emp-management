using EMS.Application.Features.Roles.Queries;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Roles.Handlers
{
    public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IEnumerable<Role>>
    {
        private readonly IRoleRepository _repo;

        public GetRolesQueryHandler(IRoleRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Role>> Handle(GetRolesQuery request, CancellationToken cancellationToken) =>
            await _repo.GetAllAsync(request.IncludeDeleted, cancellationToken);
    }
}
