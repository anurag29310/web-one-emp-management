import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Modal } from '@/app/shared/components/Modal'
import { AppError } from '@/app/shared/models/appError'
import { authRepository } from '../api'
import { passwordConfirmSchema, type PasswordConfirmFormValues } from '../types/authSchemas'

interface DisableMfaModalProps {
  onClose: () => void
  /** Called after MFA has been turned off — the caller should re-fetch the current user. */
  onDisabled: () => void
}

// Mounted by the caller only while this action is in progress (see ProfilePage), so the form
// starts fresh on every open with no reset-on-open effect needed.
export function DisableMfaModal({ onClose, onDisabled }: DisableMfaModalProps) {
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<PasswordConfirmFormValues>({ resolver: zodResolver(passwordConfirmSchema) })

  async function onSubmit(values: PasswordConfirmFormValues) {
    try {
      await authRepository.disableMfa({ password: values.password })
      onDisabled()
      onClose()
    } catch (err) {
      setError('password', {
        message: err instanceof AppError ? err.message : 'Incorrect password.',
      })
    }
  }

  return (
    <Modal isOpen onClose={onClose} title="Disable two-factor authentication">
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
        <p className="text-sm text-ink-subtle">
          This turns off MFA for your account and permanently invalidates your recovery codes.
          Confirm your password to continue.
        </p>
        <div>
          <label htmlFor="disable-mfa-password" className="mb-1.5 block text-sm font-medium text-ink-muted">
            Password
          </label>
          <input
            id="disable-mfa-password"
            type="password"
            autoComplete="current-password"
            autoFocus
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            aria-invalid={Boolean(errors.password)}
            {...register('password')}
          />
          {errors.password && <p className="mt-1 text-xs text-danger">{errors.password.message}</p>}
        </div>

        <div className="flex gap-2">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="flex-1 rounded-md bg-danger px-3 py-2 text-sm font-medium text-white transition hover:bg-danger/90 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? 'Disabling…' : 'Disable MFA'}
          </button>
        </div>
      </form>
    </Modal>
  )
}
