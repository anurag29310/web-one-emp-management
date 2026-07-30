using System;

namespace EMS.Domain.Entities
{
    public class Conversation
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public bool IsGroup { get; set; }
        public Guid CreatedByUserId { get; set; }
        public DateTime? LastMessageAtUtc { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public Guid? DeletedBy { get; set; }
        public uint RowVersion { get; set; }
    }
}
