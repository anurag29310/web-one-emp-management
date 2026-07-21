using System;

namespace EMS.Domain.Entities
{
    public class Shift
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int GraceMinutes { get; set; }
        public bool IsNightShift { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
