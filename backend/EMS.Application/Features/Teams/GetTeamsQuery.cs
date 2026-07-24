using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;

namespace EMS.Application.Features.Teams
{
    public class GetTeamsQuery : IRequest<IEnumerable<Team>>
    {
    }
}
