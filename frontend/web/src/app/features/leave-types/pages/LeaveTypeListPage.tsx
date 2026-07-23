import { useCallback, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useLeaveTypes } from '../hooks/useLeaveTypes'
import { leaveTypeRepository, type LeaveType } from '../api'
import { leaveTypeFormSchema, type LeaveTypeFormValues } from '../types/leaveTypeSchema'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

const UNDO_WINDOW_MS = 6000

const DEFAULT_VALUES: LeaveTypeFormValues = {
  name: '',
  code: '',
  isPaid: true,
  requiresApproval: true,
  annualEntitlementDays: '',
}

function LeaveTypeForm({
  defaultValues,
  submitLabel,
  onSubmit,
}: {
  defaultValues: LeaveTypeFormValues
  submitLabel: string
  onSubmit: (values: LeaveTypeFormValues) => Promise<void>
}) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LeaveTypeFormValues>({
    resolver: zodResolver(leaveTypeFormSchema),
    defaultValues,
  })

  async function submit(values: LeaveTypeFormValues) {
    setFormError(null)
    try {
      await onSubmit(values)
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to save leave type.')
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="lt-name" className="mb-1 block text-sm font-medium text-ink-muted">
            Name
          </label>
          <input
            id="lt-name"
            aria-invalid={Boolean(errors.name)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('name')}
          />
          {errors.name && <p className="mt-1 text-xs text-danger">{errors.name.message}</p>}
        </div>
        <div>
          <label htmlFor="lt-code" className="mb-1 block text-sm font-medium text-ink-muted">
            Code
          </label>
          <input
            id="lt-code"
            aria-invalid={Boolean(errors.code)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('code')}
          />
          {errors.code && <p className="mt-1 text-xs text-danger">{errors.code.message}</p>}
        </div>
      </div>

      <div>
        <label htmlFor="lt-entitlement" className="mb-1 block text-sm font-medium text-ink-muted">
          Annual entitlement (days)
        </label>
        <input
          id="lt-entitlement"
          inputMode="decimal"
          placeholder="Leave blank if not applicable"
          aria-invalid={Boolean(errors.annualEntitlementDays)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('annualEntitlementDays')}
        />
        {errors.annualEntitlementDays && (
          <p className="mt-1 text-xs text-danger">{errors.annualEntitlementDays.message}</p>
        )}
      </div>

      <div className="flex items-center gap-6">
        <label className="flex items-center gap-2 text-sm text-ink-muted">
          <input
            type="checkbox"
            className="h-4 w-4 rounded border-hairline-strong bg-surface-2 text-primary focus:ring-primary-focus/50"
            {...register('isPaid')}
          />
          Paid leave
        </label>
        <label className="flex items-center gap-2 text-sm text-ink-muted">
          <input
            type="checkbox"
            className="h-4 w-4 rounded border-hairline-strong bg-surface-2 text-primary focus:ring-primary-focus/50"
            {...register('requiresApproval')}
          />
          Requires approval
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

export function LeaveTypeListPage() {
  const { leaveTypes, isLoading, error, refresh } = useLeaveTypes()
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [editing, setEditing] = useState<LeaveType | null>(null)
  const [listError, setListError] = useState<string | null>(null)
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)
  const [lastDeleted, setLastDeleted] = useState<LeaveType | null>(null)
  const undoTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  async function handleCreate(values: LeaveTypeFormValues) {
    await leaveTypeRepository.create({
      name: values.name.trim(),
      code: values.code?.trim() || undefined,
      isPaid: values.isPaid,
      requiresApproval: values.requiresApproval,
      annualEntitlementDays: values.annualEntitlementDays ? Number(values.annualEntitlementDays) : undefined,
    })
    setIsCreateOpen(false)
    refresh()
  }

  async function handleUpdate(values: LeaveTypeFormValues) {
    if (!editing) return
    await leaveTypeRepository.update({
      id: editing.id,
      name: values.name.trim(),
      code: values.code?.trim() || undefined,
      isPaid: values.isPaid,
      requiresApproval: values.requiresApproval,
      annualEntitlementDays: values.annualEntitlementDays ? Number(values.annualEntitlementDays) : undefined,
    })
    setEditing(null)
    refresh()
  }

  const dismissUndo = useCallback(() => {
    if (undoTimerRef.current) {
      clearTimeout(undoTimerRef.current)
      undoTimerRef.current = null
    }
    setLastDeleted(null)
  }, [])

  async function handleDelete(leaveType: LeaveType) {
    setPendingDeleteId(leaveType.id)
    try {
      await leaveTypeRepository.remove(leaveType.id)
      dismissUndo()
      setLastDeleted(leaveType)
      refresh()
      undoTimerRef.current = setTimeout(() => setLastDeleted(null), UNDO_WINDOW_MS)
    } catch (err) {
      setListError(err instanceof AppError ? err.message : 'Failed to delete leave type.')
    } finally {
      setPendingDeleteId(null)
    }
  }

  async function handleUndo() {
    if (!lastDeleted) return
    const leaveType = lastDeleted
    dismissUndo()
    try {
      await leaveTypeRepository.restore(leaveType.id)
      refresh()
    } catch (err) {
      setListError(err instanceof AppError ? err.message : 'Failed to restore leave type.')
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Leave Types</h1>
          <p className="text-sm text-ink-subtle">{leaveTypes.length} leave types</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsCreateOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isCreateOpen ? 'Cancel' : 'New leave type'}
          </button>
        )}
      </div>

      {lastDeleted && (
        <div className="flex items-center justify-between rounded-md bg-surface-2 px-4 py-3 text-sm text-ink">
          <span>
            <span className="font-medium">{lastDeleted.name}</span> was deleted.
          </span>
          <button
            type="button"
            onClick={() => void handleUndo()}
            className="font-medium text-primary hover:text-primary-hover"
          >
            Undo
          </button>
        </div>
      )}

      {canManage && (
        <Modal isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)} title="New leave type">
          <LeaveTypeForm defaultValues={DEFAULT_VALUES} submitLabel="Create leave type" onSubmit={handleCreate} />
        </Modal>
      )}

      {canManage && (
        <Modal isOpen={editing !== null} onClose={() => setEditing(null)} title="Edit leave type">
          {editing && (
            <LeaveTypeForm
              key={editing.id}
              defaultValues={{
                name: editing.name,
                code: editing.code ?? '',
                isPaid: editing.isPaid,
                requiresApproval: editing.requiresApproval,
                annualEntitlementDays: editing.annualEntitlementDays?.toString() ?? '',
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

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Code</th>
              <th className="px-4 py-3">Paid</th>
              <th className="px-4 py-3">Approval</th>
              <th className="px-4 py-3">Annual entitlement</th>
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

            {!isLoading && leaveTypes.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={canManage ? 6 : 5}>
                  No leave types yet.
                </td>
              </tr>
            )}

            {!isLoading &&
              leaveTypes.map((leaveType) => (
                <tr key={leaveType.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3 font-medium text-ink">{leaveType.name}</td>
                  <td className="px-4 py-3 font-mono text-ink-subtle">{leaveType.code ?? '—'}</td>
                  <td className="px-4 py-3 text-ink-muted">{leaveType.isPaid ? 'Yes' : 'No'}</td>
                  <td className="px-4 py-3 text-ink-muted">{leaveType.requiresApproval ? 'Required' : 'Auto'}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {leaveType.annualEntitlementDays ?? '—'}
                  </td>
                  {canManage && (
                    <td className="px-4 py-3 text-right">
                      <div className="flex justify-end gap-3">
                        <button
                          type="button"
                          onClick={() => setEditing(leaveType)}
                          className="text-xs font-medium text-primary-hover hover:underline"
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          disabled={pendingDeleteId === leaveType.id}
                          onClick={() => void handleDelete(leaveType)}
                          className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                        >
                          {pendingDeleteId === leaveType.id ? 'Deleting…' : 'Delete'}
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
