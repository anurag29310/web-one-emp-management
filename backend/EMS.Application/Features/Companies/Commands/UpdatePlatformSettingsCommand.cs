using MediatR;

namespace EMS.Application.Features.Companies.Commands
{
    public class UpdatePlatformSettingsCommand : IRequest
    {
        public bool IsPublicRegistrationEnabled { get; set; }
        public bool RequireApprovalForNewCompanies { get; set; }
    }
}
