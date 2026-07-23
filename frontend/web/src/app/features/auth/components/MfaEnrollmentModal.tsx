import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { QRCodeSVG } from 'qrcode.react'
import { Modal } from '@/app/shared/components/Modal'
import { AppError } from '@/app/shared/models/appError'
import { authRepository, type MfaSetupInfo } from '../api'
import { mfaEnableCodeSchema, type MfaEnableCodeFormValues } from '../types/authSchemas'
import { RecoveryCodesReveal } from './RecoveryCodesReveal'

type Step = 'loading' | 'scan' | 'confirm' | 'reveal' | 'error'

interface MfaEnrollmentModalProps {
  onClose: () => void
  /** Called once the user has acknowledged the recovery codes — the caller should re-fetch the current user. */
  onCompleted: () => void
}

// Mounted by the caller only while enrollment is in progress (see ProfilePage) rather than kept
// alive and toggled via an `isOpen` prop — that way every field below starts fresh on open with
// no reset-on-open effect needed, and the setupMfa() call only ever runs once per mount.
export function MfaEnrollmentModal({ onClose, onCompleted }: MfaEnrollmentModalProps) {
  const [step, setStep] = useState<Step>('loading')
  const [setupInfo, setSetupInfo] = useState<MfaSetupInfo | null>(null)
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([])
  const [loadError, setLoadError] = useState<string | null>(null)
  const [confirmError, setConfirmError] = useState<string | null>(null)
  const [copiedKey, setCopiedKey] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<MfaEnableCodeFormValues>({ resolver: zodResolver(mfaEnableCodeSchema) })

  useEffect(() => {
    let cancelled = false
    authRepository
      .setupMfa()
      .then((info) => {
        if (cancelled) return
        setSetupInfo(info)
        setStep('scan')
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setLoadError(err instanceof AppError ? err.message : 'Failed to start MFA setup.')
        setStep('error')
      })
    return () => {
      cancelled = true
    }
  }, [])

  async function handleCopyKey() {
    if (!setupInfo) return
    try {
      await navigator.clipboard.writeText(setupInfo.manualEntryKey)
      setCopiedKey(true)
      setTimeout(() => setCopiedKey(false), 2000)
    } catch {
      // Non-fatal — the key is still visible and selectable as text.
    }
  }

  async function onConfirm(values: MfaEnableCodeFormValues) {
    setConfirmError(null)
    try {
      const result = await authRepository.enableMfa(values.code.trim())
      setRecoveryCodes(result.recoveryCodes)
      setStep('reveal')
    } catch (err) {
      setConfirmError(err instanceof AppError ? err.message : 'Invalid verification code.')
    }
  }

  function handleDone() {
    onCompleted()
    onClose()
  }

  // The recovery codes must be acknowledged explicitly — closing via backdrop/Escape/X is
  // disabled once they're on screen.
  const handleClose = step === 'reveal' ? () => {} : onClose

  return (
    <Modal isOpen onClose={handleClose} title="Enable two-factor authentication">
      {step === 'loading' && (
        <div className="flex h-32 items-center justify-center text-sm text-ink-subtle">
          Preparing your enrollment…
        </div>
      )}

      {step === 'error' && (
        <div className="space-y-3">
          <p role="alert" className="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger">
            {loadError}
          </p>
          <button
            type="button"
            onClick={onClose}
            className="w-full rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
          >
            Close
          </button>
        </div>
      )}

      {step === 'scan' && setupInfo && (
        <div className="space-y-4">
          <p className="text-sm text-ink-subtle">
            Scan this QR code with your authenticator app (Google Authenticator, Authy, 1Password,
            etc.), or enter the key manually.
          </p>
          <div className="flex justify-center rounded-md bg-white p-4">
            <QRCodeSVG value={setupInfo.otpAuthUri} size={180} marginSize={2} />
          </div>
          <div>
            <p className="mb-1 text-xs font-medium uppercase tracking-[0.4px] text-ink-subtle">
              Manual entry key
            </p>
            <div className="flex items-center gap-2">
              <code className="flex-1 truncate rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink">
                {setupInfo.manualEntryKey}
              </code>
              <button
                type="button"
                onClick={() => void handleCopyKey()}
                className="shrink-0 rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
              >
                {copiedKey ? 'Copied!' : 'Copy'}
              </button>
            </div>
          </div>
          <button
            type="button"
            onClick={() => setStep('confirm')}
            className="w-full rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            I&apos;ve added it — continue
          </button>
        </div>
      )}

      {step === 'confirm' && (
        <form onSubmit={handleSubmit(onConfirm)} noValidate className="space-y-4">
          <p className="text-sm text-ink-subtle">
            Enter the 6-digit code currently shown in your authenticator app to confirm setup.
          </p>
          <div>
            <label htmlFor="enable-code" className="mb-1.5 block text-sm font-medium text-ink-muted">
              Verification code
            </label>
            <input
              id="enable-code"
              type="text"
              inputMode="numeric"
              autoFocus
              placeholder="123456"
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink transition outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              aria-invalid={Boolean(errors.code)}
              {...register('code')}
            />
            {errors.code && <p className="mt-1 text-xs text-danger">{errors.code.message}</p>}
          </div>

          {confirmError && (
            <p role="alert" className="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger">
              {confirmError}
            </p>
          )}

          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setStep('scan')}
              className="flex-1 rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
            >
              Back
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="flex-1 rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? 'Verifying…' : 'Enable MFA'}
            </button>
          </div>
        </form>
      )}

      {step === 'reveal' && <RecoveryCodesReveal codes={recoveryCodes} onDone={handleDone} />}
    </Modal>
  )
}
