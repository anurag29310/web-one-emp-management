using EMS.Domain.Enums;
using System;

namespace EMS.Domain.Entities
{
    public class Asset
    {
        public Guid Id { get; set; }
        public string AssetTag { get; set; } = null!;

        /// <summary>Free text (e.g. Laptop, Mobile, Monitor) — requirements.md names Laptop/Mobile as
        /// examples, not an exhaustive list, matching Reimbursement.ExpenseCategory's precedent.</summary>
        public string Category { get; set; } = null!;
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal? PurchaseCost { get; set; }
        public AssetStatus Status { get; set; } = AssetStatus.Available;
        public string? Notes { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public Guid? DeletedBy { get; set; }
        public uint RowVersion { get; set; }
    }
}
