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
    public class GetAttendanceRecordsQueryHandler : IRequestHandler<Queries.GetAttendanceRecordsQuery, PagedResult<AttendanceRecordDto>>
    {
        private readonly IAttendanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;

        public GetAttendanceRecordsQueryHandler(IAttendanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
        }

        public async Task<PagedResult<AttendanceRecordDto>> Handle(Queries.GetAttendanceRecordsQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var filter = new DTOs.AttendanceRecordFilter
            {
                DepartmentId = request.DepartmentId,
                DateFrom = request.DateFrom,
                DateTo = request.DateTo,
                Status = request.Status,
                IsLateArrival = request.IsLateArrival,
                IsEarlyLeave = request.IsEarlyLeave
            };

            if (request.IsAdminOrHr)
            {
                filter.EmployeeId = request.EmployeeId;
                if (request.ManagerId.HasValue)
                    filter.EmployeeIdsScope = await _employeeRepo.GetDirectReportIdsAsync(request.ManagerId.Value, cancellationToken);
            }
            else
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                var requesterEmployeeId = requester?.EmployeeId;
                if (requesterEmployeeId == null)
                    return PagedResult<AttendanceRecordDto>.Create(Enumerable.Empty<AttendanceRecordDto>(), page, pageSize, 0);

                if (request.IsManager)
                {
                    var reportIds = (await _employeeRepo.GetDirectReportIdsAsync(requesterEmployeeId.Value, cancellationToken)).ToList();
                    reportIds.Add(requesterEmployeeId.Value);

                    if (request.EmployeeId.HasValue)
                    {
                        if (!reportIds.Contains(request.EmployeeId.Value))
                            throw new UnauthorizedAccessException("You can only view attendance records for your own team.");
                        filter.EmployeeId = request.EmployeeId;
                    }
                    else
                    {
                        filter.EmployeeIdsScope = reportIds;
                    }
                }
                else
                {
                    // Plain Employee callers are always scoped to their own record, regardless of
                    // any employeeId/departmentId/managerId filter supplied on the query string.
                    filter.EmployeeId = requesterEmployeeId;
                }
            }

            var items = await _repo.GetRecordsAsync(filter, page, pageSize, cancellationToken);
            var total = await _repo.CountRecordsAsync(filter, cancellationToken);

            return PagedResult<AttendanceRecordDto>.Create(
                items.Select(AttendanceRecordDto.FromEntity),
                page, pageSize, total);
        }
    }
}
