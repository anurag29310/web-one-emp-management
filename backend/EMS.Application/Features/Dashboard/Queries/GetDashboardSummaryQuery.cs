using EMS.Application.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Dashboard.Queries
{
    public class GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>
    {
        public Guid? DepartmentId { get; set; }

        // Accepted for API contract parity with api-specification.md, but not yet enforced:
        // there is no OfficeLocation entity in the domain model yet.
        public Guid? OfficeLocationId { get; set; }

        public DateTime? Date { get; set; }
    }
}
