using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Assets.Commands
{
    /// <summary>Sets an asset's status outside of the assign/return flow — e.g. Available →
    /// UnderRepair, UnderRepair → Available, either → Retired/Lost. Rejected if the target status
    /// is Assigned (that only ever happens via AssignAssetCommand) or if the asset currently has an
    /// open assignment (it must be returned first — see ReturnAssetCommand).</summary>
    public class UpdateAssetStatusCommand : IRequest
    {
        public Guid Id { get; set; }
        public AssetStatus Status { get; set; }
    }
}
