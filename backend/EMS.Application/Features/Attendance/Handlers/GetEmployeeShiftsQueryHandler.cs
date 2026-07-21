using EMS.Application.Features.Attendance.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class GetEmployeeShiftsQueryHandler : IRequestHandler<Queries.GetEmployeeShiftsQuery, IEnumerable<EmployeeShiftDto>>
    {
        private readonly IAttendanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;

        public GetEmployeeShiftsQueryHandler(IAttendanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
        }

        public async Task<IEnumerable<EmployeeShiftDto>> Handle(Queries.GetEmployeeShiftsQuery request, CancellationToken cancellationToken)
        {
            if (!request.IsAdminOrHr)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                var requesterEmployeeId = requester?.EmployeeId;

                var isSelf = requesterEmployeeId.HasValue && requesterEmployeeId.Value == request.EmployeeId;
                var isTeamMember = !isSelf && request.IsManager && requesterEmployeeId.HasValue
                    && await _employeeRepo.IsDirectReportAsync(requesterEmployeeId.Value, request.EmployeeId, cancellationToken);

                if (!isSelf && !isTeamMember)
                    throw new UnauthorizedAccessException("You can only view your own or your team's shift assignments.");
            }

            var assignments = await _repo.GetEmployeeShiftsAsync(request.EmployeeId, cancellationToken);
            return assignments.Select(EmployeeShiftDto.FromEntity);
        }
    }
}
