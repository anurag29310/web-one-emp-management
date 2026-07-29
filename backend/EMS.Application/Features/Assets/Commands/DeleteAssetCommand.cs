using MediatR;
using System;

namespace EMS.Application.Features.Assets.Commands
{
    public class DeleteAssetCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
