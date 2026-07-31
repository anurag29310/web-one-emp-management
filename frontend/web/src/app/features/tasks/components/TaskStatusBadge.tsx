import type { TaskStatus } from '../api'

const STATUS_STYLES: Record<TaskStatus, string> = {
  Assigned: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
  Accepted: 'bg-primary/15 text-primary ring-primary/30',
  Rejected: 'bg-danger/15 text-danger ring-danger/30',
  InProgress: 'bg-primary/15 text-primary ring-primary/30',
  OnHold: 'bg-warning/15 text-warning ring-warning/30',
  Completed: 'bg-success/15 text-success ring-success/30',
  Cancelled: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
}

const STATUS_LABELS: Record<TaskStatus, string> = {
  Assigned: 'Assigned',
  Accepted: 'Accepted',
  Rejected: 'Rejected',
  InProgress: 'In Progress',
  OnHold: 'On Hold',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
}

export function TaskStatusBadge({ status }: { status: TaskStatus }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${STATUS_STYLES[status]}`}
    >
      {STATUS_LABELS[status]}
    </span>
  )
}
