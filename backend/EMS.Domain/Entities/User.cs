using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public Guid? RoleId { get; set; }
        public Role? Role { get; set; }

        /// <summary>Null only for SuperAdmin-role users, who sit above every tenant. Every other role requires this to be set (enforced at the application layer).</summary>
        public Guid? CompanyId { get; set; }
        public Company? Company { get; set; }
        public Guid? EmployeeId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public bool IsMfaEnabled { get; set; }
        // TOTP secret, encrypted at rest (ASP.NET Core Data Protection) — never stored or
        // transmitted in plaintext once enrollment completes.
        public string? MfaSecretProtected { get; set; }
        public DateTime? MfaEnabledAtUtc { get; set; }
    }
}
