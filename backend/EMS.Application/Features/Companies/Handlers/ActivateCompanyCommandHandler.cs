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
    public class ActivateCompanyCommandHandler : IRequestHandler<ActivateCompanyCommand>
    {
        private readonly ICompanyRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<ActivateCompanyCommandHandler> _logger;

        public ActivateCompanyCommandHandler(ICompanyRepository repo, IAuditLogger auditLogger, ILogger<ActivateCompanyCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(ActivateCompanyCommand request, CancellationToken cancellationToken)
        {
            var company = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Company {request.Id} not found.");

            company.Status = CompanyStatus.Active;
            company.SuspendedAtUtc = null;
            company.SuspendedReason = null;
            company.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(company, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Activated company {CompanyId}", company.Id);

            await _auditLogger.LogAsync("Company", company.Id, "Activated", ct: cancellationToken);
        }
    }
}
