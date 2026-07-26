using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Clients
{
    public class GetClientByIdQuery : IRequest<Client?>
    {
        public Guid Id { get; set; }
    }
}
