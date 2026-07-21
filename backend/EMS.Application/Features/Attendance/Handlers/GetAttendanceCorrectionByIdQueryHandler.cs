using EMS.Application.Features.Attendance.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class GetAttendanceCorrectionByIdQueryHandler : IRequestHandler<Queries.GetAttendanceCorrectionByIdQuery, AttendanceCorrectionDto?>
    {
        private readonly IAttendanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;

        public GetAttendanceCorrectionByIdQueryHandler(IAttendanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
        }

        public async Task<AttendanceCorrectionDto?> Handle(Queries.GetAttendanceCorrectionByIdQuery request, CancellationToken cancellationToken)
        {
            var correction = await _repo.GetCorrectionByIdAsync(request.Id, cancellationToken);
            if (correction == null) return null;

            if (!request.IsAdminOrHr)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                var requesterEmployeeId = requester?.EmployeeId;
                if (requesterEmployeeId == null) return null;

                if (correction.RequestedByEmployeeId != requesterEmployeeId.Value)
                {
                    if (!request.IsManager || !await _employeeRepo.IsDirectReportAsync(requesterEmployeeId.Value, correction.RequestedByEmployeeId, cancellationToken))
                        return null;
                }
            }

            return AttendanceCorrectionDto.FromEntity(correction);
        }
    }
}
