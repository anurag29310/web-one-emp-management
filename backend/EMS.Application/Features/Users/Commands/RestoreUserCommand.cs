using MediatR;
using System;

namespace EMS.Application.Features.Users.Commands
{
    public class RestoreUserCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
