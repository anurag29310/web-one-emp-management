using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Attendance.DTOs
{
    public class AttendanceCorrectionDto
    {
        public Guid Id { get; set; }
        public Guid AttendanceRecordId { get; set; }
        public Guid RequestedByEmployeeId { get; set; }
        public Guid? ApprovedByEmployeeId { get; set; }
        public DateTime? RequestedCheckInAtUtc { get; set; }
        public DateTime? RequestedCheckOutAtUtc { get; set; }
        public string Reason { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? DecisionAtUtc { get; set; }
        public string? DecisionComments { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public static AttendanceCorrectionDto FromEntity(AttendanceCorrection c) => new()
        {
            Id = c.Id,
            AttendanceRecordId = c.AttendanceRecordId,
            RequestedByEmployeeId = c.RequestedByEmployeeId,
            ApprovedByEmployeeId = c.ApprovedByEmployeeId,
            RequestedCheckInAtUtc = c.RequestedCheckInAtUtc,
            RequestedCheckOutAtUtc = c.RequestedCheckOutAtUtc,
            Reason = c.Reason,
            Status = c.Status.ToString(),
            DecisionAtUtc = c.DecisionAtUtc,
            DecisionComments = c.DecisionComments,
            CreatedAtUtc = c.CreatedAtUtc
        };
    }
}
