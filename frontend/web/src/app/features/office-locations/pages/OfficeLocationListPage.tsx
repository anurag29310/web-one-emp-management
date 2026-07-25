import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useOfficeLocations } from '../hooks/useOfficeLocations'
import { officeLocationRepository, type OfficeLocation } from '../api'
import { officeLocationFormSchema, type OfficeLocationFormValues } from '../types/officeLocationSchema'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

const inputClasses =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClasses = 'mb-1 block text-sm font-medium text-ink-muted'

export function OfficeLocationListPage() {
  const { officeLocations, isLoading, error, refresh } = useOfficeLocations()
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<OfficeLocationFormValues>({
    resolver: zodResolver(officeLocationFormSchema),
    defaultValues: {
      name: '',
      code: '',
      addressLine1: '',
      addressLine2: '',
      city: '',
      state: '',
      country: '',
      timeZoneId: '',
    },
  })

  async function onCreate(values: OfficeLocationFormValues) {
    setFormError(null)
    try {
      await officeLocationRepository.create({
        name: values.name.trim(),
        code: values.code.trim(),
        addressLine1: values.addressLine1?.trim() || undefined,
        addressLine2: values.addressLine2?.trim() || undefined,
        city: values.city.trim(),
        state: values.state?.trim() || undefined,
        country: values.country.trim(),
        timeZoneId: values.timeZoneId.trim(),
      })
      reset()
      setIsFormOpen(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to create office location.')
    }
  }

  async function handleDelete(location: OfficeLocation) {
    setPendingDeleteId(location.id)
    try {
      await officeLocationRepository.remove(location.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete office location.')
    } finally {
      setPendingDeleteId(null)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Office Locations</h1>
          <p className="text-sm text-ink-subtle">{officeLocations.length} locations</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsFormOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isFormOpen ? 'Cancel' : 'New office location'}
          </button>
        )}
      </div>

      {canManage && (
        <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="New office location">
          <form onSubmit={handleSubmit(onCreate)} noValidate className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="loc-name" className={labelClasses}>
                  Name
                </label>
                <input
                  id="loc-name"
                  aria-invalid={Boolean(errors.name)}
                  className={inputClasses}
                  {...register('name')}
                />
                {errors.name && <p className="mt-1 text-xs text-danger">{errors.name.message}</p>}
              </div>
              <div>
                <label htmlFor="loc-code" className={labelClasses}>
                  Code
                </label>
                <input
                  id="loc-code"
                  aria-invalid={Boolean(errors.code)}
                  className={inputClasses}
                  {...register('code')}
                />
                {errors.code && <p className="mt-1 text-xs text-danger">{errors.code.message}</p>}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="loc-addressLine1" className={labelClasses}>
                  Address line 1
                </label>
                <input id="loc-addressLine1" className={inputClasses} {...register('addressLine1')} />
              </div>
              <div>
                <label htmlFor="loc-addressLine2" className={labelClasses}>
                  Address line 2
                </label>
                <input id="loc-addressLine2" className={inputClasses} {...register('addressLine2')} />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="loc-city" className={labelClasses}>
                  City
                </label>
                <input
                  id="loc-city"
                  aria-invalid={Boolean(errors.city)}
                  className={inputClasses}
                  {...register('city')}
                />
                {errors.city && <p className="mt-1 text-xs text-danger">{errors.city.message}</p>}
              </div>
              <div>
                <label htmlFor="loc-state" className={labelClasses}>
                  State
                </label>
                <input id="loc-state" className={inputClasses} {...register('state')} />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="loc-country" className={labelClasses}>
                  Country
                </label>
                <input
                  id="loc-country"
                  aria-invalid={Boolean(errors.country)}
                  className={inputClasses}
                  {...register('country')}
                />
                {errors.country && <p className="mt-1 text-xs text-danger">{errors.country.message}</p>}
              </div>
              <div>
                <label htmlFor="loc-timeZoneId" className={labelClasses}>
                  Time zone (IANA id)
                </label>
                <input
                  id="loc-timeZoneId"
                  placeholder="e.g. America/New_York"
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
              {isSubmitting ? 'Saving…' : 'Create office location'}
            </button>
          </form>
        </Modal>
      )}

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Code</th>
              <th className="px-4 py-3">City</th>
              <th className="px-4 py-3">Country</th>
              <th className="px-4 py-3">Time zone</th>
              {canManage && <th className="px-4 py-3" />}
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={canManage ? 6 : 5}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && officeLocations.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={canManage ? 6 : 5}>
                  No office locations yet.
                </td>
              </tr>
            )}

            {!isLoading &&
              officeLocations.map((location) => (
                <tr key={location.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link
                      to={`/office-locations/${location.id}`}
                      className="font-medium text-ink hover:text-primary-hover"
                    >
                      {location.name}
                    </Link>
                  </td>
                  <td className="px-4 py-3 font-mono text-ink-subtle">{location.code}</td>
                  <td className="px-4 py-3 text-ink-muted">{location.city}</td>
                  <td className="px-4 py-3 text-ink-muted">{location.country}</td>
                  <td className="px-4 py-3 text-ink-muted">{location.timeZoneId}</td>
                  {canManage && (
                    <td className="px-4 py-3 text-right">
                      <button
                        type="button"
                        disabled={pendingDeleteId === location.id}
                        onClick={() => void handleDelete(location)}
                        className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        {pendingDeleteId === location.id ? 'Deleting…' : 'Delete'}
                      </button>
                    </td>
                  )}
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
