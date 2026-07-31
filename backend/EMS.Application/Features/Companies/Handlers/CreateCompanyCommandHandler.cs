using EMS.Application.Features.Companies.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Companies.Handlers
{
    public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Company>
    {
        private readonly ICompanyRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CreateCompanyCommandHandler> _logger;

        public CreateCompanyCommandHandler(ICompanyRepository repo, IAuditLogger auditLogger, ILogger<CreateCompanyCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Company> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
        {
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Status = CompanyStatus.Active,
                Timezone = request.Timezone,
                Currency = request.Currency,
                LogoUrl = request.LogoUrl,
                RegisteredAtUtc = DateTime.UtcNow,
                ApprovedAtUtc = DateTime.UtcNow,
                IsDeleted = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repo.AddAsync(company, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created company {CompanyName} ({CompanyId})", company.Name, company.Id);

            await _auditLogger.LogAsync("Company", company.Id, "Created", newValues: new { company.Name, company.Status }, ct: cancellationToken);

            return company;
        }
    }
}
