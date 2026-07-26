using MediatR;
using System;

namespace EMS.Application.Features.Clients
{
    public class DeactivateClientCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
