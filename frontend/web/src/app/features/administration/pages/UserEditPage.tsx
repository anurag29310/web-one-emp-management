import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useUser } from '../hooks/useUser'
import { useRoles } from '../hooks/useRoles'
import { userRepository } from '../api'
import { editUserFormSchema, type EditUserFormValues } from '../types/userFormSchema'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { StatusBadge } from '@/app/shared/components/StatusBadge'

export function UserEditPage() {
  const { id } = useParams<{ id: string }>()
  const { user: currentUser } = useAuth()
  const isAdmin = currentUser?.role === 'Admin'
  const navigate = useNavigate()

  const { user, isLoading, error, refresh } = useUser(id)
  const { roles } = useRoles()
  const { result: employeeResult } = useEmployees({ pageSize: 100 })

  const [formError, setFormError] = useState<string | null>(null)
  const [isTogglingStatus, setIsTogglingStatus] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<EditUserFormValues>({ resolver: zodResolver(editUserFormSchema) })

  useEffect(() => {
    if (user) {
      reset({
        userName: user.userName,
        email: user.email,
        roleId: user.roleId ?? '',
        employeeId: user.employeeId ?? '',
      })
    }
  }, [user, reset])

  if (!isAdmin) {
    return (
      <div className="rounded-lg border border-hairline bg-surface-1 p-6">
        <p className="text-sm text-danger">You don't have permission to edit user accounts.</p>
      </div>
    )
  }

  async function onSave(values: EditUserFormValues) {
    if (!user) return
    setFormError(null)
    try {
      await userRepository.update({
        id: user.id,
        userName: values.userName.trim(),
        email: values.email.trim(),
        roleId: values.roleId || undefined,
        employeeId: values.employeeId || undefined,
      })
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update user.')
    }
  }

  async function handleStatusToggle() {
    if (!user) return
    setFormError(null)
    setIsTogglingStatus(true)
    try {
      await userRepository.updateStatus({ id: user.id, isActive: !user.isActive })
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update user status.')
    } finally {
      setIsTogglingStatus(false)
    }
  }

  async function handleDelete() {
    if (!user) return
    setFormError(null)
    setIsDeleting(true)
    try {
      await userRepository.remove(user.id)
      navigate('/admin/users')
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete user.')
      setIsDeleting(false)
    }
  }

  async function handleRestore() {
    if (!user) return
    setFormError(null)
    try {
      await userRepository.restore(user.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to restore user.')
    }
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!user) return null

  return (
    <div className="space-y-4">
      <Link
        to="/admin/users"
        className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover"
      >
        ← Back to users
      </Link>

      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">{user.userName}</h1>
          <p className="text-sm text-ink-subtle">{user.email}</p>
        </div>
        <div className="flex items-center gap-2">
          <StatusBadge status={user.isDeleted ? 'Terminated' : user.isActive ? 'Active' : 'Inactive'} />
        </div>
      </div>

      {user.isDeleted ? (
        <div className="flex items-center justify-between rounded-md bg-surface-2 px-4 py-3 text-sm text-ink">
          <span>This user account has been deleted.</span>
          <button
            type="button"
            onClick={() => void handleRestore()}
            className="font-medium text-primary hover:text-primary-hover"
          >
            Restore
          </button>
        </div>
      ) : (
        <div className="rounded-lg border border-hairline bg-surface-1">
          <form onSubmit={handleSubmit(onSave)} noValidate className="space-y-4 p-6">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-username" className="mb-1 block text-sm font-medium text-ink-muted">
                  Username
                </label>
                <input
                  id="edit-username"
                  aria-invalid={Boolean(errors.userName)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('userName')}
                />
                {errors.userName && <p className="mt-1 text-xs text-danger">{errors.userName.message}</p>}
              </div>
              <div>
                <label htmlFor="edit-email" className="mb-1 block text-sm font-medium text-ink-muted">
                  Email
                </label>
                <input
                  id="edit-email"
                  type="email"
                  aria-invalid={Boolean(errors.email)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('email')}
                />
                {errors.email && <p className="mt-1 text-xs text-danger">{errors.email.message}</p>}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-role" className="mb-1 block text-sm font-medium text-ink-muted">
                  Role
                </label>
                <select
                  id="edit-role"
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('roleId')}
                >
                  <option value="">No role assigned</option>
                  {roles.map((role) => (
                    <option key={role.id} value={role.id}>
                      {role.name}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label htmlFor="edit-employee" className="mb-1 block text-sm font-medium text-ink-muted">
                  Linked employee
                </label>
                <select
                  id="edit-employee"
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('employeeId')}
                >
                  <option value="">Not linked</option>
                  {employeeResult?.data.map((employee) => (
                    <option key={employee.id} value={employee.id}>
                      {employee.fullName}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            {formError && (
              <p role="alert" className="text-sm text-danger">
                {formError}
              </p>
            )}

            <div className="flex items-center justify-between border-t border-hairline pt-4">
              <button
                type="button"
                disabled={isTogglingStatus}
                onClick={() => void handleStatusToggle()}
                className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isTogglingStatus ? 'Saving…' : user.isActive ? 'Deactivate account' : 'Activate account'}
              </button>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  disabled={isDeleting}
                  onClick={() => void handleDelete()}
                  className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {isDeleting ? 'Deleting…' : 'Delete user'}
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {isSubmitting ? 'Saving…' : 'Save changes'}
                </button>
              </div>
            </div>
          </form>
        </div>
      )}
    </div>
  )
}
