using System;

namespace EMS.Domain.Entities
{
    public class Allowance
    {
        public Guid Id { get; set; }
        public Guid SalaryStructureId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Amount { get; set; }

        // Recreated wholesale (not edited in place) whenever the parent SalaryStructure is updated
        // — see UpdateSalaryStructureCommandHandler's "replace children" pattern — so there's no
        // independent update/delete lifecycle to track. CreatedAtUtc alone records when this
        // specific line item was written.
        public DateTime CreatedAtUtc { get; set; }
    }
}
