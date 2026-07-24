import type { AnnouncementPriority } from '../api'

const PRIORITY_STYLES: Record<AnnouncementPriority, string> = {
  Normal: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
  Important: 'bg-warning/15 text-warning ring-warning/30',
  Critical: 'bg-danger/15 text-danger ring-danger/30',
}

export function AnnouncementPriorityBadge({ priority }: { priority: AnnouncementPriority }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${PRIORITY_STYLES[priority]}`}
    >
      {priority}
    </span>
  )
}
