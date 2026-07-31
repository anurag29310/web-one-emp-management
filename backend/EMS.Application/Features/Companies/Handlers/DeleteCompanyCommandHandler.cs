using EMS.Application.Features.Companies.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Companies.Handlers
{
    public class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand>
    {
        private readonly ICompanyRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<DeleteCompanyCommandHandler> _logger;

        public DeleteCompanyCommandHandler(ICompanyRepository repo, IAuditLogger auditLogger, ILogger<DeleteCompanyCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
        {
            var company = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Company {request.Id} not found.");

            await _repo.DeleteAsync(company, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) company {CompanyId}", company.Id);

            await _auditLogger.LogAsync("Company", company.Id, "Deleted", oldValues: new { company.Name }, ct: cancellationToken);
        }
    }
}
