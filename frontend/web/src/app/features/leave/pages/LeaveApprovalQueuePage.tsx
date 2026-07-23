import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { useLeaveTypes } from '@/app/features/leave-types/hooks/useLeaveTypes'
import { useLeaveRequests } from '../hooks/useLeaveRequests'
import { leaveRepository, type LeaveRequest } from '../api'
import { leaveDecisionFormSchema, type LeaveDecisionFormValues } from '../types/leaveDecisionSchema'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

function DecisionForm({
  submitLabel,
  onSubmit,
}: {
  submitLabel: string
  onSubmit: (comments: string | undefined) => Promise<void>
}) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { isSubmitting },
  } = useForm<LeaveDecisionFormValues>({
    resolver: zodResolver(leaveDecisionFormSchema),
    defaultValues: { comments: '' },
  })

  async function submit(values: LeaveDecisionFormValues) {
    setFormError(null)
    try {
      await onSubmit(values.comments?.trim() || undefined)
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to record decision.')
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="space-y-3">
      <div>
        <label htmlFor="queue-decision-comments" className="mb-1 block text-sm font-medium text-ink-muted">
          Comments
        </label>
        <textarea
          id="queue-decision-comments"
          rows={3}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('comments')}
        />
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
        {isSubmitting ? 'Saving…' : submitLabel}
      </button>
    </form>
  )
}

export function LeaveApprovalQueuePage() {
  const { user } = useAuth()
  const canApproveLeave = user?.role === 'Admin' || user?.role === 'HR' || user?.role === 'Manager'

  const { result, isLoading, error, refresh } = useLeaveRequests({
    status: 'Pending',
    pageSize: 100,
  })
  const { result: employeesResult } = useEmployees({ pageSize: 100 })
  const { leaveTypes } = useLeaveTypes()

  const [target, setTarget] = useState<{ request: LeaveRequest; decision: 'approve' | 'reject' } | null>(null)

  if (!canApproveLeave) {
    return (
      <div className="rounded-lg border border-hairline bg-surface-1 p-6">
        <p className="text-sm text-danger">You don't have permission to view the approval queue.</p>
      </div>
    )
  }

  function employeeName(employeeId: string): string {
    return employeesResult?.data.find((e) => e.id === employeeId)?.fullName ?? employeeId
  }

  function leaveTypeName(leaveTypeId: string): string {
    return leaveTypes.find((t) => t.id === leaveTypeId)?.name ?? leaveTypeId
  }

  async function handleDecision(comments: string | undefined) {
    if (!target) return
    if (target.decision === 'approve') {
      await leaveRepository.approve(target.request.id, comments)
    } else {
      await leaveRepository.reject(target.request.id, comments)
    }
    setTarget(null)
    refresh()
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Leave Approvals</h1>
        <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} pending` : ' '}</p>
      </div>

      {error && <p className="text-sm text-danger">{error}</p>}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Employee</th>
              <th className="px-4 py-3">Type</th>
              <th className="px-4 py-3">Dates</th>
              <th className="px-4 py-3">Days</th>
              <th className="px-4 py-3">Reason</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={6}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && result?.data.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={6}>
                  No pending requests.
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
                  <td className="px-4 py-3 text-ink-muted">{request.reason ?? '—'}</td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex justify-end gap-3">
                      <button
                        type="button"
                        onClick={() => setTarget({ request, decision: 'approve' })}
                        className="text-xs font-medium text-success hover:underline"
                      >
                        Approve
                      </button>
                      <button
                        type="button"
                        onClick={() => setTarget({ request, decision: 'reject' })}
                        className="text-xs font-medium text-danger hover:underline"
                      >
                        Reject
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      <Modal
        isOpen={target !== null}
        onClose={() => setTarget(null)}
        title={target?.decision === 'approve' ? 'Approve leave request' : 'Reject leave request'}
      >
        <DecisionForm
          submitLabel={target?.decision === 'approve' ? 'Approve' : 'Reject'}
          onSubmit={handleDecision}
        />
      </Modal>
    </div>
  )
}
