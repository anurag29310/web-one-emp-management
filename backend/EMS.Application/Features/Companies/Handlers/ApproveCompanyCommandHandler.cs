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
    public class ApproveCompanyCommandHandler : IRequestHandler<ApproveCompanyCommand>
    {
        private readonly ICompanyRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<ApproveCompanyCommandHandler> _logger;

        public ApproveCompanyCommandHandler(ICompanyRepository repo, IAuditLogger auditLogger, ILogger<ApproveCompanyCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(ApproveCompanyCommand request, CancellationToken cancellationToken)
        {
            var company = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Company {request.Id} not found.");

            if (company.Status != CompanyStatus.PendingApproval)
                throw new InvalidOperationException($"Company {company.Name} is not pending approval (currently {company.Status}).");

            company.Status = CompanyStatus.Trial;
            company.ApprovedAtUtc = DateTime.UtcNow;
            company.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(company, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Approved company {CompanyId}", company.Id);

            await _auditLogger.LogAsync("Company", company.Id, "Approved", ct: cancellationToken);
        }
    }
}
