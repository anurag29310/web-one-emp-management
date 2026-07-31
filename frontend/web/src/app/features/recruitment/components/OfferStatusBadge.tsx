import type { OfferStatus } from '../api'

const STATUS_STYLES: Record<OfferStatus, string> = {
  Draft: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
  Sent: 'bg-primary/15 text-primary ring-primary/30',
  Accepted: 'bg-success/15 text-success ring-success/30',
  Rejected: 'bg-danger/15 text-danger ring-danger/30',
  Withdrawn: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
  Expired: 'bg-warning/15 text-warning ring-warning/30',
}

export function OfferStatusBadge({ status }: { status: OfferStatus }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${STATUS_STYLES[status]}`}
    >
      {status}
    </span>
  )
}
