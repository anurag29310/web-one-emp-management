import { useState } from 'react'
import { Modal } from '@/app/shared/components/Modal'

interface ReasonPromptModalProps {
  isOpen: boolean
  title: string
  confirmLabel: string
  isBusy: boolean
  onClose: () => void
  onConfirm: (reason: string | undefined) => void
}

export function ReasonPromptModal({ isOpen, title, confirmLabel, isBusy, onClose, onConfirm }: ReasonPromptModalProps) {
  const [reason, setReason] = useState('')

  function handleClose() {
    setReason('')
    onClose()
  }

  function handleConfirm() {
    onConfirm(reason.trim() || undefined)
    setReason('')
  }

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title={title}>
      <div className="space-y-3">
        <div>
          <label htmlFor="reason-prompt-reason" className="mb-1.5 block text-sm font-medium text-ink-muted">
            Reason <span className="text-ink-tertiary">(optional)</span>
          </label>
          <textarea
            id="reason-prompt-reason"
            rows={3}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          />
        </div>
        <button
          type="button"
          disabled={isBusy}
          onClick={handleConfirm}
          className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
        >
          {confirmLabel}
        </button>
      </div>
    </Modal>
  )
}
