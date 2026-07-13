import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useDepartment } from '../hooks/useDepartment'
import { departmentRepository } from '../api'
import { departmentFormSchema, type DepartmentFormValues } from '../types/departmentSchema'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 text-sm text-ink">{value}</p>
    </div>
  )
}

export function DepartmentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { department, isLoading, error, refresh } = useDepartment(id)
  const { user } = useAuth()
  const navigate = useNavigate()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const { result: employeeResult } = useEmployees({ pageSize: 100 })

  const [isEditing, setIsEditing] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<DepartmentFormValues>({ resolver: zodResolver(departmentFormSchema) })

  useEffect(() => {
    if (department) {
      reset({
        name: department.name,
        code: department.code ?? '',
        description: department.description ?? '',
        headEmployeeId: department.headEmployeeId ?? '',
      })
    }
  }, [department, reset])

  async function onSave(values: DepartmentFormValues) {
    if (!department) return
    setFormError(null)
    try {
      await departmentRepository.update({
        id: department.id,
        name: values.name.trim(),
        code: values.code?.trim() || undefined,
        description: values.description?.trim() || undefined,
        headEmployeeId: values.headEmployeeId || undefined,
      })
      setIsEditing(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update department.')
    }
  }

  async function handleDelete() {
    if (!department) return
    setIsDeleting(true)
    try {
      await departmentRepository.remove(department.id)
      navigate('/departments')
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete department.')
      setIsDeleting(false)
    }
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!department) return null

  const headEmployee = employeeResult?.data.find((e) => e.id === department.headEmployeeId)

  return (
    <div className="space-y-4">
      <Link
        to="/departments"
        className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover"
      >
        ← Back to departments
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
              {department.name}
            </h1>
            <p className="text-sm text-ink-subtle">{department.code ?? 'No code'}</p>
          </div>
          <div className="flex items-center gap-2">
            <Link
              to={`/employees?departmentId=${department.id}`}
              className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
            >
              View employees
            </Link>
            {canManage && (
              <>
                <button
                  type="button"
                  onClick={() => setIsEditing((editing) => !editing)}
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
          <Field label="Description" value={department.description ?? '—'} />
          <Field label="Head of department" value={headEmployee?.fullName ?? '—'} />
          <Field label="Created" value={new Date(department.createdAtUtc).toLocaleDateString()} />
          <Field
            label="Last updated"
            value={department.updatedAtUtc ? new Date(department.updatedAtUtc).toLocaleDateString() : '—'}
          />
        </div>
        {!isEditing && formError && (
          <p role="alert" className="px-6 pb-6 text-sm text-danger">
            {formError}
          </p>
        )}
      </div>

      {canManage && (
        <Modal isOpen={isEditing} onClose={() => setIsEditing(false)} title="Edit department">
          <form onSubmit={handleSubmit(onSave)} noValidate className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-name" className="mb-1 block text-sm font-medium text-ink-muted">
                  Name
                </label>
                <input
                  id="edit-name"
                  aria-invalid={Boolean(errors.name)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('name')}
                />
                {errors.name && <p className="mt-1 text-xs text-danger">{errors.name.message}</p>}
              </div>
              <div>
                <label htmlFor="edit-code" className="mb-1 block text-sm font-medium text-ink-muted">
                  Code
                </label>
                <input
                  id="edit-code"
                  aria-invalid={Boolean(errors.code)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('code')}
                />
                {errors.code && <p className="mt-1 text-xs text-danger">{errors.code.message}</p>}
              </div>
            </div>
            <div>
              <label htmlFor="edit-description" className="mb-1 block text-sm font-medium text-ink-muted">
                Description
              </label>
              <textarea
                id="edit-description"
                rows={2}
                aria-invalid={Boolean(errors.description)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...register('description')}
              />
              {errors.description && (
                <p className="mt-1 text-xs text-danger">{errors.description.message}</p>
              )}
            </div>
            <div>
              <label htmlFor="edit-head" className="mb-1 block text-sm font-medium text-ink-muted">
                Head of department
              </label>
              <select
                id="edit-head"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...register('headEmployeeId')}
              >
                <option value="">No head assigned</option>
                {employeeResult?.data.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.fullName}
                  </option>
                ))}
              </select>
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
              {isSubmitting ? 'Saving…' : 'Save changes'}
            </button>
          </form>
        </Modal>
      )}
    </div>
  )
}
