using MediatR;
using System;

namespace EMS.Application.Features.Teams
{
    public class DeleteTeamCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
