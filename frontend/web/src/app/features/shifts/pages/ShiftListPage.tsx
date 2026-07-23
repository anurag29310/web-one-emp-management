import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useShifts } from '../hooks/useShifts'
import { shiftRepository, type Shift } from '../api'
import { shiftFormSchema, toTimeInputValue, toTimeSpanValue, type ShiftFormValues } from '../types/shiftSchema'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

const EMPTY_FORM: ShiftFormValues = {
  name: '',
  startTime: '09:00',
  endTime: '17:00',
  graceMinutes: 10,
  isNightShift: false,
}

function ShiftForm({
  defaultValues,
  submitLabel,
  onSubmit,
}: {
  defaultValues: ShiftFormValues
  submitLabel: string
  onSubmit: (values: ShiftFormValues) => Promise<void>
}) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ShiftFormValues>({ resolver: zodResolver(shiftFormSchema), defaultValues })

  async function submit(values: ShiftFormValues) {
    setFormError(null)
    try {
      await onSubmit(values)
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to save shift.')
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="space-y-3">
      <div>
        <label htmlFor="shift-name" className="mb-1 block text-sm font-medium text-ink-muted">
          Name
        </label>
        <input
          id="shift-name"
          aria-invalid={Boolean(errors.name)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('name')}
        />
        {errors.name && <p className="mt-1 text-xs text-danger">{errors.name.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="shift-start" className="mb-1 block text-sm font-medium text-ink-muted">
            Start time
          </label>
          <input
            id="shift-start"
            type="time"
            aria-invalid={Boolean(errors.startTime)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('startTime')}
          />
          {errors.startTime && <p className="mt-1 text-xs text-danger">{errors.startTime.message}</p>}
        </div>
        <div>
          <label htmlFor="shift-end" className="mb-1 block text-sm font-medium text-ink-muted">
            End time
          </label>
          <input
            id="shift-end"
            type="time"
            aria-invalid={Boolean(errors.endTime)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('endTime')}
          />
          {errors.endTime && <p className="mt-1 text-xs text-danger">{errors.endTime.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-2 items-end gap-3">
        <div>
          <label htmlFor="shift-grace" className="mb-1 block text-sm font-medium text-ink-muted">
            Grace minutes
          </label>
          <input
            id="shift-grace"
            type="number"
            min={0}
            aria-invalid={Boolean(errors.graceMinutes)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('graceMinutes', { valueAsNumber: true })}
          />
          {errors.graceMinutes && <p className="mt-1 text-xs text-danger">{errors.graceMinutes.message}</p>}
        </div>
        <label className="flex items-center gap-2 pb-2 text-sm text-ink-muted">
          <input
            type="checkbox"
            className="h-4 w-4 rounded border-hairline-strong bg-surface-2 text-primary focus:ring-primary-focus"
            {...register('isNightShift')}
          />
          Night shift
        </label>
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

export function ShiftListPage() {
  const { shifts, isLoading, error, refresh } = useShifts()
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [editingShift, setEditingShift] = useState<Shift | null>(null)
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)
  const [listError, setListError] = useState<string | null>(null)

  async function handleCreate(values: ShiftFormValues) {
    await shiftRepository.create({
      name: values.name.trim(),
      startTime: toTimeSpanValue(values.startTime),
      endTime: toTimeSpanValue(values.endTime),
      graceMinutes: values.graceMinutes,
      isNightShift: values.isNightShift,
    })
    setIsCreateOpen(false)
    refresh()
  }

  async function handleEdit(values: ShiftFormValues) {
    if (!editingShift) return
    await shiftRepository.update({
      id: editingShift.id,
      name: values.name.trim(),
      startTime: toTimeSpanValue(values.startTime),
      endTime: toTimeSpanValue(values.endTime),
      graceMinutes: values.graceMinutes,
      isNightShift: values.isNightShift,
    })
    setEditingShift(null)
    refresh()
  }

  async function handleDelete(shift: Shift) {
    setPendingDeleteId(shift.id)
    setListError(null)
    try {
      await shiftRepository.remove(shift.id)
      refresh()
    } catch (err) {
      setListError(err instanceof AppError ? err.message : 'Failed to delete shift.')
    } finally {
      setPendingDeleteId(null)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Shifts</h1>
          <p className="text-sm text-ink-subtle">{shifts.length} shift definitions</p>
        </div>
        <div className="flex items-center gap-2">
          <Link
            to="/shifts/assignments"
            className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
          >
            Assign employees
          </Link>
          {canManage && (
            <button
              type="button"
              onClick={() => setIsCreateOpen((open) => !open)}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
            >
              New shift
            </button>
          )}
        </div>
      </div>

      {canManage && (
        <Modal isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)} title="New shift">
          <ShiftForm defaultValues={EMPTY_FORM} submitLabel="Create shift" onSubmit={handleCreate} />
        </Modal>
      )}

      {canManage && (
        <Modal isOpen={editingShift !== null} onClose={() => setEditingShift(null)} title="Edit shift">
          {editingShift && (
            <ShiftForm
              defaultValues={{
                name: editingShift.name,
                startTime: toTimeInputValue(editingShift.startTime),
                endTime: toTimeInputValue(editingShift.endTime),
                graceMinutes: editingShift.graceMinutes,
                isNightShift: editingShift.isNightShift,
              }}
              submitLabel="Save changes"
              onSubmit={handleEdit}
            />
          )}
        </Modal>
      )}

      {(error || listError) && (
        <p role="alert" className="text-sm text-danger">
          {error ?? listError}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Start</th>
              <th className="px-4 py-3">End</th>
              <th className="px-4 py-3">Grace</th>
              <th className="px-4 py-3">Type</th>
              {canManage && <th className="px-4 py-3" />}
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={canManage ? 6 : 5}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && shifts.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={canManage ? 6 : 5}>
                  No shifts defined yet.
                </td>
              </tr>
            )}

            {!isLoading &&
              shifts.map((shift) => (
                <tr key={shift.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3 font-medium text-ink">{shift.name}</td>
                  <td className="px-4 py-3 font-mono text-ink-subtle">{toTimeInputValue(shift.startTime)}</td>
                  <td className="px-4 py-3 font-mono text-ink-subtle">{toTimeInputValue(shift.endTime)}</td>
                  <td className="px-4 py-3 text-ink-muted">{shift.graceMinutes} min</td>
                  <td className="px-4 py-3 text-ink-muted">{shift.isNightShift ? 'Night' : 'Day'}</td>
                  {canManage && (
                    <td className="px-4 py-3 text-right">
                      <div className="flex justify-end gap-3">
                        <button
                          type="button"
                          onClick={() => setEditingShift(shift)}
                          className="text-xs font-medium text-ink-muted hover:text-ink"
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          disabled={pendingDeleteId === shift.id}
                          onClick={() => void handleDelete(shift)}
                          className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                        >
                          {pendingDeleteId === shift.id ? 'Deleting…' : 'Delete'}
                        </button>
                      </div>
                    </td>
                  )}
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
