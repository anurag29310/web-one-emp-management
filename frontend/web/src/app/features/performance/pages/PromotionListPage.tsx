import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { usePromotions } from '../hooks/usePromotions'
import { performanceRepository, type PromotionStatus } from '../api'
import { proposePromotionFormSchema, type ProposePromotionFormValues } from '../types/performanceSchema'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { useDesignations } from '@/app/features/designations/hooks/useDesignations'
import { useDepartments } from '@/app/features/departments/hooks/useDepartments'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'
import { StatusBadge } from '@/app/shared/components/StatusBadge'

const STATUS_OPTIONS: PromotionStatus[] = ['Proposed', 'Approved', 'Rejected', 'Withdrawn']

function ProposePromotionForm({ onCreated }: { onCreated: () => void }) {
  const [formError, setFormError] = useState<string | null>(null)
  const { result: employeeResult } = useEmployees({ pageSize: 100 })
  const { designations } = useDesignations()
  const { departments } = useDepartments()

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ProposePromotionFormValues>({
    resolver: zodResolver(proposePromotionFormSchema),
    defaultValues: { employeeId: '', toDesignationId: '', toDepartmentId: '', effectiveDate: '', reason: '' },
  })

  async function onSubmit(values: ProposePromotionFormValues) {
    setFormError(null)
    try {
      await performanceRepository.proposePromotion({
        employeeId: values.employeeId,
        toDesignationId: values.toDesignationId,
        toDepartmentId: values.toDepartmentId?.trim() || undefined,
        effectiveDate: values.effectiveDate,
        reason: values.reason.trim(),
      })
      reset()
      onCreated()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to propose promotion.')
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-3">
      <div>
        <label htmlFor="promo-employee" className="mb-1 block text-sm font-medium text-ink-muted">
          Employee
        </label>
        <select
          id="promo-employee"
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

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="promo-designation" className="mb-1 block text-sm font-medium text-ink-muted">
            New designation
          </label>
          <select
            id="promo-designation"
            aria-invalid={Boolean(errors.toDesignationId)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('toDesignationId')}
          >
            <option value="">Select designation…</option>
            {designations.map((designation) => (
              <option key={designation.id} value={designation.id}>
                {designation.name}
              </option>
            ))}
          </select>
          {errors.toDesignationId && <p className="mt-1 text-xs text-danger">{errors.toDesignationId.message}</p>}
        </div>
        <div>
          <label htmlFor="promo-department" className="mb-1 block text-sm font-medium text-ink-muted">
            New department (optional)
          </label>
          <select
            id="promo-department"
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('toDepartmentId')}
          >
            <option value="">No change</option>
            {departments.map((department) => (
              <option key={department.id} value={department.id}>
                {department.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div>
        <label htmlFor="promo-effective-date" className="mb-1 block text-sm font-medium text-ink-muted">
          Effective date
        </label>
        <input
          id="promo-effective-date"
          type="date"
          aria-invalid={Boolean(errors.effectiveDate)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('effectiveDate')}
        />
        {errors.effectiveDate && <p className="mt-1 text-xs text-danger">{errors.effectiveDate.message}</p>}
      </div>

      <div>
        <label htmlFor="promo-reason" className="mb-1 block text-sm font-medium text-ink-muted">
          Reason
        </label>
        <textarea
          id="promo-reason"
          rows={3}
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
        {isSubmitting ? 'Proposing…' : 'Propose promotion'}
      </button>
    </form>
  )
}

export function PromotionListPage() {
  const [status, setStatus] = useState<PromotionStatus | ''>('')
  const { result, isLoading, error, refresh } = usePromotions({
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
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Promotions</h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsFormOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isFormOpen ? 'Cancel' : 'Propose promotion'}
          </button>
        )}
      </div>

      {canManage && (
        <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="Propose promotion">
          <ProposePromotionForm
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
          onChange={(e) => setStatus(e.target.value as PromotionStatus | '')}
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
              <th className="px-4 py-3">Promotion #</th>
              <th className="px-4 py-3">Employee</th>
              <th className="px-4 py-3">From → To</th>
              <th className="px-4 py-3">Effective date</th>
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
                  No promotions found.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((promotion) => (
                <tr key={promotion.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link
                      to={`/performance/promotions/${promotion.id}`}
                      className="font-mono font-medium text-ink hover:text-primary-hover"
                    >
                      {promotion.promotionNumber}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{promotion.employeeName ?? promotion.employeeId}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {promotion.fromDesignationName ?? '—'} → {promotion.toDesignationName ?? '—'}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{new Date(promotion.effectiveDate).toLocaleDateString()}</td>
                  <td className="px-4 py-3">
                    <StatusBadge status={promotion.status} />
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
