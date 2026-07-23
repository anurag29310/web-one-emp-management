import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Modal } from '@/app/shared/components/Modal'
import { AppError } from '@/app/shared/models/appError'
import { authRepository } from '../api'
import { passwordConfirmSchema, type PasswordConfirmFormValues } from '../types/authSchemas'
import { RecoveryCodesReveal } from './RecoveryCodesReveal'

interface RegenerateRecoveryCodesModalProps {
  onClose: () => void
  onCompleted: () => void
}

// Mounted by the caller only while this action is in progress (see ProfilePage), so state below
// starts fresh on every open with no reset-on-open effect needed.
export function RegenerateRecoveryCodesModal({ onClose, onCompleted }: RegenerateRecoveryCodesModalProps) {
  const [codes, setCodes] = useState<string[] | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<PasswordConfirmFormValues>({ resolver: zodResolver(passwordConfirmSchema) })

  async function onSubmit(values: PasswordConfirmFormValues) {
    try {
      const result = await authRepository.regenerateMfaRecoveryCodes({ password: values.password })
      setCodes(result.recoveryCodes)
    } catch (err) {
      setError('password', {
        message: err instanceof AppError ? err.message : 'Incorrect password.',
      })
    }
  }

  function handleDone() {
    onCompleted()
    onClose()
  }

  // Once the new codes are on screen they must be acknowledged explicitly, same as first-time
  // enrollment — closing via backdrop/Escape/X is disabled at that point.
  const handleClose = codes ? () => {} : onClose

  return (
    <Modal isOpen onClose={handleClose} title="Regenerate recovery codes">
      {codes ? (
        <RecoveryCodesReveal codes={codes} onDone={handleDone} />
      ) : (
        <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
          <p className="text-sm text-ink-subtle">
            This invalidates all of your existing recovery codes — used or not — and issues 10 new
            ones. Confirm your password to continue.
          </p>
          <div>
            <label htmlFor="regen-codes-password" className="mb-1.5 block text-sm font-medium text-ink-muted">
              Password
            </label>
            <input
              id="regen-codes-password"
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
              className="flex-1 rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? 'Regenerating…' : 'Regenerate codes'}
            </button>
          </div>
        </form>
      )}
    </Modal>
  )
}
