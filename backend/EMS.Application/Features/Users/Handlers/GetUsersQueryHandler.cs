using EMS.Application.Features.Users.Queries;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Users.Handlers
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IEnumerable<User>>
    {
        private readonly IUserRepository _users;

        public GetUsersQueryHandler(IUserRepository users)
        {
            _users = users;
        }

        public async Task<IEnumerable<User>> Handle(GetUsersQuery request, CancellationToken cancellationToken) =>
            await _users.GetAllAsync(request.IncludeDeleted, request.RoleId, request.IsActive, cancellationToken);
    }
}
