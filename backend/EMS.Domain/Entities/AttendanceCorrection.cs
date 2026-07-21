using System;
using EMS.Domain.Enums;

namespace EMS.Domain.Entities
{
    public class AttendanceCorrection
    {
        public Guid Id { get; set; }
        public Guid AttendanceRecordId { get; set; }
        public Guid RequestedByEmployeeId { get; set; }
        public Guid? ApprovedByEmployeeId { get; set; }
        public DateTime? RequestedCheckInAtUtc { get; set; }
        public DateTime? RequestedCheckOutAtUtc { get; set; }
        public string Reason { get; set; } = null!;
        public CorrectionStatus Status { get; set; } = CorrectionStatus.Pending;
        public DateTime? DecisionAtUtc { get; set; }
        public string? DecisionComments { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public AttendanceRecord? AttendanceRecord { get; set; }
    }
}
