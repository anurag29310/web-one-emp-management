using MediatR;
using System;

namespace EMS.Application.Features.Clients
{
    public class ArchiveClientCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
