import { Link } from 'react-router-dom'
import { usePlatformDashboard } from '../hooks/usePlatformDashboard'
import { CompanyStatusBadge } from '../components/CompanyStatusBadge'

const METRIC_STYLES = {
  primary: { dot: 'bg-primary', text: 'text-ink' },
  success: { dot: 'bg-success', text: 'text-ink' },
  neutral: { dot: 'bg-ink-tertiary', text: 'text-ink' },
  danger: { dot: 'bg-danger', text: 'text-ink' },
} as const

function MetricCard({ label, value, tone }: { label: string; value: number; tone: keyof typeof METRIC_STYLES }) {
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

export function PlatformDashboardPage() {
  const { summary, isLoading, error } = usePlatformDashboard()

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="h-24 animate-pulse rounded-lg border border-hairline bg-surface-1" />
        ))}
      </div>
    )
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!summary) return null

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Platform Dashboard</h1>
        <p className="text-sm text-ink-subtle">Cross-company overview</p>
      </div>

      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
        <MetricCard label="Total Companies" value={summary.totalCompanies} tone="primary" />
        <MetricCard label="Active" value={summary.activeCompanies} tone="success" />
        <MetricCard label="Trial" value={summary.trialCompanies} tone="neutral" />
        <MetricCard label="Suspended" value={summary.suspendedCompanies} tone="danger" />
        <MetricCard label="Total Employees" value={summary.totalEmployeesAcrossAllCompanies} tone="primary" />
      </div>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="border-b border-hairline p-5">
          <h2 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
            Recently registered
          </h2>
        </div>
        {summary.recentRegistrations.length === 0 ? (
          <p className="p-5 text-sm text-ink-subtle">No companies registered yet.</p>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
              <tr>
                <th className="px-4 py-3">Company</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Registered</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-hairline">
              {summary.recentRegistrations.map((company) => (
                <tr key={company.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link
                      to={`/platform/companies/${company.id}`}
                      className="font-medium text-ink hover:text-primary-hover"
                    >
                      {company.name}
                    </Link>
                  </td>
                  <td className="px-4 py-3">
                    <CompanyStatusBadge status={company.status} />
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{new Date(company.registeredAtUtc).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
