using MediatR;
using System;

namespace EMS.Application.Features.Roles.Commands
{
    public class DeleteRoleCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
