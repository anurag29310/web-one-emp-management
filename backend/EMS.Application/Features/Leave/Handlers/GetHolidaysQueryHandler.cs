using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class GetHolidaysQueryHandler : IRequestHandler<Queries.GetHolidaysQuery, IEnumerable<Holiday>>
    {
        private readonly ILeaveRepository _repo;

        public GetHolidaysQueryHandler(ILeaveRepository repo) => _repo = repo;

        public async Task<IEnumerable<Holiday>> Handle(Queries.GetHolidaysQuery request, CancellationToken cancellationToken) =>
            await _repo.GetHolidaysAsync(request.Year, cancellationToken);
    }
}
