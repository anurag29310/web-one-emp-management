import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useReimbursements } from '../hooks/useReimbursements'
import { reimbursementRepository, type Reimbursement } from '../api'
import { reviewRemarksFormSchema, type ReviewRemarksFormValues } from '../types/reimbursementSchema'
import { ReimbursementStatusBadge } from '../components/ReimbursementStatusBadge'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

type Decision = 'reject' | 'request-changes'

function RemarksForm({ submitLabel, onSubmit }: { submitLabel: string; onSubmit: (remarks: string) => Promise<void> }) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ReviewRemarksFormValues>({ resolver: zodResolver(reviewRemarksFormSchema), defaultValues: { remarks: '' } })

  async function submit(values: ReviewRemarksFormValues) {
    setFormError(null)
    try {
      await onSubmit(values.remarks.trim())
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to record the decision.')
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="space-y-3">
      <div>
        <label htmlFor="queue-remarks" className="mb-1 block text-sm font-medium text-ink-muted">
          Remarks
        </label>
        <textarea
          id="queue-remarks"
          rows={3}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('remarks')}
        />
        {errors.remarks && <p className="mt-1 text-xs text-danger">{errors.remarks.message}</p>}
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

export function ReimbursementReviewQueuePage() {
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'

  const submitted = useReimbursements({ status: 'Submitted', pageSize: 100 })
  const underReview = useReimbursements({ status: 'UnderReview', pageSize: 100 })

  const [pendingId, setPendingId] = useState<string | null>(null)
  const [decisionTarget, setDecisionTarget] = useState<{ reimbursement: Reimbursement; decision: Decision } | null>(null)
  const [rowError, setRowError] = useState<string | null>(null)

  if (!isAdmin) {
    return (
      <div className="rounded-lg border border-hairline bg-surface-1 p-6">
        <p className="text-sm text-danger">You don't have permission to view the review queue.</p>
      </div>
    )
  }

  function refreshAll() {
    submitted.refresh()
    underReview.refresh()
  }

  async function handleStartReview(reimbursement: Reimbursement) {
    setPendingId(reimbursement.id)
    setRowError(null)
    try {
      await reimbursementRepository.startReview(reimbursement.id)
      refreshAll()
    } catch (err) {
      setRowError(err instanceof AppError ? err.message : 'Failed to start review.')
    } finally {
      setPendingId(null)
    }
  }

  async function handleApprove(reimbursement: Reimbursement) {
    setPendingId(reimbursement.id)
    setRowError(null)
    try {
      await reimbursementRepository.approve(reimbursement.id)
      refreshAll()
    } catch (err) {
      setRowError(err instanceof AppError ? err.message : 'Failed to approve the claim.')
    } finally {
      setPendingId(null)
    }
  }

  async function handleDecision(remarks: string) {
    if (!decisionTarget) return
    if (decisionTarget.decision === 'reject') {
      await reimbursementRepository.reject(decisionTarget.reimbursement.id, remarks)
    } else {
      await reimbursementRepository.requestChanges(decisionTarget.reimbursement.id, remarks)
    }
    setDecisionTarget(null)
    refreshAll()
  }

  const isLoading = submitted.isLoading || underReview.isLoading
  const totalPending = (submitted.result?.totalCount ?? 0) + (underReview.result?.totalCount ?? 0)

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Reimbursement Review</h1>
        <p className="text-sm text-ink-subtle">{isLoading ? ' ' : `${totalPending} awaiting action`}</p>
      </div>

      {(submitted.error || underReview.error || rowError) && (
        <p className="text-sm text-danger">{submitted.error ?? underReview.error ?? rowError}</p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Claim</th>
              <th className="px-4 py-3">Employee</th>
              <th className="px-4 py-3">Category</th>
              <th className="px-4 py-3">Amount</th>
              <th className="px-4 py-3">Status</th>
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

            {!isLoading && totalPending === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={6}>
                  Nothing awaiting review.
                </td>
              </tr>
            )}

            {!isLoading &&
              [...(submitted.result?.data ?? []), ...(underReview.result?.data ?? [])].map((reimbursement) => (
                <tr key={reimbursement.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link
                      to={`/reimbursements/${reimbursement.id}`}
                      className="font-medium text-ink hover:text-primary-hover"
                    >
                      {reimbursement.expenseTitle}
                    </Link>
                    <p className="font-mono text-xs text-ink-subtle">{reimbursement.reimbursementNumber}</p>
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{reimbursement.employeeName ?? '—'}</td>
                  <td className="px-4 py-3 text-ink-muted">{reimbursement.expenseCategory}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {reimbursement.amount.toLocaleString(undefined, { style: 'currency', currency: reimbursement.currency })}
                  </td>
                  <td className="px-4 py-3">
                    <ReimbursementStatusBadge status={reimbursement.status} />
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex justify-end gap-3">
                      {reimbursement.status === 'Submitted' && (
                        <button
                          type="button"
                          disabled={pendingId === reimbursement.id}
                          onClick={() => void handleStartReview(reimbursement)}
                          className="text-xs font-medium text-primary-hover hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                        >
                          Start review
                        </button>
                      )}
                      {reimbursement.status === 'UnderReview' && (
                        <>
                          <button
                            type="button"
                            disabled={pendingId === reimbursement.id}
                            onClick={() => void handleApprove(reimbursement)}
                            className="text-xs font-medium text-success hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                          >
                            Approve
                          </button>
                          <button
                            type="button"
                            onClick={() => setDecisionTarget({ reimbursement, decision: 'request-changes' })}
                            className="text-xs font-medium text-ink hover:underline"
                          >
                            Request changes
                          </button>
                          <button
                            type="button"
                            onClick={() => setDecisionTarget({ reimbursement, decision: 'reject' })}
                            className="text-xs font-medium text-danger hover:underline"
                          >
                            Reject
                          </button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      <Modal
        isOpen={decisionTarget !== null}
        onClose={() => setDecisionTarget(null)}
        title={decisionTarget?.decision === 'reject' ? 'Reject claim' : 'Request changes'}
      >
        <RemarksForm
          submitLabel={decisionTarget?.decision === 'reject' ? 'Confirm reject' : 'Send back for changes'}
          onSubmit={handleDecision}
        />
      </Modal>
    </div>
  )
}
