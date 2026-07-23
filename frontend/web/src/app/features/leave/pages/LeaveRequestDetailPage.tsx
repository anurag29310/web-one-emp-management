import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { useLeaveTypes } from '@/app/features/leave-types/hooks/useLeaveTypes'
import { useLeaveRequest } from '../hooks/useLeaveRequest'
import { leaveRepository } from '../api'
import { leaveDecisionFormSchema, type LeaveDecisionFormValues } from '../types/leaveDecisionSchema'
import { LeaveStatusBadge } from '../components/LeaveStatusBadge'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 text-sm text-ink">{value}</p>
    </div>
  )
}

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
        <label htmlFor="decision-comments" className="mb-1 block text-sm font-medium text-ink-muted">
          Comments
        </label>
        <textarea
          id="decision-comments"
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

export function LeaveRequestDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { leaveRequest, isLoading, error, refresh } = useLeaveRequest(id)
  const { result: employeesResult } = useEmployees({ pageSize: 100 })
  const { leaveTypes } = useLeaveTypes()
  const { user } = useAuth()

  const [decisionMode, setDecisionMode] = useState<'approve' | 'reject' | null>(null)
  const [isCancelling, setIsCancelling] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  const canApproveLeave = user?.role === 'Admin' || user?.role === 'HR' || user?.role === 'Manager'

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!leaveRequest) return null

  const employee = employeesResult?.data.find((e) => e.id === leaveRequest.employeeId)
  const approver = employeesResult?.data.find((e) => e.id === leaveRequest.approverEmployeeId)
  const leaveType = leaveTypes.find((t) => t.id === leaveRequest.leaveTypeId)
  const isPending = leaveRequest.status === 'Pending'

  async function handleDecision(comments: string | undefined) {
    if (!leaveRequest || !decisionMode) return
    if (decisionMode === 'approve') {
      await leaveRepository.approve(leaveRequest.id, comments)
    } else {
      await leaveRepository.reject(leaveRequest.id, comments)
    }
    setDecisionMode(null)
    refresh()
  }

  async function handleCancel() {
    if (!leaveRequest) return
    setIsCancelling(true)
    setActionError(null)
    try {
      await leaveRepository.cancel(leaveRequest.id)
      refresh()
    } catch (err) {
      setActionError(err instanceof AppError ? err.message : 'Failed to cancel leave request.')
    } finally {
      setIsCancelling(false)
    }
  }

  return (
    <div className="space-y-4">
      <Link to="/leave" className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover">
        ← Back to leave requests
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
              {employee?.fullName ?? leaveRequest.employeeId}
            </h1>
            <p className="text-sm text-ink-subtle">{leaveType?.name ?? leaveRequest.leaveTypeId}</p>
          </div>
          <div className="flex items-center gap-3">
            <LeaveStatusBadge status={leaveRequest.status} />
            {isPending && (
              <button
                type="button"
                disabled={isCancelling}
                onClick={() => void handleCancel()}
                className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isCancelling ? 'Cancelling…' : 'Cancel request'}
              </button>
            )}
            {isPending && canApproveLeave && (
              <>
                <button
                  type="button"
                  onClick={() => setDecisionMode('approve')}
                  className="rounded-md bg-success/15 px-3 py-2 text-sm font-medium text-success transition hover:bg-success/25"
                >
                  Approve
                </button>
                <button
                  type="button"
                  onClick={() => setDecisionMode('reject')}
                  className="rounded-md bg-danger/15 px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/25"
                >
                  Reject
                </button>
              </>
            )}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Start date" value={leaveRequest.startDate} />
          <Field label="End date" value={leaveRequest.endDate} />
          <Field label="Total days" value={String(leaveRequest.totalDays)} />
          <Field label="Reason" value={leaveRequest.reason ?? '—'} />
        </div>

        {actionError && (
          <p role="alert" className="px-6 pb-4 text-sm text-danger">
            {actionError}
          </p>
        )}
      </div>

      <div className="rounded-lg border border-hairline bg-surface-1 p-6">
        <h2 className="mb-4 text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">History</h2>
        <ul className="space-y-4">
          <li className="flex items-start gap-3">
            <span className="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-primary" />
            <div>
              <p className="text-sm text-ink">Submitted</p>
              <p className="text-xs text-ink-subtle">{new Date(leaveRequest.createdAtUtc).toLocaleString()}</p>
            </div>
          </li>
          {leaveRequest.decisionAtUtc && (
            <li className="flex items-start gap-3">
              <span
                className={`mt-1.5 h-2 w-2 shrink-0 rounded-full ${
                  leaveRequest.status === 'Approved' ? 'bg-success' : 'bg-danger'
                }`}
              />
              <div>
                <p className="text-sm text-ink">
                  {leaveRequest.status}
                  {approver ? ` by ${approver.fullName}` : ''}
                </p>
                <p className="text-xs text-ink-subtle">{new Date(leaveRequest.decisionAtUtc).toLocaleString()}</p>
                {leaveRequest.decisionComments && (
                  <p className="mt-1 text-sm text-ink-muted">“{leaveRequest.decisionComments}”</p>
                )}
              </div>
            </li>
          )}
          {leaveRequest.status === 'Cancelled' && !leaveRequest.decisionAtUtc && (
            <li className="flex items-start gap-3">
              <span className="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-ink-tertiary" />
              <p className="text-sm text-ink">Cancelled by the requester</p>
            </li>
          )}
        </ul>
      </div>

      <Modal
        isOpen={decisionMode !== null}
        onClose={() => setDecisionMode(null)}
        title={decisionMode === 'approve' ? 'Approve leave request' : 'Reject leave request'}
      >
        <DecisionForm
          submitLabel={decisionMode === 'approve' ? 'Approve' : 'Reject'}
          onSubmit={handleDecision}
        />
      </Modal>
    </div>
  )
}
