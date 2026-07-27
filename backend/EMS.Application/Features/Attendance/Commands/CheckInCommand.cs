using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Commands
{
    public class CheckInCommand : IRequest<AttendanceRecord>
    {
        public Guid EmployeeId { get; set; }
        public DateTime CheckInAtUtc { get; set; }
        public string? Notes { get; set; }

        /// <summary>GPS coordinates captured by the client (browser/mobile Geolocation API) at punch time. Required — GPS is captured on every punch.</summary>
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        /// <summary>Set by the controller from the request's User-Agent header — never client-supplied.</summary>
        public string? DeviceInfo { get; set; }

        /// <summary>Set by the controller from the request's remote IP — never client-supplied.</summary>
        public string? IpAddress { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin/HR role and may record on behalf of another employee.</summary>
        public bool IsPrivileged { get; set; }
    }
}
