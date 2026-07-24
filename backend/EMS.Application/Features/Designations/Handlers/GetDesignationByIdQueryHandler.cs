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

        public GetDesignationByIdQueryHandler(IDesignationRepository repo)
        {
            _repo = repo;
        }

        public async Task<Designation?> Handle(GetDesignationByIdQuery request, CancellationToken cancellationToken)
            => await _repo.GetByIdAsync(request.Id, cancellationToken);
    }
}
