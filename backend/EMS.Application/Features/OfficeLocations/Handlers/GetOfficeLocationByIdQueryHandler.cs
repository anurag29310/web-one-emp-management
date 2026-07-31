using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.OfficeLocations.Handlers
{
    public class GetOfficeLocationByIdQueryHandler : IRequestHandler<GetOfficeLocationByIdQuery, OfficeLocation?>
    {
        private readonly IOfficeLocationRepository _repo;
        private readonly ICurrentUserService _currentUser;

        public GetOfficeLocationByIdQueryHandler(IOfficeLocationRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<OfficeLocation?> Handle(GetOfficeLocationByIdQuery request, CancellationToken cancellationToken)
            => await _repo.GetByIdAsync(request.Id, _currentUser.CompanyId!.Value, cancellationToken);
    }
}
