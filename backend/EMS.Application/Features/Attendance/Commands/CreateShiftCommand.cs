using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Commands
{
    public class CreateShiftCommand : IRequest<Shift>
    {
        public string Name { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int GraceMinutes { get; set; }
        public bool IsNightShift { get; set; }
    }
}
