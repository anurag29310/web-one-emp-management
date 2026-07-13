using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class GetHolidayByIdQueryHandler : IRequestHandler<Queries.GetHolidayByIdQuery, Holiday?>
    {
        private readonly ILeaveRepository _repo;

        public GetHolidayByIdQueryHandler(ILeaveRepository repo)
        {
            _repo = repo;
        }

        public async Task<Holiday?> Handle(Queries.GetHolidayByIdQuery request, CancellationToken cancellationToken)
            => await _repo.GetHolidayByIdAsync(request.Id, cancellationToken);
    }
}
