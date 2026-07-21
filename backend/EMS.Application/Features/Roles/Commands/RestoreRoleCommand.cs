using MediatR;
using System;

namespace EMS.Application.Features.Roles.Commands
{
    public class RestoreRoleCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
