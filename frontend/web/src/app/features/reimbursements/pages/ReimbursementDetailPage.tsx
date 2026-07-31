import { useEffect, useState, type ChangeEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useReimbursement } from '../hooks/useReimbursement'
import { useReimbursementAttachments } from '../hooks/useReimbursementAttachments'
import { reimbursementRepository, type ReimbursementAttachment } from '../api'
import {
  reimbursementFormSchema,
  reviewRemarksFormSchema,
  type ReimbursementFormValues,
  type ReviewRemarksFormValues,
} from '../types/reimbursementSchema'
import { ReimbursementFormFields } from '../components/ReimbursementFormFields'
import { ReimbursementStatusBadge } from '../components/ReimbursementStatusBadge'
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

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

type ReviewAction = 'reject' | 'request-changes'

export function ReimbursementDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { reimbursement, isLoading, error, refresh } = useReimbursement(id)
  const { attachments, isLoading: attachmentsLoading, error: attachmentsError, refresh: refreshAttachments } =
    useReimbursementAttachments(id)
  const { user } = useAuth()
  const navigate = useNavigate()

  // GET /reimbursements/{id} 404s for a non-Admin caller who isn't the owner (existence isn't
  // disclosed) — so a non-Admin who successfully loaded this record IS the owner. There's no
  // endpoint exposing the caller's own employeeId to compare directly (User.Id and Employee.Id
  // are different entities, linked by a User.EmployeeId FK that GET /auth/me doesn't surface).
  const isAdmin = user?.role === 'Admin'
  const isOwner = !isAdmin

  const [isEditing, setIsEditing] = useState(false)
  const [reviewAction, setReviewAction] = useState<ReviewAction | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [isActing, setIsActing] = useState(false)
  const [isUploading, setIsUploading] = useState(false)

  const editForm = useForm<ReimbursementFormValues>({ resolver: zodResolver(reimbursementFormSchema) })
  const resetEditForm = editForm.reset
  const reviewForm = useForm<ReviewRemarksFormValues>({
    resolver: zodResolver(reviewRemarksFormSchema),
    defaultValues: { remarks: '' },
  })

  useEffect(() => {
    if (reimbursement) {
      resetEditForm({
        expenseTitle: reimbursement.expenseTitle,
        expenseCategory: reimbursement.expenseCategory,
        expenseDate: reimbursement.expenseDate.slice(0, 10),
        currency: reimbursement.currency,
        claimType: reimbursement.distanceKm ? 'Mileage' : 'Amount',
        amount: reimbursement.distanceKm ? '' : String(reimbursement.amount),
        distanceKm: reimbursement.distanceKm ? String(reimbursement.distanceKm) : '',
        description: reimbursement.description ?? '',
        notes: reimbursement.notes ?? '',
      })
    }
  }, [reimbursement, resetEditForm])

  async function onSaveEdit(values: ReimbursementFormValues) {
    if (!reimbursement) return
    setFormError(null)
    try {
      await reimbursementRepository.update(reimbursement.id, {
        expenseTitle: values.expenseTitle.trim(),
        expenseCategory: values.expenseCategory.trim(),
        expenseDate: values.expenseDate,
        amount: values.claimType === 'Amount' ? Number(values.amount) : 0,
        currency: values.currency.trim(),
        description: values.description?.trim() || undefined,
        notes: values.notes?.trim() || undefined,
        distanceKm: values.claimType === 'Mileage' ? Number(values.distanceKm) : undefined,
      })
      setIsEditing(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update the claim.')
    }
  }

  async function runAction(action: () => Promise<void>, failureMessage: string) {
    setIsActing(true)
    setFormError(null)
    try {
      await action()
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : failureMessage)
    } finally {
      setIsActing(false)
    }
  }

  async function handleDelete() {
    if (!reimbursement) return
    setIsActing(true)
    setFormError(null)
    try {
      await reimbursementRepository.remove(reimbursement.id)
      navigate('/reimbursements')
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete the claim.')
      setIsActing(false)
    }
  }

  async function onSubmitReview(values: ReviewRemarksFormValues) {
    if (!reimbursement || !reviewAction) return
    setFormError(null)
    try {
      if (reviewAction === 'reject') {
        await reimbursementRepository.reject(reimbursement.id, values.remarks.trim())
      } else {
        await reimbursementRepository.requestChanges(reimbursement.id, values.remarks.trim())
      }
      setReviewAction(null)
      reviewForm.reset()
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to record the decision.')
    }
  }

  async function handleUpload(event: ChangeEvent<HTMLInputElement>) {
    if (!reimbursement) return
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    setFormError(null)
    setIsUploading(true)
    try {
      await reimbursementRepository.uploadAttachment(reimbursement.id, file)
      refreshAttachments()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to upload attachment.')
    } finally {
      setIsUploading(false)
    }
  }

  async function handleDownload(attachment: ReimbursementAttachment) {
    setFormError(null)
    try {
      const { blob, fileName } = await reimbursementRepository.downloadAttachment(attachment.id)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to download attachment.')
    }
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!reimbursement) return null

  const canEdit = isOwner && (reimbursement.status === 'Draft' || reimbursement.status === 'ChangesRequested')
  const canDelete = isOwner && reimbursement.status === 'Draft'
  const canSubmit = isOwner && (reimbursement.status === 'Draft' || reimbursement.status === 'ChangesRequested')
  const canUpload =
    isOwner &&
    (['Draft', 'Submitted', 'UnderReview', 'ChangesRequested'] as const).includes(
      reimbursement.status as 'Draft' | 'Submitted' | 'UnderReview' | 'ChangesRequested',
    )
  const canStartReview = isAdmin && reimbursement.status === 'Submitted'
  const canDecide = isAdmin && reimbursement.status === 'UnderReview'

  return (
    <div className="space-y-4">
      <Link
        to="/reimbursements"
        className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover"
      >
        ← Back to reimbursements
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
              {reimbursement.expenseTitle}
            </h1>
            <p className="font-mono text-sm text-ink-subtle">{reimbursement.reimbursementNumber}</p>
          </div>
          <div className="flex flex-wrap items-center justify-end gap-2">
            <ReimbursementStatusBadge status={reimbursement.status} />

            {canSubmit && (
              <button
                type="button"
                disabled={isActing}
                onClick={() => void runAction(() => reimbursementRepository.submit(reimbursement.id), 'Failed to submit the claim.')}
                className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
              >
                Submit
              </button>
            )}
            {canEdit && (
              <button
                type="button"
                onClick={() => setIsEditing((open) => !open)}
                className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
              >
                {isEditing ? 'Cancel' : 'Edit'}
              </button>
            )}
            {canDelete && (
              <button
                type="button"
                disabled={isActing}
                onClick={() => void handleDelete()}
                className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
              >
                Delete
              </button>
            )}

            {canStartReview && (
              <button
                type="button"
                disabled={isActing}
                onClick={() =>
                  void runAction(() => reimbursementRepository.startReview(reimbursement.id), 'Failed to start review.')
                }
                className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
              >
                Start review
              </button>
            )}
            {canDecide && (
              <>
                <button
                  type="button"
                  disabled={isActing}
                  onClick={() =>
                    void runAction(() => reimbursementRepository.approve(reimbursement.id), 'Failed to approve the claim.')
                  }
                  className="rounded-md bg-success px-3 py-2 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  Approve
                </button>
                <button
                  type="button"
                  onClick={() => setReviewAction((a) => (a === 'request-changes' ? null : 'request-changes'))}
                  className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                >
                  Request changes
                </button>
                <button
                  type="button"
                  onClick={() => setReviewAction((a) => (a === 'reject' ? null : 'reject'))}
                  className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10"
                >
                  Reject
                </button>
              </>
            )}
          </div>
        </div>

        {reviewAction && (
          <form onSubmit={reviewForm.handleSubmit(onSubmitReview)} noValidate className="space-y-2 border-b border-hairline p-6">
            <label htmlFor="review-remarks" className="mb-1 block text-sm font-medium text-ink-muted">
              {reviewAction === 'reject' ? 'Reason for rejection' : 'What needs to change'}
            </label>
            <textarea
              id="review-remarks"
              rows={2}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...reviewForm.register('remarks')}
            />
            {reviewForm.formState.errors.remarks && (
              <p className="text-xs text-danger">{reviewForm.formState.errors.remarks.message}</p>
            )}
            <button
              type="submit"
              disabled={reviewForm.formState.isSubmitting}
              className={`rounded-md px-3 py-2 text-sm font-medium text-white transition disabled:cursor-not-allowed disabled:opacity-60 ${
                reviewAction === 'reject' ? 'bg-danger hover:opacity-90' : 'bg-primary hover:bg-primary-hover'
              }`}
            >
              {reviewAction === 'reject' ? 'Confirm reject' : 'Send back for changes'}
            </button>
          </form>
        )}

        <div className="grid grid-cols-2 gap-6 p-6">
          {isAdmin && <Field label="Employee" value={reimbursement.employeeName ?? '—'} />}
          <Field label="Category" value={reimbursement.expenseCategory} />
          <Field label="Expense date" value={new Date(reimbursement.expenseDate).toLocaleDateString()} />
          <Field
            label="Amount"
            value={reimbursement.amount.toLocaleString(undefined, { style: 'currency', currency: reimbursement.currency })}
          />
          {reimbursement.distanceKm != null && (
            <Field
              label="Mileage"
              value={`${reimbursement.distanceKm} km × ${reimbursement.mileageRatePerKm ?? '—'} ${reimbursement.currency}/km`}
            />
          )}
          <Field label="Description" value={reimbursement.description ?? '—'} />
          <Field label="Notes" value={reimbursement.notes ?? '—'} />
          <Field
            label="Submitted"
            value={reimbursement.submittedAtUtc ? new Date(reimbursement.submittedAtUtc).toLocaleString() : '—'}
          />
          {reimbursement.reviewRemarks && <Field label="Review remarks" value={reimbursement.reviewRemarks} />}
          {reimbursement.status === 'Paid' && (
            <Field label="Paid" value={reimbursement.payrollDate ? new Date(reimbursement.payrollDate).toLocaleDateString() : '—'} />
          )}
        </div>

        {formError && (
          <p role="alert" className="px-6 pb-6 text-sm text-danger">
            {formError}
          </p>
        )}
      </div>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <h2 className="text-[18px] font-medium text-ink">Supporting documents</h2>
          {canUpload && (
            <label className="cursor-pointer rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3">
              {isUploading ? 'Uploading…' : 'Upload'}
              <input
                type="file"
                className="hidden"
                accept="application/pdf,image/jpeg,image/png"
                disabled={isUploading}
                onChange={(e) => void handleUpload(e)}
              />
            </label>
          )}
        </div>
        <div className="p-6">
          {attachmentsError && <p className="mb-2 text-sm text-danger">{attachmentsError}</p>}
          {!attachmentsLoading && attachments.length === 0 && (
            <p className="text-sm text-ink-subtle">No supporting documents uploaded yet.</p>
          )}
          <ul className="divide-y divide-hairline">
            {attachments.map((attachment) => (
              <li key={attachment.id} className="flex items-center justify-between py-2">
                <div>
                  <p className="text-sm font-medium text-ink">{attachment.originalFileName}</p>
                  <p className="text-xs text-ink-subtle">
                    {formatFileSize(attachment.fileSizeBytes)} · {new Date(attachment.uploadedAtUtc).toLocaleDateString()}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => void handleDownload(attachment)}
                  className="text-xs font-medium text-primary-hover hover:underline"
                >
                  Download
                </button>
              </li>
            ))}
          </ul>
        </div>
      </div>

      {canEdit && (
        <Modal isOpen={isEditing} onClose={() => setIsEditing(false)} title="Edit claim">
          <form onSubmit={editForm.handleSubmit(onSaveEdit)} noValidate className="space-y-3">
            <ReimbursementFormFields
              register={editForm.register}
              watch={editForm.watch}
              errors={editForm.formState.errors}
              idPrefix="edit-claim"
            />
            {formError && (
              <p role="alert" className="text-sm text-danger">
                {formError}
              </p>
            )}
            <button
              type="submit"
              disabled={editForm.formState.isSubmitting}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {editForm.formState.isSubmitting ? 'Saving…' : 'Save changes'}
            </button>
          </form>
        </Modal>
      )}
    </div>
  )
}
