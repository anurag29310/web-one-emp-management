using MediatR;

namespace EMS.Application.Features.Companies.Commands
{
    /// <summary>Public self-service company registration — creates a Company and its first Admin
    /// User atomically. Gated by PlatformSettings.IsPublicRegistrationEnabled; the resulting status
    /// (PendingApproval vs Trial) depends on PlatformSettings.RequireApprovalForNewCompanies.</summary>
    public class RegisterCompanyCommand : IRequest<RegisterCompanyResult>
    {
        public string CompanyName { get; set; } = null!;
        public string Timezone { get; set; } = "UTC";
        public string Currency { get; set; } = "USD";

        public string AdminUserName { get; set; } = null!;
        public string AdminEmail { get; set; } = null!;
        public string AdminPassword { get; set; } = null!;
    }
}
