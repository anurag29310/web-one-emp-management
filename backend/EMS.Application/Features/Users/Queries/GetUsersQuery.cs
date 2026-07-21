using EMS.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Users.Queries
{
    public class GetUsersQuery : IRequest<IEnumerable<User>>
    {
        public bool IncludeDeleted { get; set; }
        public Guid? RoleId { get; set; }
        public bool? IsActive { get; set; }
    }
}
