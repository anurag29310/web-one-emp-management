using EMS.Application.Features.Assets.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Assets.Queries
{
    public class GetEmployeeAssetAssignmentsQuery : IRequest<IEnumerable<AssetAssignmentDto>>
    {
        public Guid EmployeeId { get; set; }
    }
}
