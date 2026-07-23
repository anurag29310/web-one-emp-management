import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '@/app/core/auth/useAuth'
import { AppError } from '@/app/shared/models/appError'
import { registerSchema, type RegisterFormValues } from '../types/authSchemas'
import { PASSWORD_POLICY_RULES } from '../types/passwordPolicy'

export function RegisterPage() {
  const { register: registerAccount } = useAuth()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({ resolver: zodResolver(registerSchema) })

  async function onSubmit(values: RegisterFormValues) {
    setFormError(null)
    try {
      await registerAccount({
        userName: values.userName.trim(),
        email: values.email.trim(),
        password: values.password,
      })
      navigate('/dashboard', { replace: true })
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Unable to create your account. Please try again.')
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-canvas px-4 py-8">
      <div className="w-full max-w-sm">
        <div className="mb-6 flex flex-col items-center gap-3">
          <span className="flex h-11 w-11 items-center justify-center rounded-lg bg-primary text-lg font-semibold text-white">
            E
          </span>
          <div className="text-center">
            <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">
              Create your account
            </h1>
            <p className="mt-1 text-sm text-ink-subtle">Join the Employee Management System</p>
          </div>
        </div>

        <div className="rounded-lg border border-hairline bg-surface-1 p-8">
          <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
            <div>
              <label htmlFor="userName" className="mb-1.5 block text-sm font-medium text-ink-muted">
                Username
              </label>
              <input
                id="userName"
                type="text"
                autoComplete="username"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                aria-invalid={Boolean(errors.userName)}
                {...register('userName')}
              />
              {errors.userName && <p className="mt-1 text-xs text-danger">{errors.userName.message}</p>}
            </div>

            <div>
              <label htmlFor="email" className="mb-1.5 block text-sm font-medium text-ink-muted">
                Email
              </label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                aria-invalid={Boolean(errors.email)}
                {...register('email')}
              />
              {errors.email && <p className="mt-1 text-xs text-danger">{errors.email.message}</p>}
            </div>

            <div>
              <label htmlFor="password" className="mb-1.5 block text-sm font-medium text-ink-muted">
                Password
              </label>
              <input
                id="password"
                type="password"
                autoComplete="new-password"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                aria-invalid={Boolean(errors.password)}
                {...register('password')}
              />
              {errors.password && <p className="mt-1 text-xs text-danger">{errors.password.message}</p>}
              <ul className="mt-2 space-y-0.5 text-xs text-ink-subtle">
                {PASSWORD_POLICY_RULES.map((rule) => (
                  <li key={rule} className="flex items-center gap-1.5">
                    <span className="h-1 w-1 rounded-full bg-ink-tertiary" aria-hidden="true" />
                    {rule}
                  </li>
                ))}
              </ul>
            </div>

            <div>
              <label htmlFor="confirmPassword" className="mb-1.5 block text-sm font-medium text-ink-muted">
                Confirm password
              </label>
              <input
                id="confirmPassword"
                type="password"
                autoComplete="new-password"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                aria-invalid={Boolean(errors.confirmPassword)}
                {...register('confirmPassword')}
              />
              {errors.confirmPassword && (
                <p className="mt-1 text-xs text-danger">{errors.confirmPassword.message}</p>
              )}
            </div>

            {formError && (
              <p role="alert" className="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger">
                {formError}
              </p>
            )}

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? 'Creating account…' : 'Create account'}
            </button>
          </form>
        </div>

        <p className="mt-4 text-center text-sm text-ink-subtle">
          Already have an account?{' '}
          <Link to="/login" className="font-medium text-primary-hover hover:underline">
            Sign in
          </Link>
        </p>
      </div>
    </div>
  )
}
