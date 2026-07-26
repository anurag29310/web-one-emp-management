using MediatR;
using System;

namespace EMS.Application.Features.Clients
{
    public class DeleteClientCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
