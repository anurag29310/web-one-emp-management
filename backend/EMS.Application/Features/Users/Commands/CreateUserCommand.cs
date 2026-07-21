using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Users.Commands
{
    public class CreateUserCommand : IRequest<User>
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string TemporaryPassword { get; set; } = null!;
        public Guid? RoleId { get; set; }
        public Guid? EmployeeId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
