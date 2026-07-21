using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Attendance.DTOs
{
    public class ShiftDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int GraceMinutes { get; set; }
        public bool IsNightShift { get; set; }

        public static ShiftDto FromEntity(Shift s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            GraceMinutes = s.GraceMinutes,
            IsNightShift = s.IsNightShift
        };
    }
}
