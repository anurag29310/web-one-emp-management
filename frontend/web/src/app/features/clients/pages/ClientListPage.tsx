import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useClients } from '../hooks/useClients'
import { clientRepository } from '../api'
import { clientFormSchema, type ClientFormValues } from '../types/clientSchema'
import { ClientFormFields } from '../components/ClientFormFields'
import { ClientStatusBadge } from '../components/ClientStatusBadge'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

function CreateClientForm({ onCreated }: { onCreated: () => void }) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ClientFormValues>({
    resolver: zodResolver(clientFormSchema),
    defaultValues: {
      clientName: '',
      companyName: '',
      contactPerson: '',
      mobileNumber: '',
      alternateMobile: '',
      email: '',
      gstNumber: '',
      addressLine1: '',
      addressLine2: '',
      city: '',
      state: '',
      country: '',
      postalCode: '',
      latitude: '',
      longitude: '',
      notes: '',
    },
  })

  async function onSubmit(values: ClientFormValues) {
    setFormError(null)
    try {
      await clientRepository.create({
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
      reset()
      onCreated()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to create client.')
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-3">
      <ClientFormFields register={register} errors={errors} idPrefix="create-client" />
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
        {isSubmitting ? 'Creating…' : 'Create client'}
      </button>
    </form>
  )
}

export function ClientListPage() {
  const [search, setSearch] = useState('')
  const [isActive, setIsActive] = useState<'all' | 'active' | 'inactive'>('all')
  const { result, isLoading, error, refresh } = useClients({
    pageSize: 50,
    search: search || undefined,
    isActive: isActive === 'all' ? undefined : isActive === 'active',
  })
  const { user } = useAuth()
  const canManage = user?.role === 'Admin'
  const [isFormOpen, setIsFormOpen] = useState(false)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Clients</h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsFormOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isFormOpen ? 'Cancel' : 'New client'}
          </button>
        )}
      </div>

      {canManage && (
        <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="New client">
          <CreateClientForm
            onCreated={() => {
              setIsFormOpen(false)
              refresh()
            }}
          />
        </Modal>
      )}

      <div className="flex items-center gap-3">
        <input
          type="search"
          aria-label="Search clients"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search name, company, contact, email…"
          className="w-80 rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        />
        <select
          aria-label="Filter by status"
          value={isActive}
          onChange={(e) => setIsActive(e.target.value as 'all' | 'active' | 'inactive')}
          className="rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        >
          <option value="all">All statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
        </select>
      </div>

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Client</th>
              <th className="px-4 py-3">Company</th>
              <th className="px-4 py-3">Contact</th>
              <th className="px-4 py-3">City</th>
              <th className="px-4 py-3">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={5}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && result?.data.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={5}>
                  No clients found.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((client) => (
                <tr key={client.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link to={`/clients/${client.id}`} className="font-medium text-ink hover:text-primary-hover">
                      {client.clientName}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{client.companyName}</td>
                  <td className="px-4 py-3 text-ink-muted">{client.contactPerson}</td>
                  <td className="px-4 py-3 text-ink-muted">{client.city}</td>
                  <td className="px-4 py-3">
                    <ClientStatusBadge client={client} />
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
