using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Teams
{
    public class GetTeamByIdQuery : IRequest<Team?>
    {
        public Guid Id { get; set; }
    }
}
