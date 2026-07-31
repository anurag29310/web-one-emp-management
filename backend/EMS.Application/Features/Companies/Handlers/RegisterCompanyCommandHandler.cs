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
    public class RegisterCompanyCommandHandler : IRequestHandler<RegisterCompanyCommand, RegisterCompanyResult>
    {
        private readonly ICompanyRepository _companyRepo;
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly IPlatformSettingsRepository _settingsRepo;
        private readonly IPasswordHashService _passwordHasher;
        private readonly IJwtTokenService _jwtService;
        private readonly IRefreshTokenService _refreshService;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RegisterCompanyCommandHandler> _logger;

        public RegisterCompanyCommandHandler(
            ICompanyRepository companyRepo, IUserRepository userRepo, IRoleRepository roleRepo, IPlatformSettingsRepository settingsRepo,
            IPasswordHashService passwordHasher, IJwtTokenService jwtService, IRefreshTokenService refreshService, IAuthRepository authRepo,
            IAuditLogger auditLogger, ILogger<RegisterCompanyCommandHandler> logger)
        {
            _companyRepo = companyRepo;
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _settingsRepo = settingsRepo;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _refreshService = refreshService;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<RegisterCompanyResult> Handle(RegisterCompanyCommand request, CancellationToken cancellationToken)
        {
            var settings = await _settingsRepo.GetAsync(cancellationToken);
            if (!settings.IsPublicRegistrationEnabled)
                throw new InvalidOperationException("Public company registration is currently disabled.");

            if (await _userRepo.UserNameExistsAsync(request.AdminUserName, ct: cancellationToken))
                throw new InvalidOperationException("Username already exists.");
            if (await _userRepo.EmailExistsAsync(request.AdminEmail, ct: cancellationToken))
                throw new InvalidOperationException("Email already exists.");

            var adminRole = await _roleRepo.GetByNameAsync("Admin", cancellationToken)
                ?? throw new InvalidOperationException("Admin role is not configured.");

            var requiresApproval = settings.RequireApprovalForNewCompanies;
            var now = DateTime.UtcNow;

            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = request.CompanyName,
                Status = requiresApproval ? CompanyStatus.PendingApproval : CompanyStatus.Trial,
                Timezone = request.Timezone,
                Currency = request.Currency,
                RegisteredAtUtc = now,
                ApprovedAtUtc = requiresApproval ? null : now,
                IsDeleted = false,
                CreatedAtUtc = now
            };
            await _companyRepo.AddAsync(company, cancellationToken);

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.AdminUserName,
                Email = request.AdminEmail,
                PasswordHash = _passwordHasher.Hash(request.AdminPassword),
                RoleId = adminRole.Id,
                CompanyId = company.Id,
                IsActive = true,
                CreatedAtUtc = now
            };
            await _userRepo.AddAsync(adminUser, cancellationToken);

            var result = new RegisterCompanyResult
            {
                CompanyId = company.Id,
                CompanyStatus = company.Status.ToString(),
                RequiresApproval = requiresApproval
            };

            if (!requiresApproval)
            {
                var access = _jwtService.GenerateAccessToken(adminUser);
                var refresh = _refreshService.CreateRefreshToken(adminUser.Id);
                await _authRepo.AddRefreshTokenAsync(refresh, cancellationToken);

                result.AccessToken = access;
                result.RefreshToken = refresh.Token;
                result.ExpiresInSeconds = 900;
            }

            await _companyRepo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Registered company {CompanyName} ({CompanyId}) with status {Status}", company.Name, company.Id, company.Status);

            await _auditLogger.LogAsync("Company", company.Id, "Created", newValues: new { company.Name, company.Status }, ct: cancellationToken);

            return result;
        }
    }
}
