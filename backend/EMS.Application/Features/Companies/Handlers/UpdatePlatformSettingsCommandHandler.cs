using EMS.Application.Features.Companies.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Companies.Handlers
{
    public class UpdatePlatformSettingsCommandHandler : IRequestHandler<UpdatePlatformSettingsCommand>
    {
        private readonly IPlatformSettingsRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdatePlatformSettingsCommandHandler> _logger;

        public UpdatePlatformSettingsCommandHandler(IPlatformSettingsRepository repo, IAuditLogger auditLogger, ILogger<UpdatePlatformSettingsCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(UpdatePlatformSettingsCommand request, CancellationToken cancellationToken)
        {
            var settings = await _repo.GetAsync(cancellationToken);

            settings.IsPublicRegistrationEnabled = request.IsPublicRegistrationEnabled;
            settings.RequireApprovalForNewCompanies = request.RequireApprovalForNewCompanies;
            settings.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(settings, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated platform settings");

            await _auditLogger.LogAsync("PlatformSettings", null, "Updated", newValues: new { request.IsPublicRegistrationEnabled, request.RequireApprovalForNewCompanies }, ct: cancellationToken);
        }
    }
}
