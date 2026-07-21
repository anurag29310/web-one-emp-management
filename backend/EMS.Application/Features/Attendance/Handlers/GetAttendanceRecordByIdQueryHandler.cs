using EMS.Application.Features.Attendance.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class GetAttendanceRecordByIdQueryHandler : IRequestHandler<Queries.GetAttendanceRecordByIdQuery, AttendanceRecordDto?>
    {
        private readonly IAttendanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;

        public GetAttendanceRecordByIdQueryHandler(IAttendanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
        }

        public async Task<AttendanceRecordDto?> Handle(Queries.GetAttendanceRecordByIdQuery request, CancellationToken cancellationToken)
        {
            var record = await _repo.GetRecordByIdAsync(request.Id, cancellationToken);
            if (record == null) return null;

            if (!request.IsAdminOrHr)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                var requesterEmployeeId = requester?.EmployeeId;
                if (requesterEmployeeId == null) return null;

                if (record.EmployeeId != requesterEmployeeId.Value)
                {
                    if (!request.IsManager || !await _employeeRepo.IsDirectReportAsync(requesterEmployeeId.Value, record.EmployeeId, cancellationToken))
                        return null;
                }
            }

            return AttendanceRecordDto.FromEntity(record);
        }
    }
}
