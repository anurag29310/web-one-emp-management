using EMS.Application.Common.DTOs;
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
    public class GetAttendanceCorrectionsQueryHandler : IRequestHandler<Queries.GetAttendanceCorrectionsQuery, PagedResult<AttendanceCorrectionDto>>
    {
        private readonly IAttendanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;

        public GetAttendanceCorrectionsQueryHandler(IAttendanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
        }

        public async Task<PagedResult<AttendanceCorrectionDto>> Handle(Queries.GetAttendanceCorrectionsQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var employeeId = request.EmployeeId;
            IEnumerable<Guid>? scope = null;

            if (!request.IsAdminOrHr)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                var requesterEmployeeId = requester?.EmployeeId;
                if (requesterEmployeeId == null || !request.IsManager)
                    return PagedResult<AttendanceCorrectionDto>.Create(Enumerable.Empty<AttendanceCorrectionDto>(), page, pageSize, 0);

                var reportIds = (await _employeeRepo.GetDirectReportIdsAsync(requesterEmployeeId.Value, cancellationToken)).ToList();
                reportIds.Add(requesterEmployeeId.Value);

                if (employeeId.HasValue)
                {
                    if (!reportIds.Contains(employeeId.Value))
                        throw new UnauthorizedAccessException("You can only view corrections for your own team.");
                }
                else
                {
                    scope = reportIds;
                }
            }

            var items = await _repo.GetCorrectionsAsync(page, pageSize, employeeId, scope, request.Status, cancellationToken);
            var total = await _repo.CountCorrectionsAsync(employeeId, scope, request.Status, cancellationToken);

            return PagedResult<AttendanceCorrectionDto>.Create(
                items.Select(AttendanceCorrectionDto.FromEntity),
                page, pageSize, total);
        }
    }
}
