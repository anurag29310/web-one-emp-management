import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { usePromotion } from '../hooks/usePromotion'
import { performanceRepository } from '../api'
import { promotionDecisionFormSchema, type PromotionDecisionFormValues } from '../types/performanceSchema'
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

export function PromotionDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { promotion, isLoading, error, refresh } = usePromotion(id)
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR' || user?.role === 'Manager'
  const canDecide = user?.role === 'Admin' || user?.role === 'HR'

  const [decisionAction, setDecisionAction] = useState<'approve' | 'reject' | null>(null)
  const [isWithdrawing, setIsWithdrawing] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  const decisionForm = useForm<PromotionDecisionFormValues>({
    resolver: zodResolver(promotionDecisionFormSchema),
    defaultValues: { decisionNotes: '' },
  })

  async function onDecide(values: PromotionDecisionFormValues) {
    if (!promotion || !decisionAction) return
    setFormError(null)
    try {
      const input = { decisionNotes: values.decisionNotes?.trim() || undefined }
      if (decisionAction === 'approve') {
        await performanceRepository.approvePromotion(promotion.id, input)
      } else {
        await performanceRepository.rejectPromotion(promotion.id, input)
      }
      decisionForm.reset()
      setDecisionAction(null)
      refresh()
    } catch (err) {
      setFormError(
        err instanceof AppError ? err.message : `Failed to ${decisionAction === 'approve' ? 'approve' : 'reject'} promotion.`,
      )
    }
  }

  async function handleWithdraw() {
    if (!promotion) return
    setIsWithdrawing(true)
    setFormError(null)
    try {
      await performanceRepository.withdrawPromotion(promotion.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to withdraw promotion.')
    } finally {
      setIsWithdrawing(false)
    }
  }

  async function handleDelete() {
    if (!promotion) return
    setIsDeleting(true)
    setFormError(null)
    try {
      await performanceRepository.removePromotion(promotion.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete promotion.')
    } finally {
      setIsDeleting(false)
    }
  }

  async function handleRestore() {
    if (!promotion) return
    setFormError(null)
    try {
      await performanceRepository.restorePromotion(promotion.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to restore promotion.')
    }
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!promotion) return null

  const isProposed = promotion.status === 'Proposed' && !promotion.isDeleted

  return (
    <div className="space-y-4">
      <Link
        to="/performance/promotions"
        className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover"
      >
        ← Back to promotions
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
              {promotion.employeeName ?? promotion.employeeId}
            </h1>
            <p className="font-mono text-sm text-ink-subtle">{promotion.promotionNumber}</p>
          </div>
          <div className="flex items-center gap-2">
            <StatusBadge status={promotion.status} />
            {promotion.isDeleted && canDecide && (
              <button
                type="button"
                onClick={() => void handleRestore()}
                className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
              >
                Restore
              </button>
            )}
            {isProposed && canDecide && (
              <>
                <button
                  type="button"
                  onClick={() => setDecisionAction((current) => (current === 'approve' ? null : 'approve'))}
                  className="rounded-md bg-success px-3 py-2 text-sm font-medium text-white transition hover:opacity-90"
                >
                  {decisionAction === 'approve' ? 'Cancel' : 'Approve'}
                </button>
                <button
                  type="button"
                  onClick={() => setDecisionAction((current) => (current === 'reject' ? null : 'reject'))}
                  className="rounded-md bg-danger px-3 py-2 text-sm font-medium text-white transition hover:bg-danger/90"
                >
                  {decisionAction === 'reject' ? 'Cancel' : 'Reject'}
                </button>
              </>
            )}
            {isProposed && canManage && (
              <button
                type="button"
                disabled={isWithdrawing}
                onClick={() => void handleWithdraw()}
                className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isWithdrawing ? 'Withdrawing…' : 'Withdraw'}
              </button>
            )}
            {canDecide && !promotion.isDeleted && (
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
          <Field label="From designation" value={promotion.fromDesignationName ?? '—'} />
          <Field label="To designation" value={promotion.toDesignationName ?? '—'} />
          <Field label="From department" value={promotion.fromDepartmentName ?? '—'} />
          <Field label="To department" value={promotion.toDepartmentName ?? '—'} />
          <Field label="Effective date" value={new Date(promotion.effectiveDate).toLocaleDateString()} />
          <Field
            label="Applied"
            value={
              promotion.status === 'Approved'
                ? promotion.appliedAtUtc
                  ? new Date(promotion.appliedAtUtc).toLocaleString()
                  : 'Approved — pending effective date'
                : '—'
            }
          />
          <Field label="Reason" value={promotion.reason} />
          <Field label="Decision notes" value={promotion.decisionNotes ?? '—'} />
        </div>

        {formError && !decisionAction && (
          <p role="alert" className="px-6 pb-6 text-sm text-danger">
            {formError}
          </p>
        )}
      </div>

      <Modal
        isOpen={decisionAction !== null}
        onClose={() => setDecisionAction(null)}
        title={decisionAction === 'approve' ? 'Approve promotion' : 'Reject promotion'}
      >
        <form onSubmit={decisionForm.handleSubmit(onDecide)} noValidate className="space-y-3">
          <div>
            <label htmlFor="decision-notes" className="mb-1 block text-sm font-medium text-ink-muted">
              Decision notes
            </label>
            <textarea
              id="decision-notes"
              rows={3}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...decisionForm.register('decisionNotes')}
            />
          </div>
          {formError && (
            <p role="alert" className="text-sm text-danger">
              {formError}
            </p>
          )}
          <button
            type="submit"
            disabled={decisionForm.formState.isSubmitting}
            className={`rounded-md px-3 py-2 text-sm font-medium text-white transition disabled:cursor-not-allowed disabled:opacity-60 ${
              decisionAction === 'approve' ? 'bg-success hover:opacity-90' : 'bg-danger hover:bg-danger/90'
            }`}
          >
            {decisionForm.formState.isSubmitting
              ? 'Saving…'
              : decisionAction === 'approve'
                ? 'Approve promotion'
                : 'Reject promotion'}
          </button>
        </form>
      </Modal>
    </div>
  )
}
