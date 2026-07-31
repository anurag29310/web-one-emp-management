using EMS.Application.Features.Companies.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Companies.Handlers
{
    public class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, Company>
    {
        private readonly ICompanyRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateCompanyCommandHandler> _logger;

        public UpdateCompanyCommandHandler(ICompanyRepository repo, IAuditLogger auditLogger, ILogger<UpdateCompanyCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Company> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
        {
            var company = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Company {request.Id} not found.");

            var oldValues = new { company.Name, company.Timezone, company.Currency };

            company.Name = request.Name;
            company.Timezone = request.Timezone;
            company.Currency = request.Currency;
            company.LogoUrl = request.LogoUrl;
            company.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(company, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated company {CompanyId}", company.Id);

            await _auditLogger.LogAsync("Company", company.Id, "Updated", oldValues: oldValues, newValues: new { company.Name, company.Timezone, company.Currency }, ct: cancellationToken);

            return company;
        }
    }
}
