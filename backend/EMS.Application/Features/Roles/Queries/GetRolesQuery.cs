using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;

namespace EMS.Application.Features.Roles.Queries
{
    public class GetRolesQuery : IRequest<IEnumerable<Role>>
    {
        public bool IncludeDeleted { get; set; }
    }
}
