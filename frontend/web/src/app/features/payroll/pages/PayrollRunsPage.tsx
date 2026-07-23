import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { usePayrollRuns } from '../hooks/usePayrollRuns'
import { payrollRepository, type PayslipPreview } from '../api'
import { processPayrollFormSchema, type ProcessPayrollFormValues } from '../types/payrollSchema'
import { PayrollStatusBadge } from '../components/PayrollStatusBadge'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

function formatCurrency(amount: number): string {
  return amount.toLocaleString(undefined, { style: 'currency', currency: 'USD' })
}

function ProcessPayrollForm({ onProcessed }: { onProcessed: (payrollRunId: string) => void }) {
  const { result: employeesResult } = useEmployees({ pageSize: 100 })
  const [preview, setPreview] = useState<PayslipPreview[] | null>(null)
  const [previewError, setPreviewError] = useState<string | null>(null)
  const [isPreviewing, setIsPreviewing] = useState(false)
  const [processError, setProcessError] = useState<string | null>(null)
  const [isProcessing, setIsProcessing] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ProcessPayrollFormValues>({
    resolver: zodResolver(processPayrollFormSchema),
    defaultValues: { periodStart: '', periodEnd: '' },
  })

  function employeeName(employeeId: string): string {
    return employeesResult?.data.find((e) => e.id === employeeId)?.fullName ?? employeeId
  }

  async function onPreview(values: ProcessPayrollFormValues) {
    setPreviewError(null)
    setIsPreviewing(true)
    try {
      const data = await payrollRepository.dryRunPayroll(values)
      setPreview(data)
    } catch (err) {
      setPreview(null)
      setPreviewError(err instanceof AppError ? err.message : 'Failed to preview payroll.')
    } finally {
      setIsPreviewing(false)
    }
  }

  async function onProcess(values: ProcessPayrollFormValues) {
    setProcessError(null)
    setIsProcessing(true)
    try {
      const { payrollRunId } = await payrollRepository.processPayroll(values)
      onProcessed(payrollRunId)
    } catch (err) {
      setProcessError(err instanceof AppError ? err.message : 'Failed to process payroll.')
    } finally {
      setIsProcessing(false)
    }
  }

  const totalNetPay = preview?.reduce((sum, p) => sum + p.netPay, 0) ?? 0

  return (
    <form onSubmit={handleSubmit(onProcess)} noValidate className="space-y-4">
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="pp-start" className="mb-1 block text-sm font-medium text-ink-muted">
            Period start
          </label>
          <input
            id="pp-start"
            type="date"
            aria-invalid={Boolean(errors.periodStart)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('periodStart')}
          />
          {errors.periodStart && <p className="mt-1 text-xs text-danger">{errors.periodStart.message}</p>}
        </div>
        <div>
          <label htmlFor="pp-end" className="mb-1 block text-sm font-medium text-ink-muted">
            Period end
          </label>
          <input
            id="pp-end"
            type="date"
            aria-invalid={Boolean(errors.periodEnd)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('periodEnd')}
          />
          {errors.periodEnd && <p className="mt-1 text-xs text-danger">{errors.periodEnd.message}</p>}
        </div>
      </div>

      <button
        type="button"
        disabled={isPreviewing}
        onClick={() => void handleSubmit(onPreview)()}
        className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isPreviewing ? 'Previewing…' : 'Preview (dry run)'}
      </button>

      {previewError && (
        <p role="alert" className="text-sm text-danger">
          {previewError}
        </p>
      )}

      {preview && (
        <div className="overflow-hidden rounded-md border border-hairline">
          <table className="w-full text-sm">
            <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
              <tr>
                <th className="px-3 py-2">Employee</th>
                <th className="px-3 py-2">Gross pay</th>
                <th className="px-3 py-2">Net pay</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-hairline">
              {preview.length === 0 && (
                <tr>
                  <td className="px-3 py-4 text-center text-ink-subtle" colSpan={3}>
                    No employees have an active salary structure for this period.
                  </td>
                </tr>
              )}
              {preview.map((p) => (
                <tr key={p.employeeId}>
                  <td className="px-3 py-2 text-ink">{employeeName(p.employeeId)}</td>
                  <td className="px-3 py-2 text-ink-muted">{formatCurrency(p.grossPay)}</td>
                  <td className="px-3 py-2 text-ink-muted">{formatCurrency(p.netPay)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {preview.length > 0 && (
            <p className="border-t border-hairline bg-surface-2 px-3 py-2 text-xs text-ink-subtle">
              {preview.length} payslips · {formatCurrency(totalNetPay)} total net pay
            </p>
          )}
        </div>
      )}

      {processError && (
        <p role="alert" className="text-sm text-danger">
          {processError}
        </p>
      )}

      <button
        type="submit"
        disabled={isProcessing}
        className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isProcessing ? 'Processing…' : 'Process payroll'}
      </button>
    </form>
  )
}

export function PayrollRunsPage() {
  const { user } = useAuth()
  const canManagePayroll = user?.role === 'Admin' || user?.role === 'HR'
  const { runs, isLoading, error, refresh } = usePayrollRuns()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const navigate = useNavigate()

  if (!canManagePayroll) {
    return (
      <div className="rounded-lg border border-hairline bg-surface-1 p-6 text-sm text-ink-subtle">
        You don&apos;t have access to payroll processing.
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Payroll Runs</h1>
          <p className="text-sm text-ink-subtle">{runs.length} runs</p>
        </div>
        <button
          type="button"
          onClick={() => setIsFormOpen((open) => !open)}
          className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
        >
          {isFormOpen ? 'Cancel' : 'Process payroll'}
        </button>
      </div>

      <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="Process payroll">
        <ProcessPayrollForm
          onProcessed={(payrollRunId) => {
            setIsFormOpen(false)
            refresh()
            navigate(`/payroll/runs/${payrollRunId}`)
          }}
        />
      </Modal>

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Period</th>
              <th className="px-4 py-3">Processed</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">Payslips</th>
              <th className="px-4 py-3">Total net pay</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={5}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && runs.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={5}>
                  No payroll runs yet.
                </td>
              </tr>
            )}

            {!isLoading &&
              runs.map((run) => (
                <tr key={run.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link to={`/payroll/runs/${run.id}`} className="font-medium text-ink hover:text-primary-hover">
                      {run.periodStart} → {run.periodEnd}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{new Date(run.processedAtUtc).toLocaleDateString()}</td>
                  <td className="px-4 py-3">
                    <PayrollStatusBadge status={run.status} />
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{run.payslipCount}</td>
                  <td className="px-4 py-3 text-ink-muted">{formatCurrency(run.totalNetPay)}</td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
