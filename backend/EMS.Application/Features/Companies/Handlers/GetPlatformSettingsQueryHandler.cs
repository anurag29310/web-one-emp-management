using EMS.Application.Features.Companies.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Companies.Handlers
{
    public class GetPlatformSettingsQueryHandler : IRequestHandler<GetPlatformSettingsQuery, PlatformSettingsDto>
    {
        private readonly IPlatformSettingsRepository _repo;

        public GetPlatformSettingsQueryHandler(IPlatformSettingsRepository repo) => _repo = repo;

        public async Task<PlatformSettingsDto> Handle(GetPlatformSettingsQuery request, CancellationToken cancellationToken)
        {
            var settings = await _repo.GetAsync(cancellationToken);
            return new PlatformSettingsDto
            {
                IsPublicRegistrationEnabled = settings.IsPublicRegistrationEnabled,
                RequireApprovalForNewCompanies = settings.RequireApprovalForNewCompanies
            };
        }
    }
}
