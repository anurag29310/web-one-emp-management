using EMS.Domain.Enums;
using System;

namespace EMS.Domain.Entities
{
    /// <summary>
    /// Named TaskItem (not "Task") because EMS.Domain.Entities.Task would collide with
    /// System.Threading.Tasks.Task, which every async handler in this codebase uses constantly.
    /// The database table is still named "Tasks" (see TaskItemConfiguration).
    /// </summary>
    public class TaskItem
    {
        public Guid Id { get; set; }

        /// <summary>Human-readable identifier, derived from Id (e.g. "TSK-3F2A9B10"). Not user-editable.</summary>
        public string TaskNumber { get; set; } = null!;

        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        /// <summary>Nullable — not every task is a client visit; some are internal/office work.</summary>
        public Guid? ClientId { get; set; }
        public Client? Client { get; set; }

        public Guid AssignedEmployeeId { get; set; }
        public Employee AssignedEmployee { get; set; } = null!;

        /// <summary>The User (always Admin, per business rule) who created/assigned this task.</summary>
        public Guid AssignedByUserId { get; set; }

        public DateTime AssignedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskItemPriority Priority { get; set; } = TaskItemPriority.Medium;
        public TaskItemStatus Status { get; set; } = TaskItemStatus.Assigned;
        public string? Notes { get; set; }
        public DateTime? CompletedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public Guid? UpdatedBy { get; set; }
        public uint RowVersion { get; set; }
    }
}
