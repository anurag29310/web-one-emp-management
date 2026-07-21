using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Roles.Commands
{
    public class UpdateRoleCommand : IRequest<Role>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
