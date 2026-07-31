using EMS.Application.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Dashboard.Handlers
{
    public class GetDashboardSummaryQueryHandler : IRequestHandler<Queries.GetDashboardSummaryQuery, DashboardSummaryDto>
    {
        private readonly IDashboardRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<GetDashboardSummaryQueryHandler> _logger;

        public GetDashboardSummaryQueryHandler(IDashboardRepository repo, ICurrentUserService currentUser, ILogger<GetDashboardSummaryQueryHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<DashboardSummaryDto> Handle(Queries.GetDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            // Validation runs via EMS.Application.Common.Behaviors.ValidationBehavior,
            // the shared MediatR pipeline behavior, using the IValidator<T> registered in Program.cs.
            var date = (request.Date ?? DateTime.UtcNow).Date;
            var summary = await _repo.GetSummaryAsync(_currentUser.CompanyId!.Value, request.DepartmentId, date, cancellationToken);

            _logger.LogInformation(
                "Dashboard summary requested for {Date} (department {DepartmentId})",
                date, request.DepartmentId);

            return summary;
        }
    }
}
