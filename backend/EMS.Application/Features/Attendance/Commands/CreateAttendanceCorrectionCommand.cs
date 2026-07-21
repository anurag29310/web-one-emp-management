using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Commands
{
    public class CreateAttendanceCorrectionCommand : IRequest<AttendanceCorrection>
    {
        public Guid AttendanceRecordId { get; set; }
        public DateTime? RequestedCheckInAtUtc { get; set; }
        public DateTime? RequestedCheckOutAtUtc { get; set; }
        public string Reason { get; set; } = null!;

        /// <summary>Set by the controller from the caller's identity; the correction is always
        /// requested on the caller's own behalf regardless of role (api-specification.md 8.8).</summary>
        public Guid RequestingUserId { get; set; }
    }
}
