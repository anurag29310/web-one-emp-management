import type { PayrollRunStatus } from '../api'

const STATUS_STYLES: Record<PayrollRunStatus, string> = {
  Processing: 'bg-warning/15 text-warning ring-warning/30',
  Completed: 'bg-primary/15 text-primary-hover ring-primary/30',
  Approved: 'bg-success/15 text-success ring-success/30',
}

export function PayrollStatusBadge({ status }: { status: PayrollRunStatus }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${STATUS_STYLES[status]}`}
    >
      {status}
    </span>
  )
}
