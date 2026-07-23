using EMS.Application.Common.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Exports.Queries
{
    /// <summary>Export the dashboard summary matching the same filters as
    /// <see cref="Dashboard.Queries.GetDashboardSummaryQuery"/> to PDF.</summary>
    public class ExportDashboardSummaryQuery : IRequest<ExportFileResult>
    {
        public Guid? DepartmentId { get; set; }

        // Accepted for API contract parity with api-specification.md, but not yet enforced:
        // there is no OfficeLocation entity in the domain model yet.
        public Guid? OfficeLocationId { get; set; }

        public DateTime? Date { get; set; }
    }
}
