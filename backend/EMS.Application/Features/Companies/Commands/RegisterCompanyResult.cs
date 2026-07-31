using System;

namespace EMS.Application.Features.Companies.Commands
{
    /// <summary>Distinct from Auth.LoginResult — a pending-approval registration returns no tokens
    /// at all (rather than overloading LoginResult's "null means MFA pending" meaning with a second,
    /// unrelated reason for absent tokens).</summary>
    public class RegisterCompanyResult
    {
        public Guid CompanyId { get; set; }
        public string CompanyStatus { get; set; } = null!;
        public bool RequiresApproval { get; set; }

        // Populated only when the company lands directly in Trial/Active (RequiresApproval == false).
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int? ExpiresInSeconds { get; set; }
    }
}
