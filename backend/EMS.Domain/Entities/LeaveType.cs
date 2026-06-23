using System;

namespace EMS.Domain.Entities
{
    public class LeaveType
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
        public bool IsPaid { get; set; }
        public bool RequiresApproval { get; set; }
        public decimal? AnnualEntitlementDays { get; set; }
    }
}
