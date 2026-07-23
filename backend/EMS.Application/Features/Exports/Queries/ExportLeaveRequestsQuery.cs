using EMS.Application.Common.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Exports.Queries
{
    /// <summary>Export leave requests matching the same filters as <see cref="Leave.Queries.GetLeavesQuery"/> to Excel.
    /// Access is restricted to Admin, HR, and Manager, so no self-service scoping is applied.</summary>
    public class ExportLeaveRequestsQuery : IRequest<ExportFileResult>
    {
        public Guid? EmployeeId { get; set; }
        public Guid? LeaveTypeId { get; set; }
        public int? Year { get; set; }
        public string? Status { get; set; }
    }
}
