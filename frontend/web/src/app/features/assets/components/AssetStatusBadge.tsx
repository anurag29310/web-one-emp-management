import type { AssetStatus } from '../api'

const STATUS_STYLES: Record<AssetStatus, string> = {
  Available: 'bg-success/15 text-success ring-success/30',
  Assigned: 'bg-primary/15 text-primary ring-primary/30',
  UnderRepair: 'bg-warning/15 text-warning ring-warning/30',
  Retired: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
  Lost: 'bg-danger/15 text-danger ring-danger/30',
}

export function AssetStatusBadge({ status }: { status: AssetStatus }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${STATUS_STYLES[status]}`}
    >
      {status}
    </span>
  )
}
