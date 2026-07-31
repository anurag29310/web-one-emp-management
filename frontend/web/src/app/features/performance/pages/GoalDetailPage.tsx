import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useGoal } from '../hooks/useGoal'
import { performanceRepository, type GoalStatus } from '../api'
import {
  addGoalKpiFormSchema,
  goalProgressFormSchema,
  kpiProgressFormSchema,
  updateGoalFormSchema,
  type AddGoalKpiFormInput,
  type AddGoalKpiFormValues,
  type GoalProgressFormInput,
  type GoalProgressFormValues,
  type KpiProgressFormInput,
  type KpiProgressFormValues,
  type UpdateGoalFormInput,
  type UpdateGoalFormValues,
} from '../types/performanceSchema'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'
import { StatusBadge } from '@/app/shared/components/StatusBadge'

const STATUS_OPTIONS: GoalStatus[] = ['NotStarted', 'InProgress', 'Completed', 'Cancelled']

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 text-sm text-ink">{value}</p>
    </div>
  )
}

export function GoalDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { goal, isLoading, error, refresh } = useGoal(id)
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR' || user?.role === 'Manager'

  const [isEditing, setIsEditing] = useState(false)
  const [isUpdatingProgress, setIsUpdatingProgress] = useState(false)
  const [isAddingKpi, setIsAddingKpi] = useState(false)
  const [kpiInProgress, setKpiInProgress] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  const editForm = useForm<UpdateGoalFormInput, unknown, UpdateGoalFormValues>({
    resolver: zodResolver(updateGoalFormSchema),
  })
  const resetEditForm = editForm.reset
  const progressForm = useForm<GoalProgressFormInput, unknown, GoalProgressFormValues>({
    resolver: zodResolver(goalProgressFormSchema),
    defaultValues: { progressPercent: 0 },
  })
  const resetProgressForm = progressForm.reset
  const kpiForm = useForm<AddGoalKpiFormInput, unknown, AddGoalKpiFormValues>({
    resolver: zodResolver(addGoalKpiFormSchema),
    defaultValues: { name: '', targetValue: 0, unit: '', notes: '' },
  })
  const kpiProgressForm = useForm<KpiProgressFormInput, unknown, KpiProgressFormValues>({
    resolver: zodResolver(kpiProgressFormSchema),
    defaultValues: { currentValue: 0, notes: '' },
  })

  useEffect(() => {
    if (goal) {
      resetEditForm({
        title: goal.title,
        description: goal.description ?? '',
        category: goal.category ?? '',
        targetDate: goal.targetDate.slice(0, 10),
        weight: goal.weight ?? undefined,
        status: goal.status,
      })
      resetProgressForm({ progressPercent: goal.progressPercent })
    }
  }, [goal, resetEditForm, resetProgressForm])

  async function onSaveEdit(values: UpdateGoalFormValues) {
    if (!goal) return
    setFormError(null)
    try {
      await performanceRepository.updateGoal(goal.id, {
        title: values.title.trim(),
        description: values.description?.trim() || undefined,
        category: values.category?.trim() || undefined,
        targetDate: values.targetDate,
        weight: values.weight,
        status: values.status,
      })
      setIsEditing(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update goal.')
    }
  }

  async function onUpdateProgress(values: GoalProgressFormValues) {
    if (!goal) return
    setFormError(null)
    try {
      await performanceRepository.updateGoalProgress(goal.id, { progressPercent: values.progressPercent })
      setIsUpdatingProgress(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update progress.')
    }
  }

  async function onAddKpi(values: AddGoalKpiFormValues) {
    if (!goal) return
    setFormError(null)
    try {
      await performanceRepository.addGoalKpi(goal.id, {
        name: values.name.trim(),
        targetValue: values.targetValue,
        unit: values.unit?.trim() || undefined,
        notes: values.notes?.trim() || undefined,
      })
      kpiForm.reset()
      setIsAddingKpi(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to add KPI.')
    }
  }

  async function onUpdateKpiProgress(values: KpiProgressFormValues) {
    if (!kpiInProgress) return
    setFormError(null)
    try {
      await performanceRepository.updateGoalKpiProgress(kpiInProgress, {
        currentValue: values.currentValue,
        notes: values.notes?.trim() || undefined,
      })
      kpiProgressForm.reset()
      setKpiInProgress(null)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update KPI progress.')
    }
  }

  async function handleDelete() {
    if (!goal) return
    setIsDeleting(true)
    setFormError(null)
    try {
      await performanceRepository.removeGoal(goal.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete goal.')
    } finally {
      setIsDeleting(false)
    }
  }

  async function handleRestore() {
    if (!goal) return
    setFormError(null)
    try {
      await performanceRepository.restoreGoal(goal.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to restore goal.')
    }
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!goal) return null

  const canUpdateProgress = goal.status === 'NotStarted' || goal.status === 'InProgress'

  return (
    <div className="space-y-4">
      <Link
        to="/performance/goals"
        className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover"
      >
        ← Back to goals
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">{goal.title}</h1>
            <p className="font-mono text-sm text-ink-subtle">{goal.goalNumber}</p>
          </div>
          <div className="flex items-center gap-2">
            <StatusBadge status={goal.status} />
            {goal.isDeleted && canManage && (
              <button
                type="button"
                onClick={() => void handleRestore()}
                className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
              >
                Restore
              </button>
            )}
            {!goal.isDeleted && canUpdateProgress && (
              <button
                type="button"
                onClick={() => setIsUpdatingProgress((open) => !open)}
                className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
              >
                {isUpdatingProgress ? 'Cancel' : 'Update progress'}
              </button>
            )}
            {!goal.isDeleted && canManage && (
              <>
                <button
                  type="button"
                  onClick={() => setIsAddingKpi((open) => !open)}
                  className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                >
                  {isAddingKpi ? 'Cancel' : 'Add KPI'}
                </button>
                <button
                  type="button"
                  onClick={() => setIsEditing((open) => !open)}
                  className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                >
                  {isEditing ? 'Cancel' : 'Edit'}
                </button>
                <button
                  type="button"
                  disabled={isDeleting}
                  onClick={() => void handleDelete()}
                  className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {isDeleting ? 'Deleting…' : 'Delete'}
                </button>
              </>
            )}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Employee" value={goal.employeeName ?? goal.employeeId} />
          <Field label="Category" value={goal.category ?? '—'} />
          <Field label="Start date" value={new Date(goal.startDate).toLocaleDateString()} />
          <Field label="Target date" value={new Date(goal.targetDate).toLocaleDateString()} />
          <Field label="Weight" value={goal.weight != null ? `${goal.weight}%` : '—'} />
          <Field label="Progress" value={`${goal.progressPercent}%`} />
          <Field label="Description" value={goal.description ?? '—'} />
        </div>
        {formError && !isEditing && !isUpdatingProgress && !isAddingKpi && !kpiInProgress && (
          <p role="alert" className="px-6 pb-6 text-sm text-danger">
            {formError}
          </p>
        )}
      </div>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="border-b border-hairline p-6">
          <h2 className="text-[18px] font-medium text-ink">KPIs</h2>
        </div>
        <div className="overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
              <tr>
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">Target</th>
                <th className="px-4 py-3">Current</th>
                <th className="px-4 py-3">Notes</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-hairline">
              {goal.kpis.length === 0 && (
                <tr>
                  <td className="px-4 py-8 text-center text-ink-subtle" colSpan={5}>
                    No KPIs tracked for this goal yet.
                  </td>
                </tr>
              )}
              {goal.kpis.map((kpi) => (
                <tr key={kpi.id}>
                  <td className="px-4 py-3 font-medium text-ink">{kpi.name}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {kpi.targetValue}
                    {kpi.unit ? ` ${kpi.unit}` : ''}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">
                    {kpi.currentValue}
                    {kpi.unit ? ` ${kpi.unit}` : ''}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{kpi.notes ?? '—'}</td>
                  <td className="px-4 py-3 text-right">
                    {!goal.isDeleted && (
                      <button
                        type="button"
                        onClick={() => {
                          kpiProgressForm.reset({ currentValue: kpi.currentValue, notes: kpi.notes ?? '' })
                          setKpiInProgress(kpi.id)
                        }}
                        className="rounded-md bg-surface-2 px-3 py-1.5 text-sm font-medium text-ink transition hover:bg-surface-3"
                      >
                        Update
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <Modal isOpen={isEditing} onClose={() => setIsEditing(false)} title="Edit goal">
        <form onSubmit={editForm.handleSubmit(onSaveEdit)} noValidate className="space-y-3">
          <div>
            <label htmlFor="edit-title" className="mb-1 block text-sm font-medium text-ink-muted">
              Title
            </label>
            <input
              id="edit-title"
              aria-invalid={Boolean(editForm.formState.errors.title)}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...editForm.register('title')}
            />
            {editForm.formState.errors.title && (
              <p className="mt-1 text-xs text-danger">{editForm.formState.errors.title.message}</p>
            )}
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="edit-category" className="mb-1 block text-sm font-medium text-ink-muted">
                Category
              </label>
              <input
                id="edit-category"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...editForm.register('category')}
              />
            </div>
            <div>
              <label htmlFor="edit-weight" className="mb-1 block text-sm font-medium text-ink-muted">
                Weight (%)
              </label>
              <input
                id="edit-weight"
                type="number"
                min="0"
                max="100"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...editForm.register('weight')}
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="edit-target-date" className="mb-1 block text-sm font-medium text-ink-muted">
                Target date
              </label>
              <input
                id="edit-target-date"
                type="date"
                aria-invalid={Boolean(editForm.formState.errors.targetDate)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...editForm.register('targetDate')}
              />
              {editForm.formState.errors.targetDate && (
                <p className="mt-1 text-xs text-danger">{editForm.formState.errors.targetDate.message}</p>
              )}
            </div>
            <div>
              <label htmlFor="edit-status" className="mb-1 block text-sm font-medium text-ink-muted">
                Status
              </label>
              <select
                id="edit-status"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...editForm.register('status')}
              >
                {STATUS_OPTIONS.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div>
            <label htmlFor="edit-description" className="mb-1 block text-sm font-medium text-ink-muted">
              Description
            </label>
            <textarea
              id="edit-description"
              rows={2}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...editForm.register('description')}
            />
          </div>
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

      <Modal isOpen={isUpdatingProgress} onClose={() => setIsUpdatingProgress(false)} title="Update progress">
        <form onSubmit={progressForm.handleSubmit(onUpdateProgress)} noValidate className="space-y-3">
          <div>
            <label htmlFor="progress-percent" className="mb-1 block text-sm font-medium text-ink-muted">
              Progress (%)
            </label>
            <input
              id="progress-percent"
              type="number"
              min="0"
              max="100"
              aria-invalid={Boolean(progressForm.formState.errors.progressPercent)}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...progressForm.register('progressPercent')}
            />
            {progressForm.formState.errors.progressPercent && (
              <p className="mt-1 text-xs text-danger">{progressForm.formState.errors.progressPercent.message}</p>
            )}
          </div>
          {formError && (
            <p role="alert" className="text-sm text-danger">
              {formError}
            </p>
          )}
          <button
            type="submit"
            disabled={progressForm.formState.isSubmitting}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
          >
            {progressForm.formState.isSubmitting ? 'Saving…' : 'Update progress'}
          </button>
        </form>
      </Modal>

      <Modal isOpen={isAddingKpi} onClose={() => setIsAddingKpi(false)} title="Add KPI">
        <form onSubmit={kpiForm.handleSubmit(onAddKpi)} noValidate className="space-y-3">
          <div>
            <label htmlFor="kpi-name" className="mb-1 block text-sm font-medium text-ink-muted">
              Name
            </label>
            <input
              id="kpi-name"
              aria-invalid={Boolean(kpiForm.formState.errors.name)}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...kpiForm.register('name')}
            />
            {kpiForm.formState.errors.name && (
              <p className="mt-1 text-xs text-danger">{kpiForm.formState.errors.name.message}</p>
            )}
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="kpi-target" className="mb-1 block text-sm font-medium text-ink-muted">
                Target value
              </label>
              <input
                id="kpi-target"
                type="number"
                min="0"
                aria-invalid={Boolean(kpiForm.formState.errors.targetValue)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...kpiForm.register('targetValue')}
              />
              {kpiForm.formState.errors.targetValue && (
                <p className="mt-1 text-xs text-danger">{kpiForm.formState.errors.targetValue.message}</p>
              )}
            </div>
            <div>
              <label htmlFor="kpi-unit" className="mb-1 block text-sm font-medium text-ink-muted">
                Unit
              </label>
              <input
                id="kpi-unit"
                placeholder="%, units, days…"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...kpiForm.register('unit')}
              />
            </div>
          </div>
          <div>
            <label htmlFor="kpi-notes" className="mb-1 block text-sm font-medium text-ink-muted">
              Notes
            </label>
            <textarea
              id="kpi-notes"
              rows={2}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...kpiForm.register('notes')}
            />
          </div>
          {formError && (
            <p role="alert" className="text-sm text-danger">
              {formError}
            </p>
          )}
          <button
            type="submit"
            disabled={kpiForm.formState.isSubmitting}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
          >
            {kpiForm.formState.isSubmitting ? 'Adding…' : 'Add KPI'}
          </button>
        </form>
      </Modal>

      <Modal isOpen={kpiInProgress !== null} onClose={() => setKpiInProgress(null)} title="Update KPI progress">
        <form onSubmit={kpiProgressForm.handleSubmit(onUpdateKpiProgress)} noValidate className="space-y-3">
          <div>
            <label htmlFor="kpi-current-value" className="mb-1 block text-sm font-medium text-ink-muted">
              Current value
            </label>
            <input
              id="kpi-current-value"
              type="number"
              min="0"
              aria-invalid={Boolean(kpiProgressForm.formState.errors.currentValue)}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...kpiProgressForm.register('currentValue')}
            />
            {kpiProgressForm.formState.errors.currentValue && (
              <p className="mt-1 text-xs text-danger">{kpiProgressForm.formState.errors.currentValue.message}</p>
            )}
          </div>
          <div>
            <label htmlFor="kpi-progress-notes" className="mb-1 block text-sm font-medium text-ink-muted">
              Notes
            </label>
            <textarea
              id="kpi-progress-notes"
              rows={2}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...kpiProgressForm.register('notes')}
            />
          </div>
          {formError && (
            <p role="alert" className="text-sm text-danger">
              {formError}
            </p>
          )}
          <button
            type="submit"
            disabled={kpiProgressForm.formState.isSubmitting}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
          >
            {kpiProgressForm.formState.isSubmitting ? 'Saving…' : 'Update KPI'}
          </button>
        </form>
      </Modal>
    </div>
  )
}
