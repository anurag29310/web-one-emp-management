using System;

namespace EMS.Domain.Entities
{
    public class Deduction
    {
        public Guid Id { get; set; }
        public Guid SalaryStructureId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Amount { get; set; }
    }
}
