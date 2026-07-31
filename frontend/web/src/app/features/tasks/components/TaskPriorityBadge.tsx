import type { TaskPriority } from '../api'

const PRIORITY_STYLES: Record<TaskPriority, string> = {
  Low: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
  Medium: 'bg-primary/15 text-primary ring-primary/30',
  High: 'bg-warning/15 text-warning ring-warning/30',
  Critical: 'bg-danger/15 text-danger ring-danger/30',
}

export function TaskPriorityBadge({ priority }: { priority: TaskPriority }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${PRIORITY_STYLES[priority]}`}
    >
      {priority}
    </span>
  )
}
