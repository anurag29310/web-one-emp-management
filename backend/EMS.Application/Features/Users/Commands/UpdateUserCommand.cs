using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Users.Commands
{
    public class UpdateUserCommand : IRequest<User>
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public Guid? RoleId { get; set; }
        public Guid? EmployeeId { get; set; }
    }
}
