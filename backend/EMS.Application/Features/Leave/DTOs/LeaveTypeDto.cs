using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Leave.DTOs
{
    public class LeaveTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
        public bool IsPaid { get; set; }
        public bool RequiresApproval { get; set; }
        public decimal? AnnualEntitlementDays { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static LeaveTypeDto FromEntity(LeaveType lt) => new()
        {
            Id = lt.Id,
            Name = lt.Name,
            Code = lt.Code,
            IsPaid = lt.IsPaid,
            RequiresApproval = lt.RequiresApproval,
            AnnualEntitlementDays = lt.AnnualEntitlementDays,
            CreatedAtUtc = lt.CreatedAtUtc,
            UpdatedAtUtc = lt.UpdatedAtUtc
        };
    }
}
