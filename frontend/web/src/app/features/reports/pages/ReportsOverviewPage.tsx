import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useEmployeeReport } from '../hooks/useEmployeeReport'
import { useDepartmentCounts } from '../hooks/useDepartmentCounts'
import { useLeaveSummaryReport } from '../hooks/useLeaveSummaryReport'
import { useEmployeeTurnoverReport } from '../hooks/useEmployeeTurnoverReport'
import { useFileDownload } from '../hooks/useFileDownload'
import { reportsRepository } from '../api'
import { dateRangeFormSchema, type DateRangeFormValues } from '../types/dateRangeSchema'

function firstDayOfYear(): string {
  const now = new Date()
  return `${now.getFullYear()}-01-01`
}

function today(): string {
  return new Date().toISOString().slice(0, 10)
}

const METRIC_STYLES = {
  primary: { dot: 'bg-primary', text: 'text-ink' },
  success: { dot: 'bg-success', text: 'text-ink' },
  neutral: { dot: 'bg-ink-tertiary', text: 'text-ink' },
  warning: { dot: 'bg-warning', text: 'text-ink' },
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

function SectionCard({
  title,
  action,
  children,
}: {
  title: string
  action?: ReactNode
  children: ReactNode
}) {
  return (
    <section className="rounded-lg border border-hairline bg-surface-1 p-5">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">{title}</h2>
        {action}
      </div>
      {children}
    </section>
  )
}

export function ReportsOverviewPage() {
  const { report: employeeReport, isLoading: isEmployeeReportLoading, error: employeeReportError } =
    useEmployeeReport()
  const { counts, isLoading: isCountsLoading, error: countsError } = useDepartmentCounts()
  const departmentExport = useFileDownload()
  const turnoverExport = useFileDownload()

  const [range, setRange] = useState<DateRangeFormValues>({ from: firstDayOfYear(), to: today() })

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<DateRangeFormValues>({ resolver: zodResolver(dateRangeFormSchema), defaultValues: range })

  const { report: leaveSummary, isLoading: isLeaveSummaryLoading, error: leaveSummaryError } =
    useLeaveSummaryReport(range)
  const { entries: turnoverEntries, isLoading: isTurnoverLoading, error: turnoverError } =
    useEmployeeTurnoverReport(range)

  const maxDeptCount = Math.max(...counts.map((c) => c.employeeCount), 1)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Reports</h1>
          <p className="text-sm text-ink-subtle">Organization-wide reporting and data exports</p>
        </div>
        <Link
          to="/reports/exports"
          className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
        >
          Data exports
        </Link>
      </div>

      <SectionCard title="Employee headcount">
        {isEmployeeReportLoading && (
          <div className="grid grid-cols-3 gap-4">
            {Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="h-24 animate-pulse rounded-lg bg-surface-2" />
            ))}
          </div>
        )}
        {employeeReportError && <p className="text-sm text-danger">{employeeReportError}</p>}
        {employeeReport && (
          <div className="grid grid-cols-3 gap-4">
            <MetricCard label="Total" value={employeeReport.totalEmployees} tone="primary" />
            <MetricCard label="Active" value={employeeReport.activeEmployees} tone="success" />
            <MetricCard label="Inactive" value={employeeReport.inactiveEmployees} tone="neutral" />
          </div>
        )}
      </SectionCard>

      <SectionCard
        title="Headcount by department"
        action={
          <button
            type="button"
            disabled={departmentExport.isDownloading || counts.length === 0}
            onClick={() => void departmentExport.trigger(() => reportsRepository.exportDepartmentCounts())}
            className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {departmentExport.isDownloading ? 'Exporting…' : 'Export CSV'}
          </button>
        }
      >
        {departmentExport.error && <p className="mb-3 text-sm text-danger">{departmentExport.error}</p>}
        {countsError && <p className="text-sm text-danger">{countsError}</p>}
        {isCountsLoading && (
          <div className="space-y-3">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="h-5 animate-pulse rounded bg-surface-2" />
            ))}
          </div>
        )}
        {!isCountsLoading && counts.length === 0 && !countsError && (
          <p className="text-sm text-ink-subtle">No department data available.</p>
        )}
        {!isCountsLoading && counts.length > 0 && (
          <ul className="space-y-3">
            {counts.map((department) => (
              <li key={department.departmentId}>
                <div className="mb-1 flex justify-between text-sm">
                  <span className="font-medium text-ink-muted">{department.departmentName}</span>
                  <span className="text-ink-subtle">{department.employeeCount} employees</span>
                </div>
                <div className="h-1.5 w-full overflow-hidden rounded-full bg-surface-2">
                  <div
                    className="h-full rounded-full bg-primary"
                    style={{ width: `${(department.employeeCount / maxDeptCount) * 100}%` }}
                  />
                </div>
              </li>
            ))}
          </ul>
        )}
      </SectionCard>

      <div className="rounded-lg border border-hairline bg-surface-1 p-5">
        <h2 className="mb-4 text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">Date range</h2>
        <form
          onSubmit={handleSubmit((values) => setRange(values))}
          noValidate
          className="flex flex-wrap items-end gap-3"
        >
          <div>
            <label htmlFor="report-from" className="mb-1 block text-sm font-medium text-ink-muted">
              From
            </label>
            <input
              id="report-from"
              type="date"
              aria-invalid={Boolean(errors.from)}
              className="rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...register('from')}
            />
            {errors.from && <p className="mt-1 text-xs text-danger">{errors.from.message}</p>}
          </div>
          <div>
            <label htmlFor="report-to" className="mb-1 block text-sm font-medium text-ink-muted">
              To
            </label>
            <input
              id="report-to"
              type="date"
              aria-invalid={Boolean(errors.to)}
              className="rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...register('to')}
            />
            {errors.to && <p className="mt-1 text-xs text-danger">{errors.to.message}</p>}
          </div>
          <button
            type="submit"
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            Apply range
          </button>
        </form>
        <p className="mt-2 text-xs text-ink-subtle">
          Applies to the leave summary and employee turnover reports below.
        </p>
      </div>

      <SectionCard title="Leave summary">
        {leaveSummaryError && <p className="mb-3 text-sm text-danger">{leaveSummaryError}</p>}
        {isLeaveSummaryLoading && (
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="h-24 animate-pulse rounded-lg bg-surface-2" />
            ))}
          </div>
        )}
        {leaveSummary && (
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            <MetricCard label="Total requests" value={leaveSummary.totalRequests} tone="primary" />
            <MetricCard label="Pending" value={leaveSummary.pending} tone="warning" />
            <MetricCard label="Approved" value={leaveSummary.approved} tone="success" />
            <MetricCard label="Rejected" value={leaveSummary.rejected} tone="danger" />
          </div>
        )}
      </SectionCard>

      <SectionCard
        title="Employee turnover"
        action={
          <button
            type="button"
            disabled={turnoverExport.isDownloading}
            onClick={() => void turnoverExport.trigger(() => reportsRepository.exportEmployeeTurnover(range))}
            className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {turnoverExport.isDownloading ? 'Exporting…' : 'Export CSV'}
          </button>
        }
      >
        {turnoverExport.error && <p className="mb-3 text-sm text-danger">{turnoverExport.error}</p>}
        {turnoverError && <p className="text-sm text-danger">{turnoverError}</p>}

        <div className="overflow-hidden rounded-lg border border-hairline">
          <table className="w-full text-sm">
            <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
              <tr>
                <th className="px-4 py-3">Employee</th>
                <th className="px-4 py-3">Join date</th>
                <th className="px-4 py-3">Exit date</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-hairline">
              {isTurnoverLoading &&
                Array.from({ length: 3 }).map((_, i) => (
                  <tr key={i}>
                    <td className="px-4 py-3" colSpan={3}>
                      <div className="h-5 animate-pulse rounded bg-surface-2" />
                    </td>
                  </tr>
                ))}

              {!isTurnoverLoading && turnoverEntries.length === 0 && (
                <tr>
                  <td className="px-4 py-8 text-center text-ink-subtle" colSpan={3}>
                    No joiners or leavers in this date range.
                  </td>
                </tr>
              )}

              {!isTurnoverLoading &&
                turnoverEntries.map((entry) => (
                  <tr key={entry.employeeId} className="transition hover:bg-surface-2">
                    <td className="px-4 py-3 font-medium text-ink">{entry.employeeName}</td>
                    <td className="px-4 py-3 text-ink-muted">{entry.joinDate}</td>
                    <td className="px-4 py-3 text-ink-muted">{entry.exitDate ?? '—'}</td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      </SectionCard>
    </div>
  )
}
