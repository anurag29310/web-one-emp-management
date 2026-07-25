import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useDesignations } from '../hooks/useDesignations'
import { designationRepository, type Designation } from '../api'
import { designationFormSchema, type DesignationFormValues } from '../types/designationSchema'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

export function DesignationListPage() {
  const { designations, isLoading, error, refresh } = useDesignations()
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<DesignationFormValues>({
    resolver: zodResolver(designationFormSchema),
    defaultValues: { name: '', code: '', level: '' },
  })

  async function onCreate(values: DesignationFormValues) {
    setFormError(null)
    try {
      await designationRepository.create({
        name: values.name.trim(),
        code: values.code.trim(),
        level: values.level ? Number(values.level) : undefined,
      })
      reset()
      setIsFormOpen(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to create designation.')
    }
  }

  async function handleDelete(designation: Designation) {
    setPendingDeleteId(designation.id)
    try {
      await designationRepository.remove(designation.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete designation.')
    } finally {
      setPendingDeleteId(null)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Designations</h1>
          <p className="text-sm text-ink-subtle">{designations.length} designations</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsFormOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isFormOpen ? 'Cancel' : 'New designation'}
          </button>
        )}
      </div>

      {canManage && (
        <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="New designation">
          <form onSubmit={handleSubmit(onCreate)} noValidate className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="desig-name" className="mb-1 block text-sm font-medium text-ink-muted">
                  Name
                </label>
                <input
                  id="desig-name"
                  aria-invalid={Boolean(errors.name)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('name')}
                />
                {errors.name && <p className="mt-1 text-xs text-danger">{errors.name.message}</p>}
              </div>
              <div>
                <label htmlFor="desig-code" className="mb-1 block text-sm font-medium text-ink-muted">
                  Code
                </label>
                <input
                  id="desig-code"
                  aria-invalid={Boolean(errors.code)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('code')}
                />
                {errors.code && <p className="mt-1 text-xs text-danger">{errors.code.message}</p>}
              </div>
            </div>
            <div>
              <label htmlFor="desig-level" className="mb-1 block text-sm font-medium text-ink-muted">
                Level
              </label>
              <input
                id="desig-level"
                inputMode="numeric"
                aria-invalid={Boolean(errors.level)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...register('level')}
              />
              {errors.level && <p className="mt-1 text-xs text-danger">{errors.level.message}</p>}
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
              {isSubmitting ? 'Saving…' : 'Create designation'}
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
              <th className="px-4 py-3">Level</th>
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

            {!isLoading && designations.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={canManage ? 4 : 3}>
                  No designations yet.
                </td>
              </tr>
            )}

            {!isLoading &&
              designations.map((designation) => (
                <tr key={designation.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link
                      to={`/designations/${designation.id}`}
                      className="font-medium text-ink hover:text-primary-hover"
                    >
                      {designation.name}
                    </Link>
                  </td>
                  <td className="px-4 py-3 font-mono text-ink-subtle">{designation.code}</td>
                  <td className="px-4 py-3 text-ink-muted">{designation.level ?? '—'}</td>
                  {canManage && (
                    <td className="px-4 py-3 text-right">
                      <button
                        type="button"
                        disabled={pendingDeleteId === designation.id}
                        onClick={() => void handleDelete(designation)}
                        className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        {pendingDeleteId === designation.id ? 'Deleting…' : 'Delete'}
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
