using EMS.Application.Features.Users.Queries;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Users.Handlers
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, User?>
    {
        private readonly IUserRepository _users;

        public GetUserByIdQueryHandler(IUserRepository users)
        {
            _users = users;
        }

        public async Task<User?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken) =>
            await _users.GetByIdAsync(request.Id, cancellationToken);
    }
}
