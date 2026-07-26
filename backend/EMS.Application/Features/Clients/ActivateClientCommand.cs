using MediatR;
using System;

namespace EMS.Application.Features.Clients
{
    public class ActivateClientCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
