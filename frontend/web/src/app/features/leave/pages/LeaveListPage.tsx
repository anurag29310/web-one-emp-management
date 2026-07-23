import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { useLeaveTypes } from '@/app/features/leave-types/hooks/useLeaveTypes'
import { useLeaveRequests } from '../hooks/useLeaveRequests'
import { leaveRepository } from '../api'
import { applyLeaveFormSchema, type ApplyLeaveFormValues } from '../types/leaveSchema'
import { LeaveStatusBadge } from '../components/LeaveStatusBadge'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

function ApplyLeaveForm({ onApplied }: { onApplied: () => void }) {
  const { result: employeesResult } = useEmployees({ pageSize: 100 })
  const { leaveTypes } = useLeaveTypes()
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ApplyLeaveFormValues>({
    resolver: zodResolver(applyLeaveFormSchema),
    defaultValues: { employeeId: '', leaveTypeId: '', startDate: '', endDate: '', reason: '' },
  })

  async function onSubmit(values: ApplyLeaveFormValues) {
    setFormError(null)
    const totalDays =
      Math.round((new Date(values.endDate).getTime() - new Date(values.startDate).getTime()) / 86_400_000) + 1

    try {
      await leaveRepository.apply({
        employeeId: values.employeeId,
        leaveTypeId: values.leaveTypeId,
        startDate: values.startDate,
        endDate: values.endDate,
        totalDays,
        reason: values.reason?.trim() || undefined,
      })
      reset()
      onApplied()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to submit leave request.')
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="apply-employee" className="mb-1 block text-sm font-medium text-ink-muted">
            Employee
          </label>
          <select
            id="apply-employee"
            aria-invalid={Boolean(errors.employeeId)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('employeeId')}
          >
            <option value="">Select employee…</option>
            {employeesResult?.data.map((employee) => (
              <option key={employee.id} value={employee.id}>
                {employee.fullName}
              </option>
            ))}
          </select>
          {errors.employeeId && <p className="mt-1 text-xs text-danger">{errors.employeeId.message}</p>}
        </div>
        <div>
          <label htmlFor="apply-leave-type" className="mb-1 block text-sm font-medium text-ink-muted">
            Leave type
          </label>
          <select
            id="apply-leave-type"
            aria-invalid={Boolean(errors.leaveTypeId)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('leaveTypeId')}
          >
            <option value="">Select leave type…</option>
            {leaveTypes.map((leaveType) => (
              <option key={leaveType.id} value={leaveType.id}>
                {leaveType.name}
              </option>
            ))}
          </select>
          {errors.leaveTypeId && <p className="mt-1 text-xs text-danger">{errors.leaveTypeId.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="apply-start" className="mb-1 block text-sm font-medium text-ink-muted">
            Start date
          </label>
          <input
            id="apply-start"
            type="date"
            aria-invalid={Boolean(errors.startDate)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('startDate')}
          />
          {errors.startDate && <p className="mt-1 text-xs text-danger">{errors.startDate.message}</p>}
        </div>
        <div>
          <label htmlFor="apply-end" className="mb-1 block text-sm font-medium text-ink-muted">
            End date
          </label>
          <input
            id="apply-end"
            type="date"
            aria-invalid={Boolean(errors.endDate)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('endDate')}
          />
          {errors.endDate && <p className="mt-1 text-xs text-danger">{errors.endDate.message}</p>}
        </div>
      </div>

      <div>
        <label htmlFor="apply-reason" className="mb-1 block text-sm font-medium text-ink-muted">
          Reason
        </label>
        <textarea
          id="apply-reason"
          rows={2}
          aria-invalid={Boolean(errors.reason)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
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
        {isSubmitting ? 'Submitting…' : 'Apply for leave'}
      </button>
    </form>
  )
}

export function LeaveListPage() {
  const { result, isLoading, error, refresh } = useLeaveRequests({ pageSize: 50 })
  const { result: employeesResult } = useEmployees({ pageSize: 100 })
  const { leaveTypes } = useLeaveTypes()
  const { user } = useAuth()
  const [isFormOpen, setIsFormOpen] = useState(false)

  const canApproveLeave = user?.role === 'Admin' || user?.role === 'HR' || user?.role === 'Manager'
  const canManageLeaves = user?.role === 'Admin' || user?.role === 'HR'

  function employeeName(employeeId: string): string {
    return employeesResult?.data.find((e) => e.id === employeeId)?.fullName ?? employeeId
  }

  function leaveTypeName(leaveTypeId: string): string {
    return leaveTypes.find((t) => t.id === leaveTypeId)?.name ?? leaveTypeId
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Leave Requests</h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        <div className="flex items-center gap-2">
          {canApproveLeave && (
            <Link
              to="/leave/approvals"
              className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
            >
              Approval queue
            </Link>
          )}
          {canManageLeaves && (
            <Link
              to="/leave/balances"
              className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
            >
              Leave balances
            </Link>
          )}
          <button
            type="button"
            onClick={() => setIsFormOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isFormOpen ? 'Cancel' : 'Apply for leave'}
          </button>
        </div>
      </div>

      <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="Apply for leave">
        <ApplyLeaveForm
          onApplied={() => {
            setIsFormOpen(false)
            refresh()
          }}
        />
      </Modal>

      {error && <p className="text-sm text-danger">{error}</p>}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Employee</th>
              <th className="px-4 py-3">Type</th>
              <th className="px-4 py-3">Dates</th>
              <th className="px-4 py-3">Days</th>
              <th className="px-4 py-3">Status</th>
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

            {!isLoading && result?.data.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={5}>
                  No leave requests yet.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((request) => (
                <tr key={request.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link to={`/leave/${request.id}`} className="font-medium text-ink hover:text-primary-hover">
                      {employeeName(request.employeeId)}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{leaveTypeName(request.leaveTypeId)}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {request.startDate} → {request.endDate}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{request.totalDays}</td>
                  <td className="px-4 py-3">
                    <LeaveStatusBadge status={request.status} />
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
