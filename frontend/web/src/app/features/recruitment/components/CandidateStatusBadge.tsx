import type { CandidateStatus } from '../api'

const STATUS_STYLES: Record<CandidateStatus, string> = {
  Applied: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
  Screening: 'bg-primary/15 text-primary ring-primary/30',
  Interviewing: 'bg-primary/15 text-primary ring-primary/30',
  Offered: 'bg-warning/15 text-warning ring-warning/30',
  Hired: 'bg-success/15 text-success ring-success/30',
  Rejected: 'bg-danger/15 text-danger ring-danger/30',
  Withdrawn: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
}

export function CandidateStatusBadge({ status }: { status: CandidateStatus }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${STATUS_STYLES[status]}`}
    >
      {status}
    </span>
  )
}
