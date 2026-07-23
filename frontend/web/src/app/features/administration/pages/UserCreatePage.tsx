import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useRoles } from '../hooks/useRoles'
import { userRepository } from '../api'
import { createUserFormSchema, type CreateUserFormValues } from '../types/userFormSchema'
import { PASSWORD_POLICY_RULES } from '@/app/features/auth/types/passwordPolicy'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'

export function UserCreatePage() {
  const { user: currentUser } = useAuth()
  const isAdmin = currentUser?.role === 'Admin'
  const navigate = useNavigate()

  const { roles } = useRoles()
  const { result: employeeResult } = useEmployees({ pageSize: 100 })
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CreateUserFormValues>({
    resolver: zodResolver(createUserFormSchema),
    defaultValues: {
      userName: '',
      email: '',
      temporaryPassword: '',
      roleId: '',
      employeeId: '',
      isActive: true,
    },
  })

  if (!isAdmin) {
    return (
      <div className="rounded-lg border border-hairline bg-surface-1 p-6">
        <p className="text-sm text-danger">You don't have permission to create user accounts.</p>
      </div>
    )
  }

  async function onSubmit(values: CreateUserFormValues) {
    setFormError(null)
    try {
      const created = await userRepository.create({
        userName: values.userName.trim(),
        email: values.email.trim(),
        temporaryPassword: values.temporaryPassword,
        roleId: values.roleId || undefined,
        employeeId: values.employeeId || undefined,
        isActive: values.isActive,
      })
      navigate(`/admin/users/${created.id}/edit`, { replace: true })
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to create user.')
    }
  }

  return (
    <div className="space-y-4">
      <Link
        to="/admin/users"
        className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover"
      >
        ← Back to users
      </Link>

      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">New user</h1>
        <p className="text-sm text-ink-subtle">Create a user account and assign an initial role.</p>
      </div>

      <div className="max-w-xl rounded-lg border border-hairline bg-surface-1 p-6">
        <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="create-username" className="mb-1 block text-sm font-medium text-ink-muted">
                Username
              </label>
              <input
                id="create-username"
                autoComplete="username"
                aria-invalid={Boolean(errors.userName)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...register('userName')}
              />
              {errors.userName && <p className="mt-1 text-xs text-danger">{errors.userName.message}</p>}
            </div>
            <div>
              <label htmlFor="create-email" className="mb-1 block text-sm font-medium text-ink-muted">
                Email
              </label>
              <input
                id="create-email"
                type="email"
                autoComplete="email"
                aria-invalid={Boolean(errors.email)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...register('email')}
              />
              {errors.email && <p className="mt-1 text-xs text-danger">{errors.email.message}</p>}
            </div>
          </div>

          <div>
            <label htmlFor="create-password" className="mb-1 block text-sm font-medium text-ink-muted">
              Temporary password
            </label>
            <input
              id="create-password"
              type="password"
              autoComplete="new-password"
              aria-invalid={Boolean(errors.temporaryPassword)}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...register('temporaryPassword')}
            />
            {errors.temporaryPassword && (
              <p className="mt-1 text-xs text-danger">{errors.temporaryPassword.message}</p>
            )}
            <ul className="mt-2 space-y-0.5 text-xs text-ink-subtle">
              {PASSWORD_POLICY_RULES.map((rule) => (
                <li key={rule} className="flex items-center gap-1.5">
                  <span className="h-1 w-1 rounded-full bg-ink-tertiary" aria-hidden="true" />
                  {rule}
                </li>
              ))}
            </ul>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="create-role" className="mb-1 block text-sm font-medium text-ink-muted">
                Role
              </label>
              <select
                id="create-role"
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
              <label htmlFor="create-employee" className="mb-1 block text-sm font-medium text-ink-muted">
                Linked employee
              </label>
              <select
                id="create-employee"
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

          <div className="flex items-center gap-2">
            <input
              id="create-isActive"
              type="checkbox"
              className="h-4 w-4 rounded border-hairline-strong text-primary focus:ring-primary-focus"
              {...register('isActive')}
            />
            <label htmlFor="create-isActive" className="text-sm font-medium text-ink-muted">
              Account is active
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
            {isSubmitting ? 'Creating…' : 'Create user'}
          </button>
        </form>
      </div>
    </div>
  )
}
