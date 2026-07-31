using EMS.Application.Features.Companies.DTOs;
using System.Collections.Generic;

namespace EMS.Application.DTOs
{
    public class PlatformDashboardSummaryDto
    {
        public int TotalCompanies { get; set; }
        public int ActiveCompanies { get; set; }
        public int SuspendedCompanies { get; set; }
        public int TrialCompanies { get; set; }
        public int TotalEmployeesAcrossAllCompanies { get; set; }
        public IReadOnlyList<CompanyDto> RecentRegistrations { get; set; } = System.Array.Empty<CompanyDto>();
    }
}
