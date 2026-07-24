using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Designations.Handlers
{
    public class GetDesignationsQueryHandler : IRequestHandler<GetDesignationsQuery, IEnumerable<Designation>>
    {
        private readonly IDesignationRepository _repo;

        public GetDesignationsQueryHandler(IDesignationRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Designation>> Handle(GetDesignationsQuery request, CancellationToken cancellationToken)
            => await _repo.GetAllAsync(cancellationToken);
    }
}
