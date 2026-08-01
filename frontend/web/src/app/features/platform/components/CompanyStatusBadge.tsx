import type { CompanyStatus } from '../api'

const STATUS_STYLES: Record<CompanyStatus, string> = {
  Trial: 'bg-primary/15 text-primary ring-primary/30',
  Active: 'bg-success/15 text-success ring-success/30',
  Suspended: 'bg-danger/15 text-danger ring-danger/30',
  Inactive: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
  PendingApproval: 'bg-warning/15 text-warning ring-warning/30',
  Rejected: 'bg-danger/15 text-danger ring-danger/30',
}

const STATUS_LABELS: Record<CompanyStatus, string> = {
  Trial: 'Trial',
  Active: 'Active',
  Suspended: 'Suspended',
  Inactive: 'Inactive',
  PendingApproval: 'Pending Approval',
  Rejected: 'Rejected',
}

export function CompanyStatusBadge({ status }: { status: CompanyStatus }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${STATUS_STYLES[status]}`}
    >
      {STATUS_LABELS[status]}
    </span>
  )
}
