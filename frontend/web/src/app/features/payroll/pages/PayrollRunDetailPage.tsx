import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { usePayrollRun } from '../hooks/usePayrollRun'
import { payrollRepository, type Payslip } from '../api'
import { PayrollStatusBadge } from '../components/PayrollStatusBadge'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'

function formatCurrency(amount: number): string {
  return amount.toLocaleString(undefined, { style: 'currency', currency: 'USD' })
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 text-sm text-ink">{value}</p>
    </div>
  )
}

export function PayrollRunDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { user } = useAuth()
  const canManagePayroll = user?.role === 'Admin' || user?.role === 'HR'
  const canApprovePayroll = user?.role === 'Admin'

  const { run, isLoading, error, refresh } = usePayrollRun(id)
  const { result: employeesResult } = useEmployees({ pageSize: 100 })

  const [isApproving, setIsApproving] = useState(false)
  const [approveError, setApproveError] = useState<string | null>(null)
  const [downloadingId, setDownloadingId] = useState<string | null>(null)
  const [downloadError, setDownloadError] = useState<string | null>(null)

  function employeeName(employeeId: string): string {
    return employeesResult?.data.find((e) => e.id === employeeId)?.fullName ?? employeeId
  }

  async function handleApprove() {
    if (!run) return
    setApproveError(null)
    setIsApproving(true)
    try {
      await payrollRepository.approveRun(run.id)
      refresh()
    } catch (err) {
      setApproveError(err instanceof AppError ? err.message : 'Failed to approve payroll run.')
    } finally {
      setIsApproving(false)
    }
  }

  async function handleDownload(payslip: Payslip) {
    setDownloadError(null)
    setDownloadingId(payslip.id)
    try {
      const { blob, fileName } = await payrollRepository.downloadPayslip(payslip.id)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (err) {
      setDownloadError(err instanceof AppError ? err.message : 'Failed to download payslip.')
    } finally {
      setDownloadingId(null)
    }
  }

  if (!canManagePayroll) {
    return (
      <div className="rounded-lg border border-hairline bg-surface-1 p-6 text-sm text-ink-subtle">
        You don&apos;t have access to payroll runs.
      </div>
    )
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!run) return null

  return (
    <div className="space-y-4">
      <Link
        to="/payroll/runs"
        className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover"
      >
        ← Back to payroll runs
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
              {run.periodStart} → {run.periodEnd}
            </h1>
            <div className="mt-2">
              <PayrollStatusBadge status={run.status} />
            </div>
          </div>
          {canApprovePayroll && run.status === 'Completed' && (
            <button
              type="button"
              disabled={isApproving}
              onClick={() => void handleApprove()}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isApproving ? 'Approving…' : 'Approve run'}
            </button>
          )}
        </div>

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Processed" value={new Date(run.processedAtUtc).toLocaleString()} />
          <Field label="Payslips" value={String(run.payslipCount)} />
          <Field label="Total net pay" value={formatCurrency(run.totalNetPay)} />
        </div>

        {approveError && (
          <p role="alert" className="px-6 pb-6 text-sm text-danger">
            {approveError}
          </p>
        )}
      </div>

      {downloadError && (
        <p role="alert" className="text-sm text-danger">
          {downloadError}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Employee</th>
              <th className="px-4 py-3">Basic</th>
              <th className="px-4 py-3">Allowances</th>
              <th className="px-4 py-3">Deductions</th>
              <th className="px-4 py-3">Gross pay</th>
              <th className="px-4 py-3">Net pay</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {run.payslips.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={7}>
                  No payslips for this run.
                </td>
              </tr>
            )}
            {run.payslips.map((payslip) => (
              <tr key={payslip.id} className="transition hover:bg-surface-2">
                <td className="px-4 py-3 font-medium text-ink">{employeeName(payslip.employeeId)}</td>
                <td className="px-4 py-3 text-ink-muted">{formatCurrency(payslip.basic)}</td>
                <td className="px-4 py-3 text-ink-muted">{formatCurrency(payslip.totalAllowances)}</td>
                <td className="px-4 py-3 text-ink-muted">{formatCurrency(payslip.totalDeductions)}</td>
                <td className="px-4 py-3 text-ink-muted">{formatCurrency(payslip.grossPay)}</td>
                <td className="px-4 py-3 font-medium text-ink">{formatCurrency(payslip.netPay)}</td>
                <td className="px-4 py-3 text-right">
                  {payslip.hasDocument ? (
                    <button
                      type="button"
                      disabled={downloadingId === payslip.id}
                      onClick={() => void handleDownload(payslip)}
                      className="text-xs font-medium text-primary-hover hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      {downloadingId === payslip.id ? 'Downloading…' : 'Download PDF'}
                    </button>
                  ) : (
                    <span className="text-xs text-ink-subtle">No document</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
