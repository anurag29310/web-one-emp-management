import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '@/app/core/auth/useAuth'
import { AppError } from '@/app/shared/models/appError'
import { appConfig } from '@/app/core/config/env'

const loginSchema = z.object({
  userNameOrEmail: z.string().min(1, 'Username or email is required.'),
  password: z.string().min(1, 'Password is required.'),
})

type LoginFormValues = z.infer<typeof loginSchema>

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) })

  const redirectTo = (location.state as { from?: string } | null)?.from ?? '/dashboard'

  async function onSubmit(values: LoginFormValues) {
    setFormError(null)
    try {
      const outcome = await login(values)
      if (outcome.requiresMfa) {
        navigate('/login/mfa', {
          replace: true,
          state: { mfaChallengeId: outcome.mfaChallengeId, from: redirectTo },
        })
        return
      }
      navigate(redirectTo, { replace: true })
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Unable to sign in. Please try again.')
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-canvas px-4">
      <div className="w-full max-w-sm">
        <div className="mb-6 flex flex-col items-center gap-3">
          <span className="flex h-11 w-11 items-center justify-center rounded-lg bg-primary text-lg font-semibold text-white">
            E
          </span>
          <div className="text-center">
            <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">
              Employee Management System
            </h1>
            <p className="mt-1 text-sm text-ink-subtle">Sign in to continue</p>
          </div>
        </div>

        <div className="rounded-lg border border-hairline bg-surface-1 p-8">
          <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
            <div>
              <label htmlFor="userNameOrEmail" className="mb-1.5 block text-sm font-medium text-ink-muted">
                Username or email
              </label>
              <input
                id="userNameOrEmail"
                type="text"
                autoComplete="username"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                aria-invalid={Boolean(errors.userNameOrEmail)}
                {...register('userNameOrEmail')}
              />
              {errors.userNameOrEmail && (
                <p className="mt-1 text-xs text-danger">{errors.userNameOrEmail.message}</p>
              )}
            </div>

            <div>
              <div className="mb-1.5 flex items-center justify-between">
                <label htmlFor="password" className="block text-sm font-medium text-ink-muted">
                  Password
                </label>
                <Link to="/forgot-password" className="text-xs text-ink-subtle hover:text-primary-hover">
                  Forgot password?
                </Link>
              </div>
              <input
                id="password"
                type="password"
                autoComplete="current-password"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                aria-invalid={Boolean(errors.password)}
                {...register('password')}
              />
              {errors.password && <p className="mt-1 text-xs text-danger">{errors.password.message}</p>}
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
              {isSubmitting ? 'Signing in…' : 'Sign in'}
            </button>
          </form>
        </div>

        <p className="mt-4 text-center text-sm text-ink-subtle">
          Don&apos;t have an account?{' '}
          <Link to="/register" className="font-medium text-primary-hover hover:underline">
            Create one
          </Link>
        </p>

        {appConfig.dataSource === 'mock' && (
          <div className="mt-4 rounded-lg border border-hairline bg-surface-1 px-4 py-3 text-xs text-ink-subtle">
            <p className="mb-1 font-medium text-ink-muted">Mock data source active — try:</p>
            <p className="font-mono">admin / Admin@123</p>
            <p className="mt-1 font-mono">manager / Manager@123 (MFA enabled — any 6-digit code works)</p>
          </div>
        )}
      </div>
    </div>
  )
}
