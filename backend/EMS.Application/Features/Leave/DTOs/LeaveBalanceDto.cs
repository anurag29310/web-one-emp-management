using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Leave.DTOs
{
    public class LeaveBalanceDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid LeaveTypeId { get; set; }
        public int Year { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal Accrued { get; set; }
        public decimal Used { get; set; }
        public decimal Adjusted { get; set; }
        public decimal Available { get; set; }

        public static LeaveBalanceDto FromEntity(LeaveBalance b) => new()
        {
            Id = b.Id,
            EmployeeId = b.EmployeeId,
            LeaveTypeId = b.LeaveTypeId,
            Year = b.Year,
            OpeningBalance = b.OpeningBalance,
            Accrued = b.Accrued,
            Used = b.Used,
            Adjusted = b.Adjusted,
            Available = b.Available
        };
    }
}
