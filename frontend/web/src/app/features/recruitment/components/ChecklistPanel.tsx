import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useChecklist } from '../hooks/useChecklist'
import { recruitmentRepository } from '../api'
import { addChecklistItemFormSchema, type AddChecklistItemFormValues } from '../types/recruitmentSchema'
import { AppError } from '@/app/shared/models/appError'

const inputClass =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClass = 'mb-1 block text-sm font-medium text-ink-muted'

export function ChecklistPanel({ candidateId, canManage }: { candidateId: string; canManage: boolean }) {
  const { items, isLoading, error, refresh } = useChecklist(candidateId)
  const [isAdding, setIsAdding] = useState(false)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [panelError, setPanelError] = useState<string | null>(null)

  const addForm = useForm<AddChecklistItemFormValues>({
    resolver: zodResolver(addChecklistItemFormSchema),
    defaultValues: { itemName: '', notes: '' },
  })

  async function onAdd(values: AddChecklistItemFormValues) {
    setPanelError(null)
    try {
      await recruitmentRepository.addChecklistItem(candidateId, {
        itemName: values.itemName.trim(),
        notes: values.notes?.trim() || undefined,
      })
      addForm.reset()
      setIsAdding(false)
      refresh()
    } catch (err) {
      setPanelError(err instanceof AppError ? err.message : 'Failed to add checklist item.')
    }
  }

  async function handleComplete(itemId: string) {
    setBusyId(itemId)
    setPanelError(null)
    try {
      await recruitmentRepository.completeChecklistItem(itemId)
      refresh()
    } catch (err) {
      setPanelError(err instanceof AppError ? err.message : 'Failed to complete checklist item.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="rounded-lg border border-hairline bg-surface-1">
      <div className="flex items-center justify-between border-b border-hairline p-6">
        <h2 className="text-[18px] font-medium text-ink">Onboarding checklist</h2>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsAdding((open) => !open)}
            className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
          >
            {isAdding ? 'Cancel' : 'Add item'}
          </button>
        )}
      </div>

      {(error || panelError) && <p className="px-6 pt-4 text-sm text-danger">{error ?? panelError}</p>}

      <div className="p-6">
        {!isLoading && items.length === 0 && (
          <p className="text-sm text-ink-subtle">
            No checklist yet — the default set is created automatically once an offer is accepted.
          </p>
        )}
        <ul className="divide-y divide-hairline">
          {items.map((item) => (
            <li key={item.id} className="flex items-center justify-between py-2">
              <div>
                <p className={`text-sm ${item.isCompleted ? 'text-ink-subtle line-through' : 'text-ink'}`}>{item.itemName}</p>
                {item.completedAtUtc && (
                  <p className="text-xs text-ink-subtle">Completed {new Date(item.completedAtUtc).toLocaleDateString()}</p>
                )}
              </div>
              {canManage && !item.isCompleted && (
                <button
                  type="button"
                  disabled={busyId === item.id}
                  onClick={() => void handleComplete(item.id)}
                  className="text-xs font-medium text-primary-hover hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                >
                  Mark complete
                </button>
              )}
            </li>
          ))}
        </ul>

        {isAdding && (
          <form onSubmit={addForm.handleSubmit(onAdd)} noValidate className="mt-4 space-y-2 border-t border-hairline pt-4">
            <div>
              <label htmlFor="checklist-item-name" className={labelClass}>
                Item name
              </label>
              <input id="checklist-item-name" className={inputClass} {...addForm.register('itemName')} />
              {addForm.formState.errors.itemName && <p className="mt-1 text-xs text-danger">{addForm.formState.errors.itemName.message}</p>}
            </div>
            <div>
              <label htmlFor="checklist-item-notes" className={labelClass}>
                Notes
              </label>
              <textarea id="checklist-item-notes" rows={2} className={inputClass} {...addForm.register('notes')} />
            </div>
            <button
              type="submit"
              disabled={addForm.formState.isSubmitting}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              Add item
            </button>
          </form>
        )}
      </div>
    </div>
  )
}
