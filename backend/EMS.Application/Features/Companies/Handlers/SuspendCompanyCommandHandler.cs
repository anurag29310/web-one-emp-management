using EMS.Application.Features.Companies.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Companies.Handlers
{
    /// <summary>Suspending a company is the action that actually matters for tenant safety: it
    /// flips Status to Suspended AND revokes every refresh token belonging to that company's users
    /// in the same unit of work, so no one already logged in stays logged in. TenantStatusMiddleware
    /// additionally blocks their still-valid (but now stale) access tokens on the very next request.</summary>
    public class SuspendCompanyCommandHandler : IRequestHandler<SuspendCompanyCommand>
    {
        private readonly ICompanyRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<SuspendCompanyCommandHandler> _logger;

        public SuspendCompanyCommandHandler(ICompanyRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<SuspendCompanyCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(SuspendCompanyCommand request, CancellationToken cancellationToken)
        {
            var company = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Company {request.Id} not found.");

            company.Status = CompanyStatus.Suspended;
            company.SuspendedAtUtc = DateTime.UtcNow;
            company.SuspendedReason = request.Reason;
            company.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(company, cancellationToken);
            await _authRepo.RevokeAllRefreshTokensForCompanyAsync(company.Id, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            await _authRepo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Suspended company {CompanyId} and force-logged-out its users", company.Id);

            await _auditLogger.LogAsync("Company", company.Id, "Suspended", newValues: new { request.Reason }, ct: cancellationToken);
        }
    }
}
