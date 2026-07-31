using MediatR;

namespace EMS.Application.Features.Companies.Queries
{
    public class GetPlatformSettingsQuery : IRequest<PlatformSettingsDto>
    {
    }

    public class PlatformSettingsDto
    {
        public bool IsPublicRegistrationEnabled { get; set; }
        public bool RequireApprovalForNewCompanies { get; set; }
    }
}
