import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useAttendanceCorrections } from '../hooks/useAttendanceCorrections'
import { attendanceRepository, type AttendanceCorrection, type CorrectionStatus } from '../api'
import { decisionSchema, type DecisionFormValues } from '../types/attendanceSchemas'
import { Pagination } from '../components/Pagination'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { StatusBadge } from '@/app/shared/components/StatusBadge'
import { Modal } from '@/app/shared/components/Modal'

const CORRECTION_STATUS_OPTIONS: CorrectionStatus[] = ['Pending', 'Approved', 'Rejected']

function formatDateTime(iso: string | null): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })
}

function DecisionForm({
  correction,
  action,
  onClose,
  onDecided,
}: {
  correction: AttendanceCorrection
  action: 'approve' | 'reject'
  onClose: () => void
  onDecided: () => void
}) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { isSubmitting },
  } = useForm<DecisionFormValues>({ resolver: zodResolver(decisionSchema), defaultValues: { comments: '' } })

  async function submit(values: DecisionFormValues) {
    setFormError(null)
    try {
      const input = values.comments?.trim() ? { comments: values.comments.trim() } : undefined
      if (action === 'approve') {
        await attendanceRepository.approveCorrection(correction.id, input)
      } else {
        await attendanceRepository.rejectCorrection(correction.id, input)
      }
      onDecided()
      onClose()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : `Failed to ${action} correction request.`)
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="space-y-3">
      <p className="text-sm text-ink-subtle">{correction.reason}</p>
      <div>
        <label htmlFor="decision-comments" className="mb-1 block text-sm font-medium text-ink-muted">
          Comments (optional)
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
        className={`rounded-md px-3 py-2 text-sm font-medium text-white transition disabled:cursor-not-allowed disabled:opacity-60 ${
          action === 'approve' ? 'bg-primary hover:bg-primary-hover' : 'bg-danger hover:bg-danger/90'
        }`}
      >
        {isSubmitting ? 'Submitting…' : action === 'approve' ? 'Approve request' : 'Reject request'}
      </button>
    </form>
  )
}

export function AttendanceCorrectionsPage() {
  const { user } = useAuth()
  const canReview = user?.role === 'Admin' || user?.role === 'HR' || user?.role === 'Manager'

  const [page, setPage] = useState(1)
  const [status, setStatus] = useState<CorrectionStatus | ''>('Pending')
  const [decisionState, setDecisionState] = useState<{ correction: AttendanceCorrection; action: 'approve' | 'reject' } | null>(null)

  const { result: employeeResult } = useEmployees({ pageSize: 100 })
  const employeesById = useMemo(() => new Map((employeeResult?.data ?? []).map((e) => [e.id, e])), [employeeResult])

  const { result, isLoading, error, refresh } = useAttendanceCorrections({
    page,
    pageSize: 20,
    status: status || undefined,
  })

  if (!canReview) {
    return (
      <div className="space-y-4">
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Attendance Corrections</h1>
        <p className="text-sm text-ink-subtle">You don't have permission to review correction requests.</p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Attendance Corrections</h1>
        <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
      </div>

      <div className="flex flex-wrap gap-3 rounded-lg border border-hairline bg-surface-1 p-4">
        <select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as CorrectionStatus | '')
            setPage(1)
          }}
          className="rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        >
          <option value="">All statuses</option>
          {CORRECTION_STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
      </div>

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Employee</th>
              <th className="px-4 py-3">Requested check-in</th>
              <th className="px-4 py-3">Requested check-out</th>
              <th className="px-4 py-3">Reason</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 4 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={6}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && result?.data.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={6}>
                  No correction requests match your filters.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((correction) => (
                <tr key={correction.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3 font-medium text-ink">
                    {employeesById.get(correction.requestedByEmployeeId)?.fullName ?? correction.requestedByEmployeeId}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{formatDateTime(correction.requestedCheckInAtUtc)}</td>
                  <td className="px-4 py-3 text-ink-muted">{formatDateTime(correction.requestedCheckOutAtUtc)}</td>
                  <td className="max-w-xs truncate px-4 py-3 text-ink-muted" title={correction.reason}>
                    {correction.reason}
                  </td>
                  <td className="px-4 py-3">
                    <StatusBadge status={correction.status} />
                  </td>
                  <td className="px-4 py-3 text-right">
                    {correction.status === 'Pending' && (
                      <div className="flex justify-end gap-3">
                        <button
                          type="button"
                          onClick={() => setDecisionState({ correction, action: 'approve' })}
                          className="text-xs font-medium text-primary hover:text-primary-hover"
                        >
                          Approve
                        </button>
                        <button
                          type="button"
                          onClick={() => setDecisionState({ correction, action: 'reject' })}
                          className="text-xs font-medium text-danger hover:underline"
                        >
                          Reject
                        </button>
                      </div>
                    )}
                    {correction.status !== 'Pending' && correction.decisionComments && (
                      <span className="text-xs text-ink-subtle" title={correction.decisionComments}>
                        {correction.decisionComments}
                      </span>
                    )}
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      {result && <Pagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} onPageChange={setPage} />}

      <Modal
        isOpen={decisionState !== null}
        onClose={() => setDecisionState(null)}
        title={decisionState?.action === 'approve' ? 'Approve correction request' : 'Reject correction request'}
      >
        {decisionState && (
          <DecisionForm
            correction={decisionState.correction}
            action={decisionState.action}
            onClose={() => setDecisionState(null)}
            onDecided={refresh}
          />
        )}
      </Modal>
    </div>
  )
}
