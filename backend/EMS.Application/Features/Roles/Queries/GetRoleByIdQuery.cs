using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Roles.Queries
{
    public class GetRoleByIdQuery : IRequest<Role?>
    {
        public Guid Id { get; set; }
    }
}
