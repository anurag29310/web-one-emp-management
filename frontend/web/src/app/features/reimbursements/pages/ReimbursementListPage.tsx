import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useReimbursements } from '../hooks/useReimbursements'
import { reimbursementRepository, type ReimbursementStatus } from '../api'
import { reimbursementFormSchema, type ReimbursementFormValues } from '../types/reimbursementSchema'
import { ReimbursementFormFields } from '../components/ReimbursementFormFields'
import { ReimbursementStatusBadge } from '../components/ReimbursementStatusBadge'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

const STATUS_OPTIONS: ReimbursementStatus[] = [
  'Draft',
  'Submitted',
  'UnderReview',
  'Approved',
  'Rejected',
  'ChangesRequested',
  'Paid',
]

function NewClaimForm({ onCreated }: { onCreated: () => void }) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    watch,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ReimbursementFormValues>({
    resolver: zodResolver(reimbursementFormSchema),
    defaultValues: {
      expenseTitle: '',
      expenseCategory: '',
      expenseDate: '',
      currency: 'USD',
      claimType: 'Amount',
      amount: '',
      distanceKm: '',
      description: '',
      notes: '',
    },
  })

  async function onSubmit(values: ReimbursementFormValues) {
    setFormError(null)
    try {
      await reimbursementRepository.create({
        expenseTitle: values.expenseTitle.trim(),
        expenseCategory: values.expenseCategory.trim(),
        expenseDate: values.expenseDate,
        amount: values.claimType === 'Amount' ? Number(values.amount) : 0,
        currency: values.currency.trim(),
        description: values.description?.trim() || undefined,
        notes: values.notes?.trim() || undefined,
        distanceKm: values.claimType === 'Mileage' ? Number(values.distanceKm) : undefined,
      })
      reset()
      onCreated()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to save the claim.')
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-3">
      <ReimbursementFormFields register={register} watch={watch} errors={errors} idPrefix="new-claim" />
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
        {isSubmitting ? 'Saving…' : 'Save as draft'}
      </button>
    </form>
  )
}

export function ReimbursementListPage() {
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'
  const [status, setStatus] = useState<ReimbursementStatus | ''>('')
  const { result, isLoading, error, refresh } = useReimbursements({ pageSize: 50, status: status || undefined })
  const [isFormOpen, setIsFormOpen] = useState(false)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">
            {isAdmin ? 'All Reimbursements' : 'My Reimbursements'}
          </h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        <div className="flex items-center gap-2">
          {isAdmin && (
            <Link
              to="/reimbursements/review"
              className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
            >
              Review queue
            </Link>
          )}
          {!isAdmin && (
            <button
              type="button"
              onClick={() => setIsFormOpen((open) => !open)}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
            >
              {isFormOpen ? 'Cancel' : 'New claim'}
            </button>
          )}
        </div>
      </div>

      {!isAdmin && (
        <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="New reimbursement claim">
          <NewClaimForm
            onCreated={() => {
              setIsFormOpen(false)
              refresh()
            }}
          />
        </Modal>
      )}

      <select
        aria-label="Filter by status"
        value={status}
        onChange={(e) => setStatus(e.target.value as ReimbursementStatus | '')}
        className="w-56 rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
      >
        <option value="">All statuses</option>
        {STATUS_OPTIONS.map((s) => (
          <option key={s} value={s}>
            {s}
          </option>
        ))}
      </select>

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Claim</th>
              {isAdmin && <th className="px-4 py-3">Employee</th>}
              <th className="px-4 py-3">Category</th>
              <th className="px-4 py-3">Date</th>
              <th className="px-4 py-3">Amount</th>
              <th className="px-4 py-3">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={isAdmin ? 6 : 5}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && result?.data.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={isAdmin ? 6 : 5}>
                  No reimbursements found.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((reimbursement) => (
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
                  {isAdmin && <td className="px-4 py-3 text-ink-muted">{reimbursement.employeeName ?? '—'}</td>}
                  <td className="px-4 py-3 text-ink-muted">{reimbursement.expenseCategory}</td>
                  <td className="px-4 py-3 text-ink-muted">{new Date(reimbursement.expenseDate).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {reimbursement.amount.toLocaleString(undefined, { style: 'currency', currency: reimbursement.currency })}
                  </td>
                  <td className="px-4 py-3">
                    <ReimbursementStatusBadge status={reimbursement.status} />
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
