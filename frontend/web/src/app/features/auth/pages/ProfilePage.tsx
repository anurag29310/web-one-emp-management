import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useCurrentUser } from '../hooks/useCurrentUser'
import { authRepository } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { changePasswordSchema, type ChangePasswordFormValues } from '../types/authSchemas'
import { PASSWORD_POLICY_RULES } from '../types/passwordPolicy'
import { Avatar } from '@/app/shared/components/Avatar'
import { MfaEnrollmentModal } from '../components/MfaEnrollmentModal'
import { DisableMfaModal } from '../components/DisableMfaModal'
import { RegenerateRecoveryCodesModal } from '../components/RegenerateRecoveryCodesModal'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 text-sm text-ink">{value}</p>
    </div>
  )
}

function ChangePasswordForm() {
  const [formError, setFormError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordFormValues>({ resolver: zodResolver(changePasswordSchema) })

  async function onSubmit(values: ChangePasswordFormValues) {
    setFormError(null)
    setSuccessMessage(null)
    try {
      await authRepository.changePassword({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      })
      setSuccessMessage('Your password has been changed.')
      reset()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Unable to change your password.')
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
      <div>
        <label htmlFor="currentPassword" className="mb-1.5 block text-sm font-medium text-ink-muted">
          Current password
        </label>
        <input
          id="currentPassword"
          type="password"
          autoComplete="current-password"
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          aria-invalid={Boolean(errors.currentPassword)}
          {...register('currentPassword')}
        />
        {errors.currentPassword && (
          <p className="mt-1 text-xs text-danger">{errors.currentPassword.message}</p>
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
        {errors.newPassword && <p className="mt-1 text-xs text-danger">{errors.newPassword.message}</p>}
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
      {successMessage && (
        <p role="status" className="rounded-md bg-success/10 px-3 py-2 text-sm text-success">
          {successMessage}
        </p>
      )}

      <button
        type="submit"
        disabled={isSubmitting}
        className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isSubmitting ? 'Changing…' : 'Change password'}
      </button>
    </form>
  )
}

export function ProfilePage() {
  const { currentUser, isLoading, error, refresh } = useCurrentUser()
  const [isEnrollingMfa, setIsEnrollingMfa] = useState(false)
  const [isDisablingMfa, setIsDisablingMfa] = useState(false)
  const [isRegeneratingCodes, setIsRegeneratingCodes] = useState(false)

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!currentUser) return null

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Your profile</h1>
        <p className="text-sm text-ink-subtle">Manage your account, password, and two-factor authentication.</p>
      </div>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center gap-4 border-b border-hairline p-6">
          <Avatar name={currentUser.userName} size="lg" />
          <div>
            <h2 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
              {currentUser.userName}
            </h2>
            <p className="text-sm text-ink-subtle">{currentUser.email}</p>
          </div>
        </div>
        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Role" value={currentUser.role ?? 'No role assigned'} />
          <Field label="Account status" value={currentUser.isActive ? 'Active' : 'Inactive'} />
          <Field label="Two-factor authentication" value={currentUser.isMfaEnabled ? 'Enabled' : 'Disabled'} />
        </div>
      </div>

      <div className="rounded-lg border border-hairline bg-surface-1 p-6">
        <h2 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">Change password</h2>
        <p className="mb-4 mt-1 text-sm text-ink-subtle">Choose a strong password you don&apos;t use elsewhere.</p>
        <ChangePasswordForm />
      </div>

      <div className="rounded-lg border border-hairline bg-surface-1 p-6">
        <h2 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
          Two-factor authentication
        </h2>

        {currentUser.isMfaEnabled ? (
          <>
            <p className="mb-4 mt-1 text-sm text-ink-subtle">
              MFA is protecting your account. You&apos;ll be asked for a code from your authenticator
              app (or a recovery code) every time you sign in.
            </p>
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => setIsRegeneratingCodes(true)}
                className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
              >
                Regenerate recovery codes
              </button>
              <button
                type="button"
                onClick={() => setIsDisablingMfa(true)}
                className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10"
              >
                Disable MFA
              </button>
            </div>
          </>
        ) : (
          <>
            <p className="mb-4 mt-1 text-sm text-ink-subtle">
              Add an extra layer of security to your account by requiring a code from an
              authenticator app when you sign in.
            </p>
            <button
              type="button"
              onClick={() => setIsEnrollingMfa(true)}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
            >
              Enable MFA
            </button>
          </>
        )}
      </div>

      {isEnrollingMfa && (
        <MfaEnrollmentModal onClose={() => setIsEnrollingMfa(false)} onCompleted={refresh} />
      )}
      {isDisablingMfa && (
        <DisableMfaModal onClose={() => setIsDisablingMfa(false)} onDisabled={refresh} />
      )}
      {isRegeneratingCodes && (
        <RegenerateRecoveryCodesModal
          onClose={() => setIsRegeneratingCodes(false)}
          onCompleted={refresh}
        />
      )}
    </div>
  )
}
