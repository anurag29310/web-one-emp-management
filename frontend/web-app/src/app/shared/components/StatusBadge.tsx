const STATUS_STYLES: Record<string, string> = {
  Active: 'bg-success/15 text-success ring-success/30',
  OnLeave: 'bg-warning/15 text-warning ring-warning/30',
  Inactive: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
  Terminated: 'bg-danger/15 text-danger ring-danger/30',
}

export function StatusBadge({ status }: { status: string }) {
  const style = STATUS_STYLES[status] ?? STATUS_STYLES.Inactive
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${style}`}
    >
      {status}
    </span>
  )
}
