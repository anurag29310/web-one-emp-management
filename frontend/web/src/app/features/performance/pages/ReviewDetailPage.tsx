import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useReview } from '../hooks/useReview'
import { performanceRepository } from '../api'
import {
  cancelReviewFormSchema,
  managerReviewFormSchema,
  selfAssessmentFormSchema,
  type CancelReviewFormValues,
  type ManagerReviewFormInput,
  type ManagerReviewFormValues,
  type SelfAssessmentFormValues,
} from '../types/performanceSchema'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'
import { StatusBadge } from '@/app/shared/components/StatusBadge'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 whitespace-pre-wrap text-sm text-ink">{value}</p>
    </div>
  )
}

export function ReviewDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { review, isLoading, error, refresh } = useReview(id)
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR' || user?.role === 'Manager'

  const [isSubmittingSelf, setIsSubmittingSelf] = useState(false)
  const [isSubmittingManager, setIsSubmittingManager] = useState(false)
  const [isCancelling, setIsCancelling] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  const selfForm = useForm<SelfAssessmentFormValues>({
    resolver: zodResolver(selfAssessmentFormSchema),
    defaultValues: { selfAssessment: '' },
  })
  const managerForm = useForm<ManagerReviewFormInput, unknown, ManagerReviewFormValues>({
    resolver: zodResolver(managerReviewFormSchema),
    defaultValues: { managerAssessment: '', overallRating: 3 },
  })
  const cancelForm = useForm<CancelReviewFormValues>({
    resolver: zodResolver(cancelReviewFormSchema),
    defaultValues: { reason: '' },
  })

  async function onSubmitSelfAssessment(values: SelfAssessmentFormValues) {
    if (!review) return
    setFormError(null)
    try {
      await performanceRepository.submitSelfAssessment(review.id, { selfAssessment: values.selfAssessment.trim() })
      selfForm.reset()
      setIsSubmittingSelf(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to submit self-assessment.')
    }
  }

  async function onSubmitManagerReview(values: ManagerReviewFormValues) {
    if (!review) return
    setFormError(null)
    try {
      await performanceRepository.submitManagerReview(review.id, {
        managerAssessment: values.managerAssessment.trim(),
        overallRating: values.overallRating,
      })
      managerForm.reset()
      setIsSubmittingManager(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to submit manager review.')
    }
  }

  async function onCancel(values: CancelReviewFormValues) {
    if (!review) return
    setFormError(null)
    try {
      await performanceRepository.cancelReview(review.id, { reason: values.reason?.trim() || undefined })
      cancelForm.reset()
      setIsCancelling(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to cancel review.')
    }
  }

  async function handleDelete() {
    if (!review) return
    setIsDeleting(true)
    setFormError(null)
    try {
      await performanceRepository.removeReview(review.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete review.')
    } finally {
      setIsDeleting(false)
    }
  }

  async function handleRestore() {
    if (!review) return
    setFormError(null)
    try {
      await performanceRepository.restoreReview(review.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to restore review.')
    }
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!review) return null

  const canSubmitSelfAssessment = review.status === 'Draft' && !review.isDeleted
  const canSubmitManagerReview =
    (review.status === 'Draft' || review.status === 'SelfAssessmentSubmitted') && !review.isDeleted
  const canCancel = canManage && (review.status === 'Draft' || review.status === 'SelfAssessmentSubmitted') && !review.isDeleted

  return (
    <div className="space-y-4">
      <Link
        to="/performance/reviews"
        className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover"
      >
        ← Back to reviews
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
              {review.employeeName ?? review.employeeId}
            </h1>
            <p className="font-mono text-sm text-ink-subtle">{review.reviewNumber}</p>
          </div>
          <div className="flex items-center gap-2">
            <StatusBadge status={review.status} />
            {review.isDeleted && canManage && (
              <button
                type="button"
                onClick={() => void handleRestore()}
                className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
              >
                Restore
              </button>
            )}
            {canSubmitSelfAssessment && (
              <button
                type="button"
                onClick={() => setIsSubmittingSelf((open) => !open)}
                className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
              >
                {isSubmittingSelf ? 'Cancel' : 'Submit self-assessment'}
              </button>
            )}
            {canSubmitManagerReview && (
              <button
                type="button"
                onClick={() => setIsSubmittingManager((open) => !open)}
                className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
              >
                {isSubmittingManager ? 'Cancel' : 'Submit manager review'}
              </button>
            )}
            {canCancel && (
              <button
                type="button"
                onClick={() => setIsCancelling((open) => !open)}
                className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
              >
                {isCancelling ? 'Dismiss' : 'Cancel review'}
              </button>
            )}
            {canManage && !review.isDeleted && (
              <button
                type="button"
                disabled={isDeleting}
                onClick={() => void handleDelete()}
                className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isDeleting ? 'Deleting…' : 'Delete'}
              </button>
            )}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Reviewer" value={review.reviewerName ?? review.reviewerEmployeeId} />
          <Field label="Overall rating" value={review.overallRating != null ? String(review.overallRating) : '—'} />
          <Field label="Review period start" value={new Date(review.reviewPeriodStart).toLocaleDateString()} />
          <Field label="Review period end" value={new Date(review.reviewPeriodEnd).toLocaleDateString()} />
          <Field
            label="Self-assessment submitted"
            value={review.selfSubmittedAtUtc ? new Date(review.selfSubmittedAtUtc).toLocaleString() : '—'}
          />
          <Field label="Completed" value={review.completedAtUtc ? new Date(review.completedAtUtc).toLocaleString() : '—'} />
          <Field label="Notes" value={review.notes ?? '—'} />
        </div>

        <div className="grid grid-cols-2 gap-6 border-t border-hairline p-6">
          <Field label="Self-assessment" value={review.selfAssessment ?? '—'} />
          <Field label="Manager assessment" value={review.managerAssessment ?? '—'} />
        </div>

        {formError && !isSubmittingSelf && !isSubmittingManager && !isCancelling && (
          <p role="alert" className="px-6 pb-6 text-sm text-danger">
            {formError}
          </p>
        )}
      </div>

      <Modal isOpen={isSubmittingSelf} onClose={() => setIsSubmittingSelf(false)} title="Submit self-assessment">
        <form onSubmit={selfForm.handleSubmit(onSubmitSelfAssessment)} noValidate className="space-y-3">
          <div>
            <label htmlFor="self-assessment" className="mb-1 block text-sm font-medium text-ink-muted">
              Self-assessment
            </label>
            <textarea
              id="self-assessment"
              rows={6}
              aria-invalid={Boolean(selfForm.formState.errors.selfAssessment)}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...selfForm.register('selfAssessment')}
            />
            {selfForm.formState.errors.selfAssessment && (
              <p className="mt-1 text-xs text-danger">{selfForm.formState.errors.selfAssessment.message}</p>
            )}
          </div>
          {formError && (
            <p role="alert" className="text-sm text-danger">
              {formError}
            </p>
          )}
          <button
            type="submit"
            disabled={selfForm.formState.isSubmitting}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
          >
            {selfForm.formState.isSubmitting ? 'Submitting…' : 'Submit'}
          </button>
        </form>
      </Modal>

      <Modal isOpen={isSubmittingManager} onClose={() => setIsSubmittingManager(false)} title="Submit manager review">
        <form onSubmit={managerForm.handleSubmit(onSubmitManagerReview)} noValidate className="space-y-3">
          <div>
            <label htmlFor="manager-assessment" className="mb-1 block text-sm font-medium text-ink-muted">
              Manager assessment
            </label>
            <textarea
              id="manager-assessment"
              rows={6}
              aria-invalid={Boolean(managerForm.formState.errors.managerAssessment)}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...managerForm.register('managerAssessment')}
            />
            {managerForm.formState.errors.managerAssessment && (
              <p className="mt-1 text-xs text-danger">{managerForm.formState.errors.managerAssessment.message}</p>
            )}
          </div>
          <div>
            <label htmlFor="overall-rating" className="mb-1 block text-sm font-medium text-ink-muted">
              Overall rating (1–5)
            </label>
            <input
              id="overall-rating"
              type="number"
              min="1"
              max="5"
              aria-invalid={Boolean(managerForm.formState.errors.overallRating)}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...managerForm.register('overallRating')}
            />
            {managerForm.formState.errors.overallRating && (
              <p className="mt-1 text-xs text-danger">{managerForm.formState.errors.overallRating.message}</p>
            )}
          </div>
          {formError && (
            <p role="alert" className="text-sm text-danger">
              {formError}
            </p>
          )}
          <button
            type="submit"
            disabled={managerForm.formState.isSubmitting}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
          >
            {managerForm.formState.isSubmitting ? 'Submitting…' : 'Complete review'}
          </button>
        </form>
      </Modal>

      <Modal isOpen={isCancelling} onClose={() => setIsCancelling(false)} title="Cancel review">
        <form onSubmit={cancelForm.handleSubmit(onCancel)} noValidate className="space-y-3">
          <div>
            <label htmlFor="cancel-reason" className="mb-1 block text-sm font-medium text-ink-muted">
              Reason
            </label>
            <textarea
              id="cancel-reason"
              rows={2}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...cancelForm.register('reason')}
            />
          </div>
          {formError && (
            <p role="alert" className="text-sm text-danger">
              {formError}
            </p>
          )}
          <button
            type="submit"
            disabled={cancelForm.formState.isSubmitting}
            className="rounded-md bg-danger px-3 py-2 text-sm font-medium text-white transition hover:bg-danger/90 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {cancelForm.formState.isSubmitting ? 'Cancelling…' : 'Cancel review'}
          </button>
        </form>
      </Modal>
    </div>
  )
}
