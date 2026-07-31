using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Companies.DTOs
{
    public class CompanyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string Timezone { get; set; } = null!;
        public string Currency { get; set; } = null!;
        public string? LogoUrl { get; set; }
        public DateTime RegisteredAtUtc { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public DateTime? SuspendedAtUtc { get; set; }
        public string? SuspendedReason { get; set; }
        public DateTime? RejectedAtUtc { get; set; }
        public string? RejectedReason { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static CompanyDto FromEntity(Company c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Status = c.Status.ToString(),
            Timezone = c.Timezone,
            Currency = c.Currency,
            LogoUrl = c.LogoUrl,
            RegisteredAtUtc = c.RegisteredAtUtc,
            ApprovedAtUtc = c.ApprovedAtUtc,
            SuspendedAtUtc = c.SuspendedAtUtc,
            SuspendedReason = c.SuspendedReason,
            RejectedAtUtc = c.RejectedAtUtc,
            RejectedReason = c.RejectedReason,
            IsDeleted = c.IsDeleted,
            CreatedAtUtc = c.CreatedAtUtc,
            UpdatedAtUtc = c.UpdatedAtUtc
        };
    }

    public class CompanyAdminDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    public class CompanyDetailDto : CompanyDto
    {
        public int EmployeeCount { get; set; }
        public System.Collections.Generic.IReadOnlyList<CompanyAdminDto> Admins { get; set; } = System.Array.Empty<CompanyAdminDto>();
    }
}
