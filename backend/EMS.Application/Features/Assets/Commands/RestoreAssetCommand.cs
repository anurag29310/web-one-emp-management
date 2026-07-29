using MediatR;
using System;

namespace EMS.Application.Features.Assets.Commands
{
    public class RestoreAssetCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
