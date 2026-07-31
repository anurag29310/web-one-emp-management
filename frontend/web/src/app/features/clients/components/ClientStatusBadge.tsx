import type { Client } from '../api'

export function ClientStatusBadge({ client }: { client: Pick<Client, 'isActive' | 'isArchived'> }) {
  if (client.isArchived) {
    return (
      <span className="inline-flex items-center rounded-full bg-surface-2 px-2.5 py-0.5 text-xs font-medium text-ink-subtle ring-1 ring-inset ring-hairline-strong">
        Archived
      </span>
    )
  }
  if (client.isActive) {
    return (
      <span className="inline-flex items-center rounded-full bg-success/15 px-2.5 py-0.5 text-xs font-medium text-success ring-1 ring-inset ring-success/30">
        Active
      </span>
    )
  }
  return (
    <span className="inline-flex items-center rounded-full bg-warning/15 px-2.5 py-0.5 text-xs font-medium text-warning ring-1 ring-inset ring-warning/30">
      Inactive
    </span>
  )
}
