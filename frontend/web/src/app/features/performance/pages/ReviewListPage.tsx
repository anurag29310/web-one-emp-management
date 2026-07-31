import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useReviews } from '../hooks/useReviews'
import { performanceRepository, type ReviewStatus } from '../api'
import { createReviewFormSchema, type CreateReviewFormValues } from '../types/performanceSchema'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'
import { StatusBadge } from '@/app/shared/components/StatusBadge'

const STATUS_OPTIONS: ReviewStatus[] = ['Draft', 'SelfAssessmentSubmitted', 'Completed', 'Cancelled']

function CreateReviewForm({ onCreated }: { onCreated: () => void }) {
  const [formError, setFormError] = useState<string | null>(null)
  const { result: employeeResult } = useEmployees({ pageSize: 100 })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateReviewFormValues>({
    resolver: zodResolver(createReviewFormSchema),
    defaultValues: { employeeId: '', reviewerEmployeeId: '', reviewPeriodStart: '', reviewPeriodEnd: '', notes: '' },
  })

  async function onSubmit(values: CreateReviewFormValues) {
    setFormError(null)
    try {
      await performanceRepository.createReview({
        employeeId: values.employeeId,
        reviewerEmployeeId: values.reviewerEmployeeId,
        reviewPeriodStart: values.reviewPeriodStart,
        reviewPeriodEnd: values.reviewPeriodEnd,
        notes: values.notes?.trim() || undefined,
      })
      reset()
      onCreated()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to start review.')
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-3">
      <div>
        <label htmlFor="review-employee" className="mb-1 block text-sm font-medium text-ink-muted">
          Employee
        </label>
        <select
          id="review-employee"
          aria-invalid={Boolean(errors.employeeId)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('employeeId')}
        >
          <option value="">Select employee…</option>
          {employeeResult?.data.map((employee) => (
            <option key={employee.id} value={employee.id}>
              {employee.fullName}
            </option>
          ))}
        </select>
        {errors.employeeId && <p className="mt-1 text-xs text-danger">{errors.employeeId.message}</p>}
      </div>

      <div>
        <label htmlFor="review-reviewer" className="mb-1 block text-sm font-medium text-ink-muted">
          Reviewer
        </label>
        <select
          id="review-reviewer"
          aria-invalid={Boolean(errors.reviewerEmployeeId)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('reviewerEmployeeId')}
        >
          <option value="">Select reviewer…</option>
          {employeeResult?.data.map((employee) => (
            <option key={employee.id} value={employee.id}>
              {employee.fullName}
            </option>
          ))}
        </select>
        {errors.reviewerEmployeeId && <p className="mt-1 text-xs text-danger">{errors.reviewerEmployeeId.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="review-period-start" className="mb-1 block text-sm font-medium text-ink-muted">
            Review period start
          </label>
          <input
            id="review-period-start"
            type="date"
            aria-invalid={Boolean(errors.reviewPeriodStart)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('reviewPeriodStart')}
          />
          {errors.reviewPeriodStart && <p className="mt-1 text-xs text-danger">{errors.reviewPeriodStart.message}</p>}
        </div>
        <div>
          <label htmlFor="review-period-end" className="mb-1 block text-sm font-medium text-ink-muted">
            Review period end
          </label>
          <input
            id="review-period-end"
            type="date"
            aria-invalid={Boolean(errors.reviewPeriodEnd)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('reviewPeriodEnd')}
          />
          {errors.reviewPeriodEnd && <p className="mt-1 text-xs text-danger">{errors.reviewPeriodEnd.message}</p>}
        </div>
      </div>

      <div>
        <label htmlFor="review-notes" className="mb-1 block text-sm font-medium text-ink-muted">
          Notes
        </label>
        <textarea
          id="review-notes"
          rows={2}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('notes')}
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
        {isSubmitting ? 'Starting…' : 'Start review'}
      </button>
    </form>
  )
}

export function ReviewListPage() {
  const [status, setStatus] = useState<ReviewStatus | ''>('')
  const { result, isLoading, error, refresh } = useReviews({
    pageSize: 50,
    status: status || undefined,
  })
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR' || user?.role === 'Manager'
  const [isFormOpen, setIsFormOpen] = useState(false)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Performance Reviews</h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsFormOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isFormOpen ? 'Cancel' : 'Start review'}
          </button>
        )}
      </div>

      {canManage && (
        <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="Start review">
          <CreateReviewForm
            onCreated={() => {
              setIsFormOpen(false)
              refresh()
            }}
          />
        </Modal>
      )}

      <div className="flex items-center gap-3">
        <select
          aria-label="Filter by status"
          value={status}
          onChange={(e) => setStatus(e.target.value as ReviewStatus | '')}
          className="rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        >
          <option value="">All statuses</option>
          {STATUS_OPTIONS.map((s) => (
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
              <th className="px-4 py-3">Review #</th>
              <th className="px-4 py-3">Employee</th>
              <th className="px-4 py-3">Reviewer</th>
              <th className="px-4 py-3">Period</th>
              <th className="px-4 py-3">Rating</th>
              <th className="px-4 py-3">Status</th>
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
                  No reviews found.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((review) => (
                <tr key={review.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link
                      to={`/performance/reviews/${review.id}`}
                      className="font-mono font-medium text-ink hover:text-primary-hover"
                    >
                      {review.reviewNumber}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{review.employeeName ?? review.employeeId}</td>
                  <td className="px-4 py-3 text-ink-muted">{review.reviewerName ?? review.reviewerEmployeeId}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {new Date(review.reviewPeriodStart).toLocaleDateString()} –{' '}
                    {new Date(review.reviewPeriodEnd).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{review.overallRating ?? '—'}</td>
                  <td className="px-4 py-3">
                    <StatusBadge status={review.status} />
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
