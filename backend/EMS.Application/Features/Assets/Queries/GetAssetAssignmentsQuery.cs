using EMS.Application.Features.Assets.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Assets.Queries
{
    public class GetAssetAssignmentsQuery : IRequest<IEnumerable<AssetAssignmentDto>>
    {
        public Guid AssetId { get; set; }
    }
}
