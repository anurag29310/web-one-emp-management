import { useCallback, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useDepartments } from '../hooks/useDepartments'
import { departmentRepository, type Department } from '../api'
import { departmentFormSchema, type DepartmentFormValues } from '../types/departmentSchema'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

const UNDO_WINDOW_MS = 6000

export function DepartmentListPage() {
  const { departments, isLoading, error, refresh } = useDepartments()
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)
  const [lastDeleted, setLastDeleted] = useState<Department | null>(null)
  const undoTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<DepartmentFormValues>({
    resolver: zodResolver(departmentFormSchema),
    defaultValues: { name: '', code: '', description: '', headEmployeeId: '' },
  })

  async function onCreate(values: DepartmentFormValues) {
    setFormError(null)
    try {
      await departmentRepository.create({
        name: values.name.trim(),
        code: values.code?.trim() || undefined,
        description: values.description?.trim() || undefined,
      })
      reset()
      setIsFormOpen(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to create department.')
    }
  }

  const dismissUndo = useCallback(() => {
    if (undoTimerRef.current) {
      clearTimeout(undoTimerRef.current)
      undoTimerRef.current = null
    }
    setLastDeleted(null)
  }, [])

  async function handleDelete(department: Department) {
    setPendingDeleteId(department.id)
    try {
      await departmentRepository.remove(department.id)
      dismissUndo()
      setLastDeleted(department)
      refresh()
      undoTimerRef.current = setTimeout(() => setLastDeleted(null), UNDO_WINDOW_MS)
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete department.')
    } finally {
      setPendingDeleteId(null)
    }
  }

  async function handleUndo() {
    if (!lastDeleted) return
    const department = lastDeleted
    dismissUndo()
    try {
      await departmentRepository.restore(department.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to restore department.')
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Departments</h1>
          <p className="text-sm text-ink-subtle">{departments.length} departments</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsFormOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isFormOpen ? 'Cancel' : 'New department'}
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
        <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="New department">
          <form onSubmit={handleSubmit(onCreate)} noValidate className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="dept-name" className="mb-1 block text-sm font-medium text-ink-muted">
                  Name
                </label>
                <input
                  id="dept-name"
                  aria-invalid={Boolean(errors.name)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('name')}
                />
                {errors.name && <p className="mt-1 text-xs text-danger">{errors.name.message}</p>}
              </div>
              <div>
                <label htmlFor="dept-code" className="mb-1 block text-sm font-medium text-ink-muted">
                  Code
                </label>
                <input
                  id="dept-code"
                  aria-invalid={Boolean(errors.code)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('code')}
                />
                {errors.code && <p className="mt-1 text-xs text-danger">{errors.code.message}</p>}
              </div>
            </div>
            <div>
              <label htmlFor="dept-description" className="mb-1 block text-sm font-medium text-ink-muted">
                Description
              </label>
              <textarea
                id="dept-description"
                rows={2}
                aria-invalid={Boolean(errors.description)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...register('description')}
              />
              {errors.description && <p className="mt-1 text-xs text-danger">{errors.description.message}</p>}
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
              {isSubmitting ? 'Saving…' : 'Create department'}
            </button>
          </form>
        </Modal>
      )}

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Code</th>
              <th className="px-4 py-3">Description</th>
              {canManage && <th className="px-4 py-3" />}
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={canManage ? 4 : 3}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && departments.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={canManage ? 4 : 3}>
                  No departments yet.
                </td>
              </tr>
            )}

            {!isLoading &&
              departments.map((department) => (
                <tr key={department.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link
                      to={`/departments/${department.id}`}
                      className="font-medium text-ink hover:text-primary-hover"
                    >
                      {department.name}
                    </Link>
                  </td>
                  <td className="px-4 py-3 font-mono text-ink-subtle">{department.code ?? '—'}</td>
                  <td className="px-4 py-3 text-ink-muted">{department.description ?? '—'}</td>
                  {canManage && (
                    <td className="px-4 py-3 text-right">
                      <button
                        type="button"
                        disabled={pendingDeleteId === department.id}
                        onClick={() => void handleDelete(department)}
                        className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        {pendingDeleteId === department.id ? 'Deleting…' : 'Delete'}
                      </button>
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
