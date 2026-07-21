using EMS.Domain.Entities;
using MediatR;

namespace EMS.Application.Features.Roles.Commands
{
    public class CreateRoleCommand : IRequest<Role>
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
