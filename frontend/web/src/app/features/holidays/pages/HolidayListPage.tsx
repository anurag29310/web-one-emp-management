import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useHolidays } from '../hooks/useHolidays'
import { holidayRepository, type Holiday } from '../api'
import { holidayFormSchema, type HolidayFormValues } from '../types/holidaySchema'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

const DEFAULT_VALUES: HolidayFormValues = {
  name: '',
  officeLocationId: '',
  holidayDate: '',
  isOptional: false,
}

function HolidayForm({
  defaultValues,
  submitLabel,
  onSubmit,
}: {
  defaultValues: HolidayFormValues
  submitLabel: string
  onSubmit: (values: HolidayFormValues) => Promise<void>
}) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<HolidayFormValues>({
    resolver: zodResolver(holidayFormSchema),
    defaultValues,
  })

  async function submit(values: HolidayFormValues) {
    setFormError(null)
    try {
      await onSubmit(values)
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to save holiday.')
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="space-y-3">
      <div>
        <label htmlFor="hol-name" className="mb-1 block text-sm font-medium text-ink-muted">
          Name
        </label>
        <input
          id="hol-name"
          aria-invalid={Boolean(errors.name)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('name')}
        />
        {errors.name && <p className="mt-1 text-xs text-danger">{errors.name.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="hol-date" className="mb-1 block text-sm font-medium text-ink-muted">
            Date
          </label>
          <input
            id="hol-date"
            type="date"
            aria-invalid={Boolean(errors.holidayDate)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('holidayDate')}
          />
          {errors.holidayDate && <p className="mt-1 text-xs text-danger">{errors.holidayDate.message}</p>}
        </div>
        <div>
          <label htmlFor="hol-office" className="mb-1 block text-sm font-medium text-ink-muted">
            Office location ID
          </label>
          <input
            id="hol-office"
            placeholder="Leave blank for org-wide"
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('officeLocationId')}
          />
        </div>
      </div>

      <label className="flex items-center gap-2 text-sm text-ink-muted">
        <input
          type="checkbox"
          className="h-4 w-4 rounded border-hairline-strong bg-surface-2 text-primary focus:ring-primary-focus/50"
          {...register('isOptional')}
        />
        Optional holiday
      </label>

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

export function HolidayListPage() {
  const [year, setYear] = useState(() => new Date().getFullYear())
  const { holidays, isLoading, error, refresh } = useHolidays({ year })
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [editing, setEditing] = useState<Holiday | null>(null)
  const [listError, setListError] = useState<string | null>(null)
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)

  const groupedByMonth = useMemo(() => {
    const groups = new Map<number, Holiday[]>()
    for (const holiday of holidays) {
      const month = new Date(holiday.holidayDate).getMonth()
      groups.set(month, [...(groups.get(month) ?? []), holiday])
    }
    return [...groups.entries()].sort(([a], [b]) => a - b)
  }, [holidays])

  async function handleCreate(values: HolidayFormValues) {
    await holidayRepository.create({
      name: values.name.trim(),
      officeLocationId: values.officeLocationId?.trim() || undefined,
      holidayDate: values.holidayDate,
      isOptional: values.isOptional,
    })
    setIsCreateOpen(false)
    refresh()
  }

  async function handleUpdate(values: HolidayFormValues) {
    if (!editing) return
    await holidayRepository.update({
      id: editing.id,
      name: values.name.trim(),
      officeLocationId: values.officeLocationId?.trim() || undefined,
      holidayDate: values.holidayDate,
      isOptional: values.isOptional,
    })
    setEditing(null)
    refresh()
  }

  async function handleDelete(holiday: Holiday) {
    setPendingDeleteId(holiday.id)
    try {
      await holidayRepository.remove(holiday.id)
      refresh()
    } catch (err) {
      setListError(err instanceof AppError ? err.message : 'Failed to delete holiday.')
    } finally {
      setPendingDeleteId(null)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Holiday Calendar</h1>
          <p className="text-sm text-ink-subtle">{holidays.length} holidays in {year}</p>
        </div>
        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1 rounded-md border border-hairline-strong bg-surface-2 px-1 py-1">
            <button
              type="button"
              onClick={() => setYear((y) => y - 1)}
              aria-label="Previous year"
              className="rounded px-2 py-1 text-sm text-ink-subtle hover:bg-surface-3 hover:text-ink"
            >
              ‹
            </button>
            <span className="px-2 text-sm font-medium text-ink">{year}</span>
            <button
              type="button"
              onClick={() => setYear((y) => y + 1)}
              aria-label="Next year"
              className="rounded px-2 py-1 text-sm text-ink-subtle hover:bg-surface-3 hover:text-ink"
            >
              ›
            </button>
          </div>
          {canManage && (
            <button
              type="button"
              onClick={() => setIsCreateOpen((open) => !open)}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
            >
              {isCreateOpen ? 'Cancel' : 'New holiday'}
            </button>
          )}
        </div>
      </div>

      {canManage && (
        <Modal isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)} title="New holiday">
          <HolidayForm defaultValues={DEFAULT_VALUES} submitLabel="Create holiday" onSubmit={handleCreate} />
        </Modal>
      )}

      {canManage && (
        <Modal isOpen={editing !== null} onClose={() => setEditing(null)} title="Edit holiday">
          {editing && (
            <HolidayForm
              key={editing.id}
              defaultValues={{
                name: editing.name,
                officeLocationId: editing.officeLocationId ?? '',
                holidayDate: editing.holidayDate.slice(0, 10),
                isOptional: editing.isOptional,
              }}
              submitLabel="Save changes"
              onSubmit={handleUpdate}
            />
          )}
        </Modal>
      )}

      {(error || listError) && (
        <p role="alert" className="text-sm text-danger">
          {error ?? listError}
        </p>
      )}

      {isLoading && (
        <div className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="h-16 animate-pulse rounded-lg border border-hairline bg-surface-1" />
          ))}
        </div>
      )}

      {!isLoading && holidays.length === 0 && (
        <div className="rounded-lg border border-hairline bg-surface-1 px-4 py-8 text-center text-sm text-ink-subtle">
          No holidays scheduled for {year}.
        </div>
      )}

      {!isLoading && groupedByMonth.length > 0 && (
        <div className="space-y-4">
          {groupedByMonth.map(([month, monthHolidays]) => (
            <div key={month} className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
              <div className="border-b border-hairline bg-surface-2 px-4 py-2 text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
                {MONTH_NAMES[month]}
              </div>
              <ul className="divide-y divide-hairline">
                {monthHolidays
                  .slice()
                  .sort((a, b) => a.holidayDate.localeCompare(b.holidayDate))
                  .map((holiday) => (
                    <li key={holiday.id} className="flex items-center justify-between px-4 py-3">
                      <div className="flex items-center gap-3">
                        <span className="w-12 shrink-0 font-mono text-sm text-ink-subtle">
                          {new Date(holiday.holidayDate).toLocaleDateString(undefined, {
                            day: '2-digit',
                            month: 'short',
                          })}
                        </span>
                        <span className="text-sm font-medium text-ink">{holiday.name}</span>
                        {holiday.isOptional && (
                          <span className="inline-flex items-center rounded-full bg-surface-2 px-2.5 py-0.5 text-xs font-medium text-ink-subtle ring-1 ring-inset ring-hairline-strong">
                            Optional
                          </span>
                        )}
                        {!holiday.officeLocationId && (
                          <span className="text-xs text-ink-subtle">Org-wide</span>
                        )}
                      </div>
                      {canManage && (
                        <div className="flex items-center gap-3">
                          <button
                            type="button"
                            onClick={() => setEditing(holiday)}
                            className="text-xs font-medium text-primary-hover hover:underline"
                          >
                            Edit
                          </button>
                          <button
                            type="button"
                            disabled={pendingDeleteId === holiday.id}
                            onClick={() => void handleDelete(holiday)}
                            className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                          >
                            {pendingDeleteId === holiday.id ? 'Deleting…' : 'Delete'}
                          </button>
                        </div>
                      )}
                    </li>
                  ))}
              </ul>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
