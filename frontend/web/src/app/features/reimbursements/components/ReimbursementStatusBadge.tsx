import type { ReimbursementStatus } from '../api'

const STATUS_STYLES: Record<ReimbursementStatus, string> = {
  Draft: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
  Submitted: 'bg-primary/15 text-primary ring-primary/30',
  UnderReview: 'bg-primary/15 text-primary ring-primary/30',
  Approved: 'bg-success/15 text-success ring-success/30',
  Rejected: 'bg-danger/15 text-danger ring-danger/30',
  ChangesRequested: 'bg-warning/15 text-warning ring-warning/30',
  Paid: 'bg-success/15 text-success ring-success/30',
}

const STATUS_LABELS: Record<ReimbursementStatus, string> = {
  Draft: 'Draft',
  Submitted: 'Submitted',
  UnderReview: 'Under Review',
  Approved: 'Approved',
  Rejected: 'Rejected',
  ChangesRequested: 'Changes Requested',
  Paid: 'Paid',
}

export function ReimbursementStatusBadge({ status }: { status: ReimbursementStatus }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${STATUS_STYLES[status]}`}
    >
      {STATUS_LABELS[status]}
    </span>
  )
}
