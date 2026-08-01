import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '@/app/core/auth/useAuth'
import { AppError } from '@/app/shared/models/appError'
import { PASSWORD_POLICY_RULES } from '@/app/features/auth/types/passwordPolicy'
import { companyRegistrationRepository } from '../api'
import { companyRegistrationSchema, type CompanyRegistrationFormValues } from '../types/companyRegistrationSchema'

export function CompanyRegisterPage() {
  const { establishSession } = useAuth()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)
  const [isStatusLoading, setIsStatusLoading] = useState(true)
  const [isRegistrationEnabled, setIsRegistrationEnabled] = useState(true)
  const [pendingApproval, setPendingApproval] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CompanyRegistrationFormValues>({
    resolver: zodResolver(companyRegistrationSchema),
    defaultValues: { timezone: 'UTC', currency: 'USD' },
  })

  useEffect(() => {
    let cancelled = false
    companyRegistrationRepository
      .getStatus()
      .then((enabled) => {
        if (!cancelled) setIsRegistrationEnabled(enabled)
      })
      .catch(() => {
        if (!cancelled) setIsRegistrationEnabled(false)
      })
      .finally(() => {
        if (!cancelled) setIsStatusLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  async function onSubmit(values: CompanyRegistrationFormValues) {
    setFormError(null)
    try {
      const result = await companyRegistrationRepository.register({
        companyName: values.companyName.trim(),
        timezone: values.timezone,
        currency: values.currency,
        adminUserName: values.adminUserName.trim(),
        adminEmail: values.adminEmail.trim(),
        adminPassword: values.adminPassword,
      })

      if (result.requiresApproval || !result.accessToken || !result.refreshToken) {
        setPendingApproval(true)
        return
      }

      await establishSession(result.accessToken, result.refreshToken)
      navigate('/dashboard', { replace: true })
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Unable to register your company. Please try again.')
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
              Register your company
            </h1>
            <p className="mt-1 text-sm text-ink-subtle">Create a company and its first admin account</p>
          </div>
        </div>

        <div className="rounded-lg border border-hairline bg-surface-1 p-8">
          {isStatusLoading ? (
            <div className="h-32 animate-pulse rounded-md bg-surface-2" />
          ) : pendingApproval ? (
            <div className="space-y-4 text-center">
              <div className="mx-auto flex h-10 w-10 items-center justify-center rounded-full bg-success/15 text-success">
                <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.5} stroke="currentColor" className="h-5 w-5">
                  <path strokeLinecap="round" strokeLinejoin="round" d="m4.5 12.75 6 6 9-13.5" />
                </svg>
              </div>
              <p className="text-sm text-ink">
                Your company has been registered and is awaiting Super Admin approval. You&apos;ll be able to sign
                in once it&apos;s approved.
              </p>
              <Link to="/login" className="inline-block text-sm font-medium text-primary-hover hover:underline">
                Back to login
              </Link>
            </div>
          ) : !isRegistrationEnabled ? (
            <div className="space-y-4 text-center">
              <p className="text-sm text-ink">Registration is currently closed.</p>
              <Link to="/login" className="inline-block text-sm font-medium text-primary-hover hover:underline">
                Back to login
              </Link>
            </div>
          ) : (
            <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
              <div>
                <label htmlFor="companyName" className="mb-1.5 block text-sm font-medium text-ink-muted">
                  Company name
                </label>
                <input
                  id="companyName"
                  type="text"
                  autoComplete="organization"
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  aria-invalid={Boolean(errors.companyName)}
                  {...register('companyName')}
                />
                {errors.companyName && <p className="mt-1 text-xs text-danger">{errors.companyName.message}</p>}
              </div>

              <div>
                <label htmlFor="adminUserName" className="mb-1.5 block text-sm font-medium text-ink-muted">
                  Your username
                </label>
                <input
                  id="adminUserName"
                  type="text"
                  autoComplete="username"
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  aria-invalid={Boolean(errors.adminUserName)}
                  {...register('adminUserName')}
                />
                {errors.adminUserName && (
                  <p className="mt-1 text-xs text-danger">{errors.adminUserName.message}</p>
                )}
              </div>

              <div>
                <label htmlFor="adminEmail" className="mb-1.5 block text-sm font-medium text-ink-muted">
                  Your email
                </label>
                <input
                  id="adminEmail"
                  type="email"
                  autoComplete="email"
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  aria-invalid={Boolean(errors.adminEmail)}
                  {...register('adminEmail')}
                />
                {errors.adminEmail && <p className="mt-1 text-xs text-danger">{errors.adminEmail.message}</p>}
              </div>

              <div>
                <label htmlFor="adminPassword" className="mb-1.5 block text-sm font-medium text-ink-muted">
                  Password
                </label>
                <input
                  id="adminPassword"
                  type="password"
                  autoComplete="new-password"
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  aria-invalid={Boolean(errors.adminPassword)}
                  {...register('adminPassword')}
                />
                {errors.adminPassword && (
                  <p className="mt-1 text-xs text-danger">{errors.adminPassword.message}</p>
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
                {isSubmitting ? 'Registering…' : 'Register company'}
              </button>
            </form>
          )}
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
