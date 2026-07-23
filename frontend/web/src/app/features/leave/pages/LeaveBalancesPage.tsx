import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { useLeaveTypes } from '@/app/features/leave-types/hooks/useLeaveTypes'
import { useLeaveBalances } from '../hooks/useLeaveBalances'
import { leaveRepository, type LeaveBalance } from '../api'
import { leaveBalanceFormSchema, type LeaveBalanceFormValues } from '../types/leaveBalanceSchema'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

function AdjustBalanceForm({
  employeeId,
  balance,
  leaveTypeName,
  onSaved,
}: {
  employeeId: string
  balance: LeaveBalance
  leaveTypeName: string
  onSaved: () => void
}) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LeaveBalanceFormValues>({
    resolver: zodResolver(leaveBalanceFormSchema),
    defaultValues: {
      leaveTypeId: balance.leaveTypeId,
      year: String(balance.year),
      adjusted: String(balance.adjusted),
      reason: '',
    },
  })

  async function submit(values: LeaveBalanceFormValues) {
    setFormError(null)
    try {
      await leaveRepository.adjustBalance({
        employeeId,
        leaveTypeId: values.leaveTypeId,
        year: Number(values.year),
        adjusted: Number(values.adjusted),
        reason: values.reason?.trim() || undefined,
      })
      onSaved()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to adjust leave balance.')
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="space-y-3">
      <p className="text-sm text-ink-subtle">
        Adjusting <span className="font-medium text-ink">{leaveTypeName}</span>
      </p>
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="bal-year" className="mb-1 block text-sm font-medium text-ink-muted">
            Year
          </label>
          <input
            id="bal-year"
            inputMode="numeric"
            aria-invalid={Boolean(errors.year)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('year')}
          />
          {errors.year && <p className="mt-1 text-xs text-danger">{errors.year.message}</p>}
        </div>
        <div>
          <label htmlFor="bal-adjusted" className="mb-1 block text-sm font-medium text-ink-muted">
            Adjustment (days)
          </label>
          <input
            id="bal-adjusted"
            inputMode="decimal"
            aria-invalid={Boolean(errors.adjusted)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('adjusted')}
          />
          {errors.adjusted && <p className="mt-1 text-xs text-danger">{errors.adjusted.message}</p>}
        </div>
      </div>
      <div>
        <label htmlFor="bal-reason" className="mb-1 block text-sm font-medium text-ink-muted">
          Reason
        </label>
        <textarea
          id="bal-reason"
          rows={2}
          placeholder="e.g. Carry-forward correction"
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('reason')}
        />
        {errors.reason && <p className="mt-1 text-xs text-danger">{errors.reason.message}</p>}
      </div>
      {formError && (
        <p role="alert" className="text-sm text-danger">
          {formError}
        </p>
      )}
      <button
        type="submit"
        disabled={isSubmitting}
        className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isSubmitting ? 'Saving…' : 'Save adjustment'}
      </button>
    </form>
  )
}

export function LeaveBalancesPage() {
  const { user } = useAuth()
  const canManageLeaves = user?.role === 'Admin' || user?.role === 'HR'

  const { result: employeesResult } = useEmployees({ pageSize: 100 })
  const { leaveTypes } = useLeaveTypes()
  const [employeeId, setEmployeeId] = useState('')
  const { balances, isLoading, error, refresh } = useLeaveBalances(employeeId || undefined)
  const [adjustingBalance, setAdjustingBalance] = useState<LeaveBalance | null>(null)

  if (!canManageLeaves) {
    return (
      <div className="rounded-lg border border-hairline bg-surface-1 p-6">
        <p className="text-sm text-danger">You don't have permission to view leave balances.</p>
      </div>
    )
  }

  function leaveTypeName(leaveTypeId: string): string {
    return leaveTypes.find((t) => t.id === leaveTypeId)?.name ?? leaveTypeId
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Leave Balances</h1>
        <p className="text-sm text-ink-subtle">View and adjust an employee's leave balances</p>
      </div>

      <div className="max-w-sm">
        <label htmlFor="bal-employee" className="mb-1 block text-sm font-medium text-ink-muted">
          Employee
        </label>
        <select
          id="bal-employee"
          value={employeeId}
          onChange={(e) => setEmployeeId(e.target.value)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        >
          <option value="">Select employee…</option>
          {employeesResult?.data.map((employee) => (
            <option key={employee.id} value={employee.id}>
              {employee.fullName}
            </option>
          ))}
        </select>
      </div>

      {error && <p className="text-sm text-danger">{error}</p>}

      {!employeeId && (
        <div className="rounded-lg border border-hairline bg-surface-1 px-4 py-8 text-center text-sm text-ink-subtle">
          Select an employee to view their leave balances.
        </div>
      )}

      {employeeId && (
        <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
          <table className="w-full text-sm">
            <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
              <tr>
                <th className="px-4 py-3">Leave type</th>
                <th className="px-4 py-3">Year</th>
                <th className="px-4 py-3">Opening</th>
                <th className="px-4 py-3">Accrued</th>
                <th className="px-4 py-3">Used</th>
                <th className="px-4 py-3">Adjusted</th>
                <th className="px-4 py-3">Available</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-hairline">
              {isLoading &&
                Array.from({ length: 3 }).map((_, i) => (
                  <tr key={i}>
                    <td className="px-4 py-3" colSpan={8}>
                      <div className="h-5 animate-pulse rounded bg-surface-2" />
                    </td>
                  </tr>
                ))}

              {!isLoading && balances.length === 0 && (
                <tr>
                  <td className="px-4 py-8 text-center text-ink-subtle" colSpan={8}>
                    No balances recorded for this employee.
                  </td>
                </tr>
              )}

              {!isLoading &&
                balances.map((balance) => (
                  <tr key={balance.id} className="transition hover:bg-surface-2">
                    <td className="px-4 py-3 font-medium text-ink">{leaveTypeName(balance.leaveTypeId)}</td>
                    <td className="px-4 py-3 text-ink-muted">{balance.year}</td>
                    <td className="px-4 py-3 text-ink-muted">{balance.openingBalance}</td>
                    <td className="px-4 py-3 text-ink-muted">{balance.accrued}</td>
                    <td className="px-4 py-3 text-ink-muted">{balance.used}</td>
                    <td className="px-4 py-3 text-ink-muted">{balance.adjusted}</td>
                    <td className="px-4 py-3 font-medium text-ink">{balance.available}</td>
                    <td className="px-4 py-3 text-right">
                      <button
                        type="button"
                        onClick={() => setAdjustingBalance(balance)}
                        className="text-xs font-medium text-primary-hover hover:underline"
                      >
                        Adjust
                      </button>
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      )}

      <Modal isOpen={adjustingBalance !== null} onClose={() => setAdjustingBalance(null)} title="Adjust leave balance">
        {adjustingBalance && (
          <AdjustBalanceForm
            employeeId={employeeId}
            balance={adjustingBalance}
            leaveTypeName={leaveTypeName(adjustingBalance.leaveTypeId)}
            onSaved={() => {
              setAdjustingBalance(null)
              refresh()
            }}
          />
        )}
      </Modal>
    </div>
  )
}
