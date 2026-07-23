using EMS.Application.Common.DTOs;
using EMS.Application.Features.Exports.Queries;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Exports.Handlers
{
    public class ExportEmployeesQueryHandler : IRequestHandler<ExportEmployeesQuery, ExportFileResult>
    {
        private static readonly string[] Headers =
        {
            "Employee Code", "First Name", "Last Name", "Email", "Phone Number",
            "Department", "Designation", "Join Date", "Exit Date", "Employment Status"
        };

        private readonly IEmployeeRepository _repo;
        private readonly IExcelExportService _excelService;

        public ExportEmployeesQueryHandler(IEmployeeRepository repo, IExcelExportService excelService)
        {
            _repo = repo;
            _excelService = excelService;
        }

        public async Task<ExportFileResult> Handle(ExportEmployeesQuery request, CancellationToken cancellationToken)
        {
            var employees = await _repo.GetAllForExportAsync(
                request.Search, request.SortBy, request.SortDir, request.DepartmentId, request.Status, cancellationToken);

            var rows = new List<IReadOnlyList<object?>>();
            foreach (var e in employees)
            {
                rows.Add(new List<object?>
                {
                    e.EmployeeCode, e.FirstName, e.LastName, e.Email, e.PhoneNumber,
                    e.Department?.Name, e.Designation, e.JoinDate, e.ExitDate, e.EmploymentStatus
                });
            }

            var bytes = await _excelService.GenerateAsync("Employees", Headers, rows);
            var fileName = $"employees_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

            return new ExportFileResult
            {
                Content = bytes,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = fileName
            };
        }
    }
}
