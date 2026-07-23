import { useDashboardSummary } from '../hooks/useDashboardSummary'

const METRIC_STYLES = {
  primary: { dot: 'bg-primary', text: 'text-ink' },
  success: { dot: 'bg-success', text: 'text-ink' },
  neutral: { dot: 'bg-ink-tertiary', text: 'text-ink' },
  warning: { dot: 'bg-warning', text: 'text-ink' },
  danger: { dot: 'bg-danger', text: 'text-ink' },
} as const

function MetricCard({
  label,
  value,
  tone,
}: {
  label: string
  value: number
  tone: keyof typeof METRIC_STYLES
}) {
  const style = METRIC_STYLES[tone]
  return (
    <div className="rounded-lg border border-hairline bg-surface-1 p-5">
      <div className="flex items-center justify-between">
        <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
        <span className={`h-2 w-2 rounded-full ${style.dot}`} />
      </div>
      <p className={`mt-2 text-3xl font-semibold tracking-[-0.6px] ${style.text}`}>{value}</p>
    </div>
  )
}

export function DashboardPage() {
  const { summary, isLoading, error } = useDashboardSummary()

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <div key={i} className="h-24 animate-pulse rounded-lg border border-hairline bg-surface-1" />
        ))}
      </div>
    )
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!summary) return null

  const maxDeptCount = Math.max(...summary.departments.map((d) => d.activeEmployees), 1)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Dashboard</h1>
        <p className="text-sm text-ink-subtle">Organization overview at a glance</p>
      </div>

      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
        <MetricCard label="Total Employees" value={summary.totalEmployees} tone="primary" />
        <MetricCard label="Active" value={summary.activeEmployees} tone="success" />
        <MetricCard label="Inactive" value={summary.inactiveEmployees} tone="neutral" />
        <MetricCard label="Present Today" value={summary.attendance.present} tone="success" />
        <MetricCard label="On Leave" value={summary.attendance.onLeave} tone="warning" />
        <MetricCard label="Pending Leave" value={summary.leave.pending} tone="danger" />
      </div>

      <div className="rounded-lg border border-hairline bg-surface-1 p-5">
        <h2 className="mb-4 text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">Departments</h2>
        <ul className="space-y-3">
          {summary.departments.map((department) => (
            <li key={department.departmentId}>
              <div className="mb-1 flex justify-between text-sm">
                <span className="font-medium text-ink-muted">{department.departmentName}</span>
                <span className="text-ink-subtle">{department.activeEmployees} active</span>
              </div>
              <div className="h-1.5 w-full overflow-hidden rounded-full bg-surface-2">
                <div
                  className="h-full rounded-full bg-primary"
                  style={{ width: `${(department.activeEmployees / maxDeptCount) * 100}%` }}
                />
              </div>
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}
