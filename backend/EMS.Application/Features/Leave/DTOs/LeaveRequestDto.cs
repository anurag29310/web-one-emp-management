using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Leave.DTOs
{
    public class LeaveRequestDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid LeaveTypeId { get; set; }
        public Guid? ApproverEmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalDays { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? DecisionAtUtc { get; set; }
        public string? DecisionComments { get; set; }

        public static LeaveRequestDto FromEntity(LeaveRequest lr) => new()
        {
            Id = lr.Id,
            EmployeeId = lr.EmployeeId,
            LeaveTypeId = lr.LeaveTypeId,
            ApproverEmployeeId = lr.ApproverEmployeeId,
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            TotalDays = lr.TotalDays,
            Reason = lr.Reason,
            Status = lr.Status.ToString(),
            CreatedAtUtc = lr.CreatedAtUtc,
            DecisionAtUtc = lr.DecisionAtUtc,
            DecisionComments = lr.DecisionComments
        };
    }
}
