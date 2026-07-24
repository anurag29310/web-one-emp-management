using System;

namespace EMS.Domain.Entities
{
    public class Team
    {
        public Guid Id { get; set; }
        public Guid DepartmentId { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public Guid? LeadEmployeeId { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public Guid? DeletedBy { get; set; }
        public uint RowVersion { get; set; }

        public Domain.Entities.Department? Department { get; set; }
        public Domain.Entities.Employee? LeadEmployee { get; set; }
    }
}
