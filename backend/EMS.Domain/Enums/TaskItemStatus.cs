namespace EMS.Domain.Enums
{
    public enum TaskItemStatus
    {
        Assigned = 0,
        Accepted = 1,
        /// <summary>Landing state for the optional "Reject task" employee action. Not in the six
        /// statuses requirements.md lists explicitly, but Reject needs some state to land in that
        /// isn't Cancelled (an admin-only action) or a silent no-op back to Assigned.</summary>
        Rejected = 2,
        InProgress = 3,
        OnHold = 4,
        Completed = 5,
        Cancelled = 6
    }
}
