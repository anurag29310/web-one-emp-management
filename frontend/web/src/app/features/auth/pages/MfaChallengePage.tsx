import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '@/app/core/auth/useAuth'
import { AppError } from '@/app/shared/models/appError'
import { mfaChallengeSchema, type MfaChallengeFormValues } from '../types/authSchemas'

interface MfaChallengeState {
  mfaChallengeId?: string
  from?: string
}

export function MfaChallengePage() {
  const { completeMfaLogin } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [formError, setFormError] = useState<string | null>(null)

  const state = (location.state as MfaChallengeState | null) ?? {}
  const { mfaChallengeId, from } = state
  const redirectTo = from ?? '/dashboard'

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<MfaChallengeFormValues>({ resolver: zodResolver(mfaChallengeSchema) })

  // No challenge to complete (e.g. the user navigated here directly) — send them back to login.
  if (!mfaChallengeId) {
    return <Navigate to="/login" replace />
  }

  async function onSubmit(values: MfaChallengeFormValues) {
    setFormError(null)
    try {
      await completeMfaLogin({ mfaChallengeId: mfaChallengeId as string, code: values.code.trim() })
      navigate(redirectTo, { replace: true })
    } catch (err) {
      setFormError(
        err instanceof AppError ? err.message : 'Unable to verify your code. Please try again.',
      )
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
              Two-factor verification
            </h1>
            <p className="mt-1 text-sm text-ink-subtle">
              Enter the 6-digit code from your authenticator app, or a recovery code.
            </p>
          </div>
        </div>

        <div className="rounded-lg border border-hairline bg-surface-1 p-8">
          <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
            <div>
              <label htmlFor="code" className="mb-1.5 block text-sm font-medium text-ink-muted">
                Verification code
              </label>
              <input
                id="code"
                type="text"
                inputMode="text"
                autoComplete="one-time-code"
                autoFocus
                placeholder="123456 or XXXXX-XXXXX"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                aria-invalid={Boolean(errors.code)}
                {...register('code')}
              />
              {errors.code && <p className="mt-1 text-xs text-danger">{errors.code.message}</p>}
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
              {isSubmitting ? 'Verifying…' : 'Verify'}
            </button>
          </form>
        </div>

        <p className="mt-4 text-center text-sm text-ink-subtle">
          Challenge expired or wrong account?{' '}
          <button
            type="button"
            onClick={() => navigate('/login', { replace: true })}
            className="font-medium text-primary-hover hover:underline"
          >
            Back to login
          </button>
        </p>
      </div>
    </div>
  )
}
