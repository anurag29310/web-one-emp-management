using EMS.Application.Common.DTOs;
using EMS.Application.Features.Attendance.DTOs;
using EMS.Application.Features.Exports.Queries;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Exports.Handlers
{
    public class ExportAttendanceQueryHandler : IRequestHandler<ExportAttendanceQuery, ExportFileResult>
    {
        private static readonly string[] Headers =
        {
            "Employee Code", "Employee Name", "Attendance Date", "Check-In (UTC)", "Check-Out (UTC)",
            "Status", "Late Arrival", "Early Leave", "Total Work Minutes", "Notes"
        };

        private readonly IAttendanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IExcelExportService _excelService;

        public ExportAttendanceQueryHandler(
            IAttendanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo, IExcelExportService excelService)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
            _excelService = excelService;
        }

        public async Task<ExportFileResult> Handle(ExportAttendanceQuery request, CancellationToken cancellationToken)
        {
            var filter = new AttendanceRecordFilter
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
                // Only Admin, HR, and Manager can reach this handler (policy-gated), so a
                // non-Admin/HR caller here is always a Manager, scoped to their own team.
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                var requesterEmployeeId = requester?.EmployeeId;
                if (requesterEmployeeId == null)
                    return await BuildResult(Enumerable.Empty<AttendanceRecord>(), cancellationToken);

                var reportIds = (await _employeeRepo.GetDirectReportIdsAsync(requesterEmployeeId.Value, cancellationToken)).ToList();
                reportIds.Add(requesterEmployeeId.Value);

                if (request.EmployeeId.HasValue)
                {
                    if (!reportIds.Contains(request.EmployeeId.Value))
                        throw new UnauthorizedAccessException("You can only export attendance records for your own team.");
                    filter.EmployeeId = request.EmployeeId;
                }
                else
                {
                    filter.EmployeeIdsScope = reportIds;
                }
            }

            var records = await _repo.GetAllRecordsAsync(filter, cancellationToken);
            return await BuildResult(records, cancellationToken);
        }

        private async Task<ExportFileResult> BuildResult(IEnumerable<AttendanceRecord> records, CancellationToken ct)
        {
            var recordList = records.ToList();
            var employeeIds = recordList.Select(r => r.EmployeeId).Distinct().ToList();
            var employeesById = (await _employeeRepo.GetByIdsAsync(employeeIds, ct)).ToDictionary(e => e.Id);

            var rows = new List<IReadOnlyList<object?>>();
            foreach (var record in recordList)
            {
                var dto = AttendanceRecordDto.FromEntity(record);
                employeesById.TryGetValue(record.EmployeeId, out var employee);

                rows.Add(new List<object?>
                {
                    employee?.EmployeeCode, employee == null ? null : $"{employee.FirstName} {employee.LastName}",
                    dto.AttendanceDate, dto.CheckInAtUtc, dto.CheckOutAtUtc,
                    dto.Status, dto.IsLateArrival, dto.IsEarlyLeave, dto.TotalWorkMinutes, dto.Notes
                });
            }

            var bytes = await _excelService.GenerateAsync("Attendance", Headers, rows);
            var fileName = $"attendance_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

            return new ExportFileResult
            {
                Content = bytes,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = fileName
            };
        }
    }
}
