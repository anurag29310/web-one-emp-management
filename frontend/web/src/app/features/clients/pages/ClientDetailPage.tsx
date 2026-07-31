import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useClient } from '../hooks/useClient'
import { clientRepository } from '../api'
import { clientFormSchema, type ClientFormValues } from '../types/clientSchema'
import { ClientFormFields } from '../components/ClientFormFields'
import { ClientStatusBadge } from '../components/ClientStatusBadge'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 text-sm text-ink">{value}</p>
    </div>
  )
}

export function ClientDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { client, isLoading, error, refresh } = useClient(id)
  const { user } = useAuth()
  const navigate = useNavigate()
  const canManage = user?.role === 'Admin'

  const [isEditing, setIsEditing] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ClientFormValues>({ resolver: zodResolver(clientFormSchema) })

  useEffect(() => {
    if (client) {
      reset({
        clientName: client.clientName,
        companyName: client.companyName,
        contactPerson: client.contactPerson,
        mobileNumber: client.mobileNumber,
        alternateMobile: client.alternateMobile ?? '',
        email: client.email,
        gstNumber: client.gstNumber ?? '',
        addressLine1: client.addressLine1,
        addressLine2: client.addressLine2 ?? '',
        city: client.city,
        state: client.state ?? '',
        country: client.country,
        postalCode: client.postalCode,
        latitude: client.latitude?.toString() ?? '',
        longitude: client.longitude?.toString() ?? '',
        notes: client.notes ?? '',
      })
    }
  }, [client, reset])

  async function onSave(values: ClientFormValues) {
    if (!client) return
    setFormError(null)
    try {
      await clientRepository.update(client.id, {
        clientName: values.clientName.trim(),
        companyName: values.companyName.trim(),
        contactPerson: values.contactPerson.trim(),
        mobileNumber: values.mobileNumber.trim(),
        alternateMobile: values.alternateMobile?.trim() || undefined,
        email: values.email.trim(),
        gstNumber: values.gstNumber?.trim() || undefined,
        addressLine1: values.addressLine1.trim(),
        addressLine2: values.addressLine2?.trim() || undefined,
        city: values.city.trim(),
        state: values.state?.trim() || undefined,
        country: values.country.trim(),
        postalCode: values.postalCode.trim(),
        latitude: values.latitude ? Number(values.latitude) : undefined,
        longitude: values.longitude ? Number(values.longitude) : undefined,
        notes: values.notes?.trim() || undefined,
      })
      setIsEditing(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update client.')
    }
  }

  async function runAction(action: () => Promise<void>, failureMessage: string) {
    if (!client) return
    setIsBusy(true)
    setFormError(null)
    try {
      await action()
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : failureMessage)
    } finally {
      setIsBusy(false)
    }
  }

  async function handleDelete() {
    if (!client) return
    setIsBusy(true)
    setFormError(null)
    try {
      await clientRepository.remove(client.id)
      navigate('/clients')
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete client.')
      setIsBusy(false)
    }
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!client) return null

  const mapsUrl =
    client.latitude != null && client.longitude != null
      ? `https://www.google.com/maps/search/?api=1&query=${client.latitude},${client.longitude}`
      : null

  return (
    <div className="space-y-4">
      <Link to="/clients" className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover">
        ← Back to clients
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">{client.clientName}</h1>
            <p className="text-sm text-ink-subtle">{client.companyName}</p>
          </div>
          <div className="flex items-center gap-2">
            <ClientStatusBadge client={client} />
            {canManage && (
              <>
                <button
                  type="button"
                  onClick={() => setIsEditing((editing) => !editing)}
                  className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                >
                  {isEditing ? 'Cancel' : 'Edit'}
                </button>
                {client.isArchived || client.isDeleted ? (
                  <button
                    type="button"
                    disabled={isBusy}
                    onClick={() => void runAction(() => clientRepository.restore(client.id), 'Failed to restore client.')}
                    className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Restore
                  </button>
                ) : client.isActive ? (
                  <button
                    type="button"
                    disabled={isBusy}
                    onClick={() => void runAction(() => clientRepository.deactivate(client.id), 'Failed to deactivate client.')}
                    className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Deactivate
                  </button>
                ) : (
                  <button
                    type="button"
                    disabled={isBusy}
                    onClick={() => void runAction(() => clientRepository.activate(client.id), 'Failed to activate client.')}
                    className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Activate
                  </button>
                )}
                {!client.isArchived && !client.isDeleted && (
                  <button
                    type="button"
                    disabled={isBusy}
                    onClick={() => void runAction(() => clientRepository.archive(client.id), 'Failed to archive client.')}
                    className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Archive
                  </button>
                )}
                <button
                  type="button"
                  disabled={isBusy}
                  onClick={() => void handleDelete()}
                  className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  Delete
                </button>
              </>
            )}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Contact person" value={client.contactPerson} />
          <Field label="Email" value={client.email} />
          <Field label="Mobile" value={client.mobileNumber} />
          <Field label="Alternate mobile" value={client.alternateMobile ?? '—'} />
          <Field
            label="Address"
            value={[client.addressLine1, client.addressLine2, client.city, client.state, client.country, client.postalCode]
              .filter(Boolean)
              .join(', ')}
          />
          <Field label="GST number" value={client.gstNumber ?? '—'} />
          <Field label="Notes" value={client.notes ?? '—'} />
          <div>
            <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">Location</p>
            {mapsUrl ? (
              <a href={mapsUrl} target="_blank" rel="noreferrer" className="mt-0.5 text-sm text-primary hover:text-primary-hover">
                Open in Maps
              </a>
            ) : (
              <p className="mt-0.5 text-sm text-ink">—</p>
            )}
          </div>
        </div>
        {formError && !isEditing && (
          <p role="alert" className="px-6 pb-6 text-sm text-danger">
            {formError}
          </p>
        )}
      </div>

      {canManage && (
        <Modal isOpen={isEditing} onClose={() => setIsEditing(false)} title="Edit client">
          <form onSubmit={handleSubmit(onSave)} noValidate className="space-y-3">
            <ClientFormFields register={register} errors={errors} idPrefix="edit-client" />
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
