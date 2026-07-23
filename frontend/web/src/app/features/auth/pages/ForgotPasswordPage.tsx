import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate } from 'react-router-dom'
import { AppError } from '@/app/shared/models/appError'
import { authRepository } from '../api'
import { forgotPasswordSchema, type ForgotPasswordFormValues } from '../types/authSchemas'

export function ForgotPasswordPage() {
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)
  const [isSent, setIsSent] = useState(false)
  const [submittedEmail, setSubmittedEmail] = useState('')

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordFormValues>({ resolver: zodResolver(forgotPasswordSchema) })

  async function onSubmit(values: ForgotPasswordFormValues) {
    setFormError(null)
    try {
      await authRepository.forgotPassword({ email: values.email.trim() })
      // Always show the same success state, whether or not the email belongs to an account —
      // the API itself never reveals that, so the UI must not either.
      setSubmittedEmail(values.email.trim())
      setIsSent(true)
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Something went wrong. Please try again.')
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
              Forgot password
            </h1>
            <p className="mt-1 text-sm text-ink-subtle">
              We&apos;ll send you a link to reset it if an account exists.
            </p>
          </div>
        </div>

        <div className="rounded-lg border border-hairline bg-surface-1 p-8">
          {isSent ? (
            <div className="space-y-4 text-center">
              <div className="mx-auto flex h-10 w-10 items-center justify-center rounded-full bg-success/15 text-success">
                <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.5} stroke="currentColor" className="h-5 w-5">
                  <path strokeLinecap="round" strokeLinejoin="round" d="m4.5 12.75 6 6 9-13.5" />
                </svg>
              </div>
              <p className="text-sm text-ink">
                If an account exists for <span className="font-medium text-ink">{submittedEmail}</span>, we&apos;ve
                sent an email with instructions to reset your password.
              </p>
              <p className="text-xs text-ink-subtle">
                Didn&apos;t get it? Check your spam folder, or try again in a few minutes.
              </p>
              <Link
                to="/reset-password"
                className="inline-block text-sm font-medium text-primary-hover hover:underline"
              >
                I already have a reset code
              </Link>
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
                  autoFocus
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  aria-invalid={Boolean(errors.email)}
                  {...register('email')}
                />
                {errors.email && <p className="mt-1 text-xs text-danger">{errors.email.message}</p>}
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
                {isSubmitting ? 'Sending…' : 'Send reset link'}
              </button>
            </form>
          )}
        </div>

        <p className="mt-4 text-center text-sm text-ink-subtle">
          <button
            type="button"
            onClick={() => navigate('/login')}
            className="font-medium text-primary-hover hover:underline"
          >
            Back to login
          </button>
        </p>
      </div>
    </div>
  )
}
