using EMS.Application.Features.Assets.DTOs;
using EMS.Application.Features.Assets.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assets.Handlers
{
    public class GetEmployeeAssetAssignmentsQueryHandler : IRequestHandler<GetEmployeeAssetAssignmentsQuery, IEnumerable<AssetAssignmentDto>>
    {
        private readonly IAssetRepository _repo;

        public GetEmployeeAssetAssignmentsQueryHandler(IAssetRepository repo) => _repo = repo;

        public async Task<IEnumerable<AssetAssignmentDto>> Handle(GetEmployeeAssetAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var assignments = await _repo.GetAssignmentsByEmployeeAsync(request.EmployeeId, cancellationToken);
            return assignments.Select(AssetAssignmentDto.FromEntity);
        }
    }
}
