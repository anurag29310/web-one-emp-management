using EMS.Application.Common.DTOs;
using EMS.Application.Features.Exports.Queries;
using EMS.Application.Features.Leave.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Exports.Handlers
{
    /// <summary>Only Admin, HR, and Manager (all treated as privileged, matching
    /// <see cref="Leave.Handlers.GetLeavesQueryHandler"/>) can reach this handler, so no
    /// self-service scoping is applied.</summary>
    public class ExportLeaveRequestsQueryHandler : IRequestHandler<ExportLeaveRequestsQuery, ExportFileResult>
    {
        private static readonly string[] Headers =
        {
            "Employee Code", "Employee Name", "Leave Type", "Start Date", "End Date",
            "Total Days", "Status", "Reason", "Decision At", "Decision Comments"
        };

        private readonly ILeaveRepository _repo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IExcelExportService _excelService;

        public ExportLeaveRequestsQueryHandler(ILeaveRepository repo, IEmployeeRepository employeeRepo, IExcelExportService excelService)
        {
            _repo = repo;
            _employeeRepo = employeeRepo;
            _excelService = excelService;
        }

        public async Task<ExportFileResult> Handle(ExportLeaveRequestsQuery request, CancellationToken cancellationToken)
        {
            var requests = (await _repo.GetAllLeavesAsync(
                request.EmployeeId, request.LeaveTypeId, request.Year, request.Status, cancellationToken)).ToList();

            var employeeIds = requests.Select(r => r.EmployeeId).Distinct().ToList();
            var employeesById = (await _employeeRepo.GetByIdsAsync(employeeIds, cancellationToken)).ToDictionary(e => e.Id);

            var leaveTypesById = new Dictionary<Guid, string>();
            foreach (var leaveTypeId in requests.Select(r => r.LeaveTypeId).Distinct())
            {
                var leaveType = await _repo.GetLeaveTypeByIdIncludingDeletedAsync(leaveTypeId, cancellationToken);
                if (leaveType != null)
                    leaveTypesById[leaveTypeId] = leaveType.Name;
            }

            var rows = new List<IReadOnlyList<object?>>();
            foreach (var r in requests)
            {
                var dto = LeaveRequestDto.FromEntity(r);
                employeesById.TryGetValue(r.EmployeeId, out var employee);
                leaveTypesById.TryGetValue(r.LeaveTypeId, out var leaveTypeName);

                rows.Add(new List<object?>
                {
                    employee?.EmployeeCode, employee == null ? null : $"{employee.FirstName} {employee.LastName}",
                    leaveTypeName, dto.StartDate, dto.EndDate, dto.TotalDays, dto.Status, dto.Reason,
                    dto.DecisionAtUtc, dto.DecisionComments
                });
            }

            var bytes = await _excelService.GenerateAsync("Leave Requests", Headers, rows);
            var fileName = $"leave-requests_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

            return new ExportFileResult
            {
                Content = bytes,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = fileName
            };
        }
    }
}
