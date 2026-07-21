using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Users.Queries
{
    public class GetUserByIdQuery : IRequest<User?>
    {
        public Guid Id { get; set; }
    }
}
