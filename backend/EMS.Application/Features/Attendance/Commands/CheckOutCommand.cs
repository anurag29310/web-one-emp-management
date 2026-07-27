using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Commands
{
    public class CheckOutCommand : IRequest<AttendanceRecord>
    {
        public Guid EmployeeId { get; set; }
        public DateTime CheckOutAtUtc { get; set; }
        public string? Notes { get; set; }

        /// <summary>GPS coordinates captured by the client at punch time. Required — Punch Out location must always be recorded, even off-premises.</summary>
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        /// <summary>Set by the controller from the request's User-Agent header — never client-supplied.</summary>
        public string? DeviceInfo { get; set; }

        /// <summary>Set by the controller from the request's remote IP — never client-supplied.</summary>
        public string? IpAddress { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }
}
