using EMS.Application.Features.Companies.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Companies.Handlers
{
    public class ForceLogoutCompanyCommandHandler : IRequestHandler<ForceLogoutCompanyCommand>
    {
        private readonly ICompanyRepository _companyRepo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<ForceLogoutCompanyCommandHandler> _logger;

        public ForceLogoutCompanyCommandHandler(ICompanyRepository companyRepo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<ForceLogoutCompanyCommandHandler> logger)
        {
            _companyRepo = companyRepo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(ForceLogoutCompanyCommand request, CancellationToken cancellationToken)
        {
            var company = await _companyRepo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Company {request.Id} not found.");

            await _authRepo.RevokeAllRefreshTokensForCompanyAsync(company.Id, cancellationToken);
            await _authRepo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Force-logged-out all users of company {CompanyId}", company.Id);

            await _auditLogger.LogAsync("Company", company.Id, "ForceLogout", ct: cancellationToken);
        }
    }
}
