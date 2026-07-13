using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Leave.DTOs
{
    public class HolidayDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Guid? OfficeLocationId { get; set; }
        public DateTime HolidayDate { get; set; }
        public bool IsOptional { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static HolidayDto FromEntity(Holiday h) => new()
        {
            Id = h.Id,
            Name = h.Name,
            OfficeLocationId = h.OfficeLocationId,
            HolidayDate = h.HolidayDate,
            IsOptional = h.IsOptional,
            CreatedAtUtc = h.CreatedAtUtc,
            UpdatedAtUtc = h.UpdatedAtUtc
        };
    }
}
