import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useOfficeLocation } from '../hooks/useOfficeLocation'
import { officeLocationRepository } from '../api'
import { officeLocationFormSchema, type OfficeLocationFormValues } from '../types/officeLocationSchema'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

const inputClasses =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClasses = 'mb-1 block text-sm font-medium text-ink-muted'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 text-sm text-ink">{value}</p>
    </div>
  )
}

export function OfficeLocationDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { officeLocation, isLoading, error, refresh } = useOfficeLocation(id)
  const { user } = useAuth()
  const navigate = useNavigate()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const [isEditing, setIsEditing] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<OfficeLocationFormValues>({ resolver: zodResolver(officeLocationFormSchema) })

  useEffect(() => {
    if (officeLocation) {
      reset({
        name: officeLocation.name,
        code: officeLocation.code,
        addressLine1: officeLocation.addressLine1 ?? '',
        addressLine2: officeLocation.addressLine2 ?? '',
        city: officeLocation.city,
        state: officeLocation.state ?? '',
        country: officeLocation.country,
        timeZoneId: officeLocation.timeZoneId,
      })
    }
  }, [officeLocation, reset])

  async function onSave(values: OfficeLocationFormValues) {
    if (!officeLocation) return
    setFormError(null)
    try {
      await officeLocationRepository.update({
        id: officeLocation.id,
        name: values.name.trim(),
        code: values.code.trim(),
        addressLine1: values.addressLine1?.trim() || undefined,
        addressLine2: values.addressLine2?.trim() || undefined,
        city: values.city.trim(),
        state: values.state?.trim() || undefined,
        country: values.country.trim(),
        timeZoneId: values.timeZoneId.trim(),
      })
      setIsEditing(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update office location.')
    }
  }

  async function handleDelete() {
    if (!officeLocation) return
    setIsDeleting(true)
    try {
      await officeLocationRepository.remove(officeLocation.id)
      navigate('/office-locations')
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete office location.')
      setIsDeleting(false)
    }
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!officeLocation) return null

  const address = [officeLocation.addressLine1, officeLocation.addressLine2, officeLocation.city, officeLocation.state, officeLocation.country]
    .filter(Boolean)
    .join(', ')

  return (
    <div className="space-y-4">
      <Link
        to="/office-locations"
        className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover"
      >
        ← Back to office locations
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
              {officeLocation.name}
            </h1>
            <p className="text-sm text-ink-subtle">{officeLocation.code}</p>
          </div>
          {canManage && (
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => setIsEditing((editing) => !editing)}
                className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
              >
                {isEditing ? 'Cancel' : 'Edit'}
              </button>
              <button
                type="button"
                disabled={isDeleting}
                onClick={() => void handleDelete()}
                className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isDeleting ? 'Deleting…' : 'Delete'}
              </button>
            </div>
          )}
        </div>

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Address" value={address || '—'} />
          <Field label="Time zone" value={officeLocation.timeZoneId} />
          <Field label="Created" value={new Date(officeLocation.createdAtUtc).toLocaleDateString()} />
          <Field
            label="Last updated"
            value={officeLocation.updatedAtUtc ? new Date(officeLocation.updatedAtUtc).toLocaleDateString() : '—'}
          />
        </div>
        {!isEditing && formError && (
          <p role="alert" className="px-6 pb-6 text-sm text-danger">
            {formError}
          </p>
        )}
      </div>

      {canManage && (
        <Modal isOpen={isEditing} onClose={() => setIsEditing(false)} title="Edit office location">
          <form onSubmit={handleSubmit(onSave)} noValidate className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-loc-name" className={labelClasses}>
                  Name
                </label>
                <input
                  id="edit-loc-name"
                  aria-invalid={Boolean(errors.name)}
                  className={inputClasses}
                  {...register('name')}
                />
                {errors.name && <p className="mt-1 text-xs text-danger">{errors.name.message}</p>}
              </div>
              <div>
                <label htmlFor="edit-loc-code" className={labelClasses}>
                  Code
                </label>
                <input
                  id="edit-loc-code"
                  aria-invalid={Boolean(errors.code)}
                  className={inputClasses}
                  {...register('code')}
                />
                {errors.code && <p className="mt-1 text-xs text-danger">{errors.code.message}</p>}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-loc-addressLine1" className={labelClasses}>
                  Address line 1
                </label>
                <input id="edit-loc-addressLine1" className={inputClasses} {...register('addressLine1')} />
              </div>
              <div>
                <label htmlFor="edit-loc-addressLine2" className={labelClasses}>
                  Address line 2
                </label>
                <input id="edit-loc-addressLine2" className={inputClasses} {...register('addressLine2')} />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-loc-city" className={labelClasses}>
                  City
                </label>
                <input
                  id="edit-loc-city"
                  aria-invalid={Boolean(errors.city)}
                  className={inputClasses}
                  {...register('city')}
                />
                {errors.city && <p className="mt-1 text-xs text-danger">{errors.city.message}</p>}
              </div>
              <div>
                <label htmlFor="edit-loc-state" className={labelClasses}>
                  State
                </label>
                <input id="edit-loc-state" className={inputClasses} {...register('state')} />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-loc-country" className={labelClasses}>
                  Country
                </label>
                <input
                  id="edit-loc-country"
                  aria-invalid={Boolean(errors.country)}
                  className={inputClasses}
                  {...register('country')}
                />
                {errors.country && <p className="mt-1 text-xs text-danger">{errors.country.message}</p>}
              </div>
              <div>
                <label htmlFor="edit-loc-timeZoneId" className={labelClasses}>
                  Time zone (IANA id)
                </label>
                <input
                  id="edit-loc-timeZoneId"
                  aria-invalid={Boolean(errors.timeZoneId)}
                  className={inputClasses}
                  {...register('timeZoneId')}
                />
                {errors.timeZoneId && <p className="mt-1 text-xs text-danger">{errors.timeZoneId.message}</p>}
              </div>
            </div>

            {formError && (
              <p role="alert" className="text-sm text-danger">
                {formError}
              </p>
            )}
            <button
              type="submit"
              disabled={isSubmitting}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? 'Saving…' : 'Save changes'}
            </button>
          </form>
        </Modal>
      )}
    </div>
  )
}
