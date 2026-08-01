import { useState } from 'react'
import { usePlatformSettings } from '../hooks/usePlatformSettings'
import { platformSettingsRepository } from '../api'
import type { PlatformSettings } from '../api'
import { AppError } from '@/app/shared/models/appError'

function ToggleRow({
  label,
  description,
  checked,
  onChange,
}: {
  label: string
  description: string
  checked: boolean
  onChange: (checked: boolean) => void
}) {
  return (
    <label className="flex cursor-pointer items-start justify-between gap-4 rounded-lg border border-hairline bg-surface-1 p-5">
      <div>
        <p className="text-sm font-medium text-ink">{label}</p>
        <p className="mt-0.5 text-sm text-ink-subtle">{description}</p>
      </div>
      <input
        type="checkbox"
        role="switch"
        aria-checked={checked}
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="mt-1 h-5 w-5 shrink-0 accent-primary"
      />
    </label>
  )
}

export function PlatformSettingsPage() {
  const { settings, isLoading, error, refresh } = usePlatformSettings()
  const [draft, setDraft] = useState<PlatformSettings | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  // Adopts freshly-loaded/refreshed settings into the editable draft. Done during render (React's
  // documented pattern for state derived from a changing prop) rather than in an effect, so it
  // doesn't trigger an extra render pass — see
  // https://react.dev/learn/you-might-not-need-an-effect#adjusting-some-state-when-a-prop-changes.
  const [prevSettings, setPrevSettings] = useState(settings)
  if (settings !== prevSettings) {
    setPrevSettings(settings)
    if (settings) setDraft(settings)
  }

  async function handleSave() {
    if (!draft) return
    setIsSaving(true)
    setSaveError(null)
    setSaved(false)
    try {
      await platformSettingsRepository.update(draft)
      setSaved(true)
      refresh()
    } catch (err) {
      setSaveError(err instanceof AppError ? err.message : 'Failed to save platform settings.')
    } finally {
      setIsSaving(false)
    }
  }

  if (isLoading) {
    return <div className="h-40 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!draft) return null

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Platform Settings</h1>
        <p className="text-sm text-ink-subtle">Controls for public company self-registration</p>
      </div>

      <div className="space-y-3">
        <ToggleRow
          label="Public registration enabled"
          description="Whether the public company registration form is shown at all."
          checked={draft.isPublicRegistrationEnabled}
          onChange={(checked) => {
            setDraft({ ...draft, isPublicRegistrationEnabled: checked })
            setSaved(false)
          }}
        />
        <ToggleRow
          label="Require approval for new companies"
          description="New registrations land in Pending Approval and can't sign in until a Super Admin approves them."
          checked={draft.requireApprovalForNewCompanies}
          onChange={(checked) => {
            setDraft({ ...draft, requireApprovalForNewCompanies: checked })
            setSaved(false)
          }}
        />
      </div>

      {saveError && (
        <p role="alert" className="text-sm text-danger">
          {saveError}
        </p>
      )}
      {saved && <p className="text-sm text-success">Platform settings updated.</p>}

      <button
        type="button"
        disabled={isSaving}
        onClick={() => void handleSave()}
        className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isSaving ? 'Saving…' : 'Save settings'}
      </button>
    </div>
  )
}
