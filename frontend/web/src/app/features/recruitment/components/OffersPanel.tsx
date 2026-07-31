import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useOffers } from '../hooks/useOffers'
import { recruitmentRepository, type Offer } from '../api'
import { createOfferFormSchema, type CreateOfferFormInput, type CreateOfferFormValues } from '../types/recruitmentSchema'
import { OfferStatusBadge } from './OfferStatusBadge'
import { useDesignations } from '@/app/features/designations/hooks/useDesignations'
import { useDepartments } from '@/app/features/departments/hooks/useDepartments'
import { AppError } from '@/app/shared/models/appError'
import { Modal } from '@/app/shared/components/Modal'

const inputClass =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClass = 'mb-1 block text-sm font-medium text-ink-muted'

export function OffersPanel({ candidateId, canManage }: { candidateId: string; canManage: boolean }) {
  const { offers, isLoading, error, refresh } = useOffers(candidateId)
  const { designations } = useDesignations()
  const { departments } = useDepartments()
  const [isCreating, setIsCreating] = useState(false)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [panelError, setPanelError] = useState<string | null>(null)

  const createForm = useForm<CreateOfferFormInput, unknown, CreateOfferFormValues>({
    resolver: zodResolver(createOfferFormSchema),
    defaultValues: { designationId: '', departmentId: '', offeredSalary: 0, joiningDate: '', expiresAtUtc: '', notes: '' },
  })

  async function onCreate(values: CreateOfferFormValues) {
    setPanelError(null)
    try {
      await recruitmentRepository.createOffer(candidateId, {
        designationId: values.designationId,
        departmentId: values.departmentId || undefined,
        offeredSalary: values.offeredSalary,
        joiningDate: values.joiningDate,
        expiresAtUtc: values.expiresAtUtc ? new Date(values.expiresAtUtc).toISOString() : undefined,
        notes: values.notes?.trim() || undefined,
      })
      createForm.reset()
      setIsCreating(false)
      refresh()
    } catch (err) {
      setPanelError(err instanceof AppError ? err.message : 'Failed to create offer.')
    }
  }

  async function runAction(offer: Offer, action: () => Promise<void>, failureMessage: string) {
    setBusyId(offer.id)
    setPanelError(null)
    try {
      await action()
      refresh()
    } catch (err) {
      setPanelError(err instanceof AppError ? err.message : failureMessage)
    } finally {
      setBusyId(null)
    }
  }

  async function handleDownload(offer: Offer) {
    setPanelError(null)
    try {
      const { blob, fileName } = await recruitmentRepository.downloadOffer(offer.id)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (err) {
      setPanelError(err instanceof AppError ? err.message : 'Failed to download offer letter.')
    }
  }

  return (
    <div className="rounded-lg border border-hairline bg-surface-1">
      <div className="flex items-center justify-between border-b border-hairline p-6">
        <h2 className="text-[18px] font-medium text-ink">Offers</h2>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsCreating((open) => !open)}
            className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
          >
            {isCreating ? 'Cancel' : 'Create offer'}
          </button>
        )}
      </div>

      {(error || panelError) && <p className="px-6 pt-4 text-sm text-danger">{error ?? panelError}</p>}

      <div className="p-6">
        {!isLoading && offers.length === 0 && <p className="text-sm text-ink-subtle">No offers created yet.</p>}
        <ul className="space-y-3">
          {offers.map((offer) => (
            <li key={offer.id} className="rounded-md border border-hairline p-3">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-ink">{offer.offerNumber}</p>
                  <p className="text-xs text-ink-subtle">
                    {offer.designationName ?? '—'} ·{' '}
                    {offer.offeredSalary.toLocaleString(undefined, { style: 'currency', currency: 'USD' })} · Joins{' '}
                    {new Date(offer.joiningDate).toLocaleDateString()}
                  </p>
                </div>
                <OfferStatusBadge status={offer.status} />
              </div>

              <div className="mt-2 flex flex-wrap gap-3">
                {canManage && offer.status === 'Draft' && (
                  <button
                    type="button"
                    disabled={busyId === offer.id}
                    onClick={() => void runAction(offer, () => recruitmentRepository.sendOffer(offer.id), 'Failed to send offer.')}
                    className="text-xs font-medium text-primary-hover hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Send
                  </button>
                )}
                {canManage && offer.status === 'Sent' && (
                  <>
                    <button
                      type="button"
                      disabled={busyId === offer.id}
                      onClick={() => void runAction(offer, () => recruitmentRepository.acceptOffer(offer.id), 'Failed to accept offer.')}
                      className="text-xs font-medium text-success hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      Accept
                    </button>
                    <button
                      type="button"
                      disabled={busyId === offer.id}
                      onClick={() => void runAction(offer, () => recruitmentRepository.rejectOffer(offer.id), 'Failed to reject offer.')}
                      className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      Reject
                    </button>
                  </>
                )}
                {canManage && (offer.status === 'Draft' || offer.status === 'Sent') && (
                  <button
                    type="button"
                    disabled={busyId === offer.id}
                    onClick={() => void runAction(offer, () => recruitmentRepository.withdrawOffer(offer.id), 'Failed to withdraw offer.')}
                    className="text-xs font-medium text-ink hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Withdraw
                  </button>
                )}
                {offer.hasDocument && (
                  <button
                    type="button"
                    onClick={() => void handleDownload(offer)}
                    className="text-xs font-medium text-primary-hover hover:underline"
                  >
                    Download letter
                  </button>
                )}
              </div>
            </li>
          ))}
        </ul>
      </div>

      <Modal isOpen={isCreating} onClose={() => setIsCreating(false)} title="Create offer">
        <form onSubmit={createForm.handleSubmit(onCreate)} noValidate className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="offer-designation" className={labelClass}>
                Designation
              </label>
              <select id="offer-designation" className={inputClass} {...createForm.register('designationId')}>
                <option value="">Select designation…</option>
                {designations.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.name}
                  </option>
                ))}
              </select>
              {createForm.formState.errors.designationId && (
                <p className="mt-1 text-xs text-danger">{createForm.formState.errors.designationId.message}</p>
              )}
            </div>
            <div>
              <label htmlFor="offer-department" className={labelClass}>
                Department <span className="text-ink-tertiary">(optional)</span>
              </label>
              <select id="offer-department" className={inputClass} {...createForm.register('departmentId')}>
                <option value="">Unspecified</option>
                {departments.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.name}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="offer-salary" className={labelClass}>
                Offered salary
              </label>
              <input id="offer-salary" type="number" step="0.01" min="0" className={inputClass} {...createForm.register('offeredSalary')} />
              {createForm.formState.errors.offeredSalary && (
                <p className="mt-1 text-xs text-danger">{createForm.formState.errors.offeredSalary.message}</p>
              )}
            </div>
            <div>
              <label htmlFor="offer-joining" className={labelClass}>
                Joining date
              </label>
              <input id="offer-joining" type="date" className={inputClass} {...createForm.register('joiningDate')} />
              {createForm.formState.errors.joiningDate && (
                <p className="mt-1 text-xs text-danger">{createForm.formState.errors.joiningDate.message}</p>
              )}
            </div>
          </div>
          <div>
            <label htmlFor="offer-expires" className={labelClass}>
              Expires on <span className="text-ink-tertiary">(optional)</span>
            </label>
            <input id="offer-expires" type="date" className={inputClass} {...createForm.register('expiresAtUtc')} />
          </div>
          <div>
            <label htmlFor="offer-notes" className={labelClass}>
              Notes
            </label>
            <textarea id="offer-notes" rows={2} className={inputClass} {...createForm.register('notes')} />
          </div>
          {panelError && (
            <p role="alert" className="text-sm text-danger">
              {panelError}
            </p>
          )}
          <button
            type="submit"
            disabled={createForm.formState.isSubmitting}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
          >
            {createForm.formState.isSubmitting ? 'Creating…' : 'Create offer'}
          </button>
        </form>
      </Modal>
    </div>
  )
}
