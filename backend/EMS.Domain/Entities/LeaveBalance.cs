using System;

namespace EMS.Domain.Entities
{
    public class LeaveBalance
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
    }
}
