using EMS.Application.Features.Companies.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Companies.Handlers
{
    public class RestoreCompanyCommandHandler : IRequestHandler<RestoreCompanyCommand>
    {
        private readonly ICompanyRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RestoreCompanyCommandHandler> _logger;

        public RestoreCompanyCommandHandler(ICompanyRepository repo, IAuditLogger auditLogger, ILogger<RestoreCompanyCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RestoreCompanyCommand request, CancellationToken cancellationToken)
        {
            var company = await _repo.GetByIdIncludingDeletedAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Company {request.Id} not found.");

            if (!company.IsDeleted)
                throw new InvalidOperationException("Company is not deleted and cannot be restored.");

            await _repo.RestoreAsync(company, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Restored company {CompanyId}", company.Id);

            await _auditLogger.LogAsync("Company", company.Id, "Restored", newValues: new { company.Name }, ct: cancellationToken);
        }
    }
}
