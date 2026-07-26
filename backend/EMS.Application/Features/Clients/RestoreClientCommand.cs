using MediatR;
using System;

namespace EMS.Application.Features.Clients
{
    /// <summary>Reverses whichever terminal state applies: un-deletes a soft-deleted client, or un-archives an archived one.</summary>
    public class RestoreClientCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
