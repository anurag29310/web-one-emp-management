import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { AppError } from '@/app/shared/models/appError'
import { authRepository } from '../api'
import { resetPasswordSchema, type ResetPasswordFormValues } from '../types/authSchemas'
import { PASSWORD_POLICY_RULES } from '../types/passwordPolicy'

export function ResetPasswordPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [formError, setFormError] = useState<string | null>(null)
  const [isComplete, setIsComplete] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    // A real reset link arrives as /reset-password?email=...&token=..., so pre-fill from the
    // query string when present while still letting the user type/paste either field.
    defaultValues: {
      email: searchParams.get('email') ?? '',
      resetToken: searchParams.get('token') ?? '',
    },
  })

  async function onSubmit(values: ResetPasswordFormValues) {
    setFormError(null)
    try {
      await authRepository.resetPassword({
        email: values.email.trim(),
        resetToken: values.resetToken.trim(),
        newPassword: values.newPassword,
      })
      setIsComplete(true)
    } catch (err) {
      setFormError(
        err instanceof AppError ? err.message : 'Unable to reset your password. Please try again.',
      )
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
              Reset password
            </h1>
            <p className="mt-1 text-sm text-ink-subtle">
              Enter the reset code from your email along with a new password.
            </p>
          </div>
        </div>

        <div className="rounded-lg border border-hairline bg-surface-1 p-8">
          {isComplete ? (
            <div className="space-y-4 text-center">
              <div className="mx-auto flex h-10 w-10 items-center justify-center rounded-full bg-success/15 text-success">
                <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.5} stroke="currentColor" className="h-5 w-5">
                  <path strokeLinecap="round" strokeLinejoin="round" d="m4.5 12.75 6 6 9-13.5" />
                </svg>
              </div>
              <p className="text-sm text-ink">Your password has been reset. You can now sign in.</p>
              <button
                type="button"
                onClick={() => navigate('/login', { replace: true })}
                className="w-full rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
              >
                Go to login
              </button>
            </div>
          ) : (
            <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
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
                <label htmlFor="resetToken" className="mb-1.5 block text-sm font-medium text-ink-muted">
                  Reset code
                </label>
                <input
                  id="resetToken"
                  type="text"
                  autoComplete="one-time-code"
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 font-mono text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  aria-invalid={Boolean(errors.resetToken)}
                  {...register('resetToken')}
                />
                {errors.resetToken && (
                  <p className="mt-1 text-xs text-danger">{errors.resetToken.message}</p>
                )}
              </div>

              <div>
                <label htmlFor="newPassword" className="mb-1.5 block text-sm font-medium text-ink-muted">
                  New password
                </label>
                <input
                  id="newPassword"
                  type="password"
                  autoComplete="new-password"
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  aria-invalid={Boolean(errors.newPassword)}
                  {...register('newPassword')}
                />
                {errors.newPassword && (
                  <p className="mt-1 text-xs text-danger">{errors.newPassword.message}</p>
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

              <div>
                <label htmlFor="confirmNewPassword" className="mb-1.5 block text-sm font-medium text-ink-muted">
                  Confirm new password
                </label>
                <input
                  id="confirmNewPassword"
                  type="password"
                  autoComplete="new-password"
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  aria-invalid={Boolean(errors.confirmNewPassword)}
                  {...register('confirmNewPassword')}
                />
                {errors.confirmNewPassword && (
                  <p className="mt-1 text-xs text-danger">{errors.confirmNewPassword.message}</p>
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
                {isSubmitting ? 'Resetting…' : 'Reset password'}
              </button>
            </form>
          )}
        </div>

        {!isComplete && (
          <p className="mt-4 text-center text-sm text-ink-subtle">
            Need a code?{' '}
            <Link to="/forgot-password" className="font-medium text-primary-hover hover:underline">
              Request one
            </Link>
          </p>
        )}
      </div>
    </div>
  )
}
