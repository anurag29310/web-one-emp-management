import { useState } from 'react'

interface RecoveryCodesRevealProps {
  codes: string[]
  onDone: () => void
}

/**
 * Shown exactly once after POST /auth/mfa/enable or
 * POST /auth/mfa/recovery-codes/regenerate — the codes are never retrievable
 * again after this render, so this makes that unmistakable and offers a
 * one-click copy before the user moves on.
 */
export function RecoveryCodesReveal({ codes, onDone }: RecoveryCodesRevealProps) {
  const [copied, setCopied] = useState(false)

  async function handleCopyAll() {
    try {
      await navigator.clipboard.writeText(codes.join('\n'))
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      // Clipboard access can be denied by the browser — the codes remain visible/selectable, so
      // there's no hard failure to surface here.
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-md bg-warning/10 px-3 py-2.5 text-sm text-warning">
        <p className="font-medium">Save these recovery codes now.</p>
        <p className="mt-0.5 text-xs text-warning/90">
          Each code can be used once to sign in if you lose access to your authenticator app. They
          will not be shown again.
        </p>
      </div>

      <div className="grid grid-cols-2 gap-2 rounded-md border border-hairline bg-surface-2 p-4 font-mono text-sm text-ink">
        {codes.map((code) => (
          <span key={code}>{code}</span>
        ))}
      </div>

      <div className="flex items-center gap-2">
        <button
          type="button"
          onClick={() => void handleCopyAll()}
          className="flex-1 rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
        >
          {copied ? 'Copied!' : 'Copy all codes'}
        </button>
        <button
          type="button"
          onClick={onDone}
          className="flex-1 rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
        >
          I&apos;ve saved these
        </button>
      </div>
    </div>
  )
}
