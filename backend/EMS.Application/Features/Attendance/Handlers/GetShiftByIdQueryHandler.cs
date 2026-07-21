using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class GetShiftByIdQueryHandler : IRequestHandler<Queries.GetShiftByIdQuery, Shift?>
    {
        private readonly IAttendanceRepository _repo;

        public GetShiftByIdQueryHandler(IAttendanceRepository repo)
        {
            _repo = repo;
        }

        public async Task<Shift?> Handle(Queries.GetShiftByIdQuery request, CancellationToken cancellationToken) =>
            await _repo.GetShiftByIdAsync(request.Id, cancellationToken);
    }
}
