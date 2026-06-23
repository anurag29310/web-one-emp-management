using System;
using EMS.Domain.Enums;

namespace EMS.Domain.Entities
{
    public class LeaveRequest
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid LeaveTypeId { get; set; }
        public Guid? ApproverEmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalDays { get; set; }
        public string? Reason { get; set; }
        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? DecisionAtUtc { get; set; }
        public string? DecisionComments { get; set; }
    }
}
