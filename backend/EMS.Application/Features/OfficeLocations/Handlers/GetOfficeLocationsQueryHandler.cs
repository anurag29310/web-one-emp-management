using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.OfficeLocations.Handlers
{
    public class GetOfficeLocationsQueryHandler : IRequestHandler<GetOfficeLocationsQuery, IEnumerable<OfficeLocation>>
    {
        private readonly IOfficeLocationRepository _repo;

        public GetOfficeLocationsQueryHandler(IOfficeLocationRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<OfficeLocation>> Handle(GetOfficeLocationsQuery request, CancellationToken cancellationToken)
            => await _repo.GetAllAsync(cancellationToken);
    }
}
