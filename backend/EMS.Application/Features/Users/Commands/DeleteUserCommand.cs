using MediatR;
using System;

namespace EMS.Application.Features.Users.Commands
{
    public class DeleteUserCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
