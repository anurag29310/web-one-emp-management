import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useGoals } from '../hooks/useGoals'
import { performanceRepository, type GoalStatus } from '../api'
import { createGoalFormSchema, type CreateGoalFormInput, type CreateGoalFormValues } from '../types/performanceSchema'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'
import { StatusBadge } from '@/app/shared/components/StatusBadge'

const STATUS_OPTIONS: GoalStatus[] = ['NotStarted', 'InProgress', 'Completed', 'Cancelled']

function CreateGoalForm({ onCreated }: { onCreated: () => void }) {
  const [formError, setFormError] = useState<string | null>(null)
  const { result: employeeResult } = useEmployees({ pageSize: 100 })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateGoalFormInput, unknown, CreateGoalFormValues>({
    resolver: zodResolver(createGoalFormSchema),
    defaultValues: {
      employeeId: '',
      title: '',
      description: '',
      category: '',
      startDate: '',
      targetDate: '',
    },
  })

  async function onSubmit(values: CreateGoalFormValues) {
    setFormError(null)
    try {
      await performanceRepository.createGoal({
        employeeId: values.employeeId,
        title: values.title.trim(),
        description: values.description?.trim() || undefined,
        category: values.category?.trim() || undefined,
        startDate: values.startDate,
        targetDate: values.targetDate,
        weight: values.weight,
      })
      reset()
      onCreated()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to create goal.')
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-3">
      <div>
        <label htmlFor="goal-employee" className="mb-1 block text-sm font-medium text-ink-muted">
          Employee
        </label>
        <select
          id="goal-employee"
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
        <label htmlFor="goal-title" className="mb-1 block text-sm font-medium text-ink-muted">
          Title
        </label>
        <input
          id="goal-title"
          aria-invalid={Boolean(errors.title)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('title')}
        />
        {errors.title && <p className="mt-1 text-xs text-danger">{errors.title.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="goal-category" className="mb-1 block text-sm font-medium text-ink-muted">
            Category
          </label>
          <input
            id="goal-category"
            placeholder="Delivery, Sales, People…"
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('category')}
          />
        </div>
        <div>
          <label htmlFor="goal-weight" className="mb-1 block text-sm font-medium text-ink-muted">
            Weight (%)
          </label>
          <input
            id="goal-weight"
            type="number"
            min="0"
            max="100"
            aria-invalid={Boolean(errors.weight)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('weight')}
          />
          {errors.weight && <p className="mt-1 text-xs text-danger">{errors.weight.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="goal-start-date" className="mb-1 block text-sm font-medium text-ink-muted">
            Start date
          </label>
          <input
            id="goal-start-date"
            type="date"
            aria-invalid={Boolean(errors.startDate)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('startDate')}
          />
          {errors.startDate && <p className="mt-1 text-xs text-danger">{errors.startDate.message}</p>}
        </div>
        <div>
          <label htmlFor="goal-target-date" className="mb-1 block text-sm font-medium text-ink-muted">
            Target date
          </label>
          <input
            id="goal-target-date"
            type="date"
            aria-invalid={Boolean(errors.targetDate)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('targetDate')}
          />
          {errors.targetDate && <p className="mt-1 text-xs text-danger">{errors.targetDate.message}</p>}
        </div>
      </div>

      <div>
        <label htmlFor="goal-description" className="mb-1 block text-sm font-medium text-ink-muted">
          Description
        </label>
        <textarea
          id="goal-description"
          rows={2}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('description')}
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
        {isSubmitting ? 'Creating…' : 'Set goal'}
      </button>
    </form>
  )
}

export function GoalListPage() {
  const [status, setStatus] = useState<GoalStatus | ''>('')
  const { result, isLoading, error, refresh } = useGoals({
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
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Goals</h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsFormOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isFormOpen ? 'Cancel' : 'Set goal'}
          </button>
        )}
      </div>

      {canManage && (
        <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="Set goal">
          <CreateGoalForm
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
          onChange={(e) => setStatus(e.target.value as GoalStatus | '')}
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
              <th className="px-4 py-3">Goal #</th>
              <th className="px-4 py-3">Employee</th>
              <th className="px-4 py-3">Title</th>
              <th className="px-4 py-3">Category</th>
              <th className="px-4 py-3">Progress</th>
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
                  No goals found.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((goal) => (
                <tr key={goal.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link
                      to={`/performance/goals/${goal.id}`}
                      className="font-mono font-medium text-ink hover:text-primary-hover"
                    >
                      {goal.goalNumber}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{goal.employeeName ?? goal.employeeId}</td>
                  <td className="px-4 py-3 text-ink-muted">{goal.title}</td>
                  <td className="px-4 py-3 text-ink-muted">{goal.category ?? '—'}</td>
                  <td className="px-4 py-3 text-ink-muted">{goal.progressPercent}%</td>
                  <td className="px-4 py-3">
                    <StatusBadge status={goal.status} />
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
