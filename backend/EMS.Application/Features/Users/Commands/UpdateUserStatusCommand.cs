using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Users.Commands
{
    public class UpdateUserStatusCommand : IRequest<User>
    {
        public Guid Id { get; set; }
        public bool IsActive { get; set; }
    }
}
