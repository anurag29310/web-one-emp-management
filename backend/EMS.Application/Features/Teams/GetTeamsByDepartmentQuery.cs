using EMS.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Teams
{
    public class GetTeamsByDepartmentQuery : IRequest<IEnumerable<Team>>
    {
        public Guid DepartmentId { get; set; }
    }
}
