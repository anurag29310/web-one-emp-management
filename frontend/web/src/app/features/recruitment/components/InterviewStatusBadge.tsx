import type { InterviewStatus } from '../api'

const STATUS_STYLES: Record<InterviewStatus, string> = {
  Scheduled: 'bg-primary/15 text-primary ring-primary/30',
  Completed: 'bg-success/15 text-success ring-success/30',
  Cancelled: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
  NoShow: 'bg-danger/15 text-danger ring-danger/30',
}

const STATUS_LABELS: Record<InterviewStatus, string> = {
  Scheduled: 'Scheduled',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
  NoShow: 'No-Show',
}

export function InterviewStatusBadge({ status }: { status: InterviewStatus }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${STATUS_STYLES[status]}`}
    >
      {STATUS_LABELS[status]}
    </span>
  )
}
