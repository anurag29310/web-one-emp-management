using EMS.Application.Common.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Exports.Queries
{
    /// <summary>Export employees matching the same filters as <see cref="Employees.Queries.GetEmployeesQuery"/> to Excel.</summary>
    public class ExportEmployeesQuery : IRequest<ExportFileResult>
    {
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string? SortDir { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? Status { get; set; }
    }
}
