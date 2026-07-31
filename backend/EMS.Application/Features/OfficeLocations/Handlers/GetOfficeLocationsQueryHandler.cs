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
        private readonly ICurrentUserService _currentUser;

        public GetOfficeLocationsQueryHandler(IOfficeLocationRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<OfficeLocation>> Handle(GetOfficeLocationsQuery request, CancellationToken cancellationToken)
            => await _repo.GetAllAsync(_currentUser.CompanyId!.Value, cancellationToken);
    }
}
