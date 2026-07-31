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
        private readonly ICurrentUserService _currentUser;

        public GetShiftByIdQueryHandler(IAttendanceRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<Shift?> Handle(Queries.GetShiftByIdQuery request, CancellationToken cancellationToken) =>
            await _repo.GetShiftByIdAsync(request.Id, _currentUser.CompanyId!.Value, cancellationToken);
    }
}
