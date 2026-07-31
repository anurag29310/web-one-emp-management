using EMS.Application.DTOs;
using EMS.Application.Features.Companies.DTOs;
using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.PlatformDashboard.Handlers
{
    public class GetPlatformDashboardSummaryQueryHandler : IRequestHandler<Queries.GetPlatformDashboardSummaryQuery, PlatformDashboardSummaryDto>
    {
        private readonly ICompanyRepository _repo;
        private readonly ILogger<GetPlatformDashboardSummaryQueryHandler> _logger;

        public GetPlatformDashboardSummaryQueryHandler(ICompanyRepository repo, ILogger<GetPlatformDashboardSummaryQueryHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<PlatformDashboardSummaryDto> Handle(Queries.GetPlatformDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            var statusCounts = await _repo.GetStatusCountsAsync(cancellationToken);
            var totalEmployees = await _repo.GetTotalEmployeeCountAcrossAllCompaniesAsync(cancellationToken);
            var recent = await _repo.GetRecentRegistrationsAsync(request.RecentCount, cancellationToken);

            _logger.LogInformation("Platform dashboard summary requested");

            return new PlatformDashboardSummaryDto
            {
                TotalCompanies = statusCounts.Values.Sum(),
                ActiveCompanies = statusCounts.GetValueOrDefault(CompanyStatus.Active),
                SuspendedCompanies = statusCounts.GetValueOrDefault(CompanyStatus.Suspended),
                TrialCompanies = statusCounts.GetValueOrDefault(CompanyStatus.Trial),
                TotalEmployeesAcrossAllCompanies = totalEmployees,
                RecentRegistrations = recent.Select(CompanyDto.FromEntity).ToList()
            };
        }
    }
}
