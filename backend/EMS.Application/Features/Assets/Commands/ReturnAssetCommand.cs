using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Assets.Commands
{
    public class ReturnAssetCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? ConditionAtReturn { get; set; }
        public string? Notes { get; set; }

        /// <summary>The asset's resulting status. Defaults to Available; set to UnderRepair/Retired/Lost
        /// when the returned condition warrants it. Must not be Assigned.</summary>
        public AssetStatus ResultingAssetStatus { get; set; } = AssetStatus.Available;
    }
}
