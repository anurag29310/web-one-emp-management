using System;

namespace EMS.Domain.Entities
{
    /// <summary>Append-only progress/notes log for a task — never updated or deleted, matching the AnnouncementReads convention.</summary>
    public class TaskComment
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid AuthorUserId { get; set; }
        public string Comment { get; set; } = null!;
        public DateTime CreatedAtUtc { get; set; }
    }
}
