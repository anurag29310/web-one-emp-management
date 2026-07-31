using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class GetShiftsQueryHandler : IRequestHandler<Queries.GetShiftsQuery, IEnumerable<Shift>>
    {
        private readonly IAttendanceRepository _repo;
        private readonly ICurrentUserService _currentUser;

        public GetShiftsQueryHandler(IAttendanceRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<Shift>> Handle(Queries.GetShiftsQuery request, CancellationToken cancellationToken) =>
            await _repo.GetShiftsAsync(_currentUser.CompanyId!.Value, cancellationToken);
    }
}
