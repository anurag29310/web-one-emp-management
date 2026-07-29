using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Assets.DTOs
{
    public class AssetDto
    {
        public Guid Id { get; set; }
        public string AssetTag { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal? PurchaseCost { get; set; }
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static AssetDto FromEntity(Asset a) => new()
        {
            Id = a.Id,
            AssetTag = a.AssetTag,
            Category = a.Category,
            Brand = a.Brand,
            Model = a.Model,
            SerialNumber = a.SerialNumber,
            PurchaseDate = a.PurchaseDate,
            PurchaseCost = a.PurchaseCost,
            Status = a.Status.ToString(),
            Notes = a.Notes,
            IsDeleted = a.IsDeleted,
            CreatedAtUtc = a.CreatedAtUtc,
            UpdatedAtUtc = a.UpdatedAtUtc
        };
    }
}
