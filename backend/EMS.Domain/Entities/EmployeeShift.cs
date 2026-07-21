using System;

namespace EMS.Domain.Entities
{
    public class EmployeeShift
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid ShiftId { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public Employee? Employee { get; set; }
        public Shift? Shift { get; set; }
    }
}
