using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
