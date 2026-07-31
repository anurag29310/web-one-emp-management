using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Designations.Handlers
{
    public class GetDesignationByIdQueryHandler : IRequestHandler<GetDesignationByIdQuery, Designation?>
    {
        private readonly IDesignationRepository _repo;
        private readonly ICurrentUserService _currentUser;

        public GetDesignationByIdQueryHandler(IDesignationRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<Designation?> Handle(GetDesignationByIdQuery request, CancellationToken cancellationToken)
            => await _repo.GetByIdAsync(request.Id, _currentUser.CompanyId!.Value, cancellationToken);
    }
}
