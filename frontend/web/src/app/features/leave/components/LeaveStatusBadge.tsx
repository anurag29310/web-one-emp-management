import type { LeaveStatus } from '../api'

const STATUS_STYLES: Record<LeaveStatus, string> = {
  Pending: 'bg-warning/15 text-warning ring-warning/30',
  Approved: 'bg-success/15 text-success ring-success/30',
  Rejected: 'bg-danger/15 text-danger ring-danger/30',
  Cancelled: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
}

export function LeaveStatusBadge({ status }: { status: LeaveStatus }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${STATUS_STYLES[status]}`}
    >
      {status}
    </span>
  )
}
