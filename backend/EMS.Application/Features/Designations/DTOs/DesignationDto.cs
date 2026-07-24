using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Designations.DTOs
{
    public class DesignationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public int? Level { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static DesignationDto FromEntity(Designation d) => new()
        {
            Id = d.Id,
            Name = d.Name,
            Code = d.Code,
            Level = d.Level,
            IsDeleted = d.IsDeleted,
            CreatedAtUtc = d.CreatedAtUtc,
            UpdatedAtUtc = d.UpdatedAtUtc
        };
    }
}
