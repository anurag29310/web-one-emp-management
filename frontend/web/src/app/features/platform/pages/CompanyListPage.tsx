import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useCompanies } from '../hooks/useCompanies'
import { companyRepository } from '../api'
import type { CompanyStatus } from '../api'
import { companyFormSchema, type CompanyFormValues } from '../types/companySchema'
import { CompanyFormFields } from '../components/CompanyFormFields'
import { CompanyStatusBadge } from '../components/CompanyStatusBadge'
import { Pagination } from '../components/Pagination'
import { AppError } from '@/app/shared/models/appError'
import { Modal } from '@/app/shared/components/Modal'

const STATUS_OPTIONS: CompanyStatus[] = ['Trial', 'Active', 'Suspended', 'Inactive', 'PendingApproval', 'Rejected']

function CreateCompanyForm({ onCreated }: { onCreated: () => void }) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CompanyFormValues>({
    resolver: zodResolver(companyFormSchema),
    defaultValues: { name: '', timezone: 'UTC', currency: 'USD', logoUrl: '' },
  })

  async function onSubmit(values: CompanyFormValues) {
    setFormError(null)
    try {
      await companyRepository.create({
        name: values.name.trim(),
        timezone: values.timezone.trim(),
        currency: values.currency.trim(),
        logoUrl: values.logoUrl?.trim() || undefined,
      })
      reset()
      onCreated()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to create company.')
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-3">
      <CompanyFormFields register={register} errors={errors} idPrefix="create-company" />
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
        {isSubmitting ? 'Creating…' : 'Create company'}
      </button>
    </form>
  )
}

export function CompanyListPage() {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<CompanyStatus | 'all'>('all')
  const [isFormOpen, setIsFormOpen] = useState(false)

  const { result, isLoading, error, refresh } = useCompanies({
    page,
    pageSize: 20,
    search: search || undefined,
    status: status === 'all' ? undefined : status,
  })

  function resetPage<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value)
      setPage(1)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Companies</h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        <button
          type="button"
          onClick={() => setIsFormOpen((open) => !open)}
          className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
        >
          {isFormOpen ? 'Cancel' : 'New company'}
        </button>
      </div>

      <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="New company">
        <CreateCompanyForm
          onCreated={() => {
            setIsFormOpen(false)
            refresh()
          }}
        />
      </Modal>

      <div className="flex items-center gap-3">
        <input
          type="search"
          aria-label="Search companies"
          value={search}
          onChange={(e) => resetPage(setSearch)(e.target.value)}
          placeholder="Search company name…"
          className="w-80 rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        />
        <select
          aria-label="Filter by status"
          value={status}
          onChange={(e) => resetPage(setStatus)(e.target.value as CompanyStatus | 'all')}
          className="rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        >
          <option value="all">All statuses</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
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
              <th className="px-4 py-3">Company</th>
              <th className="px-4 py-3">Timezone</th>
              <th className="px-4 py-3">Currency</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">Registered</th>
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
                  No companies found.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((company) => (
                <tr key={company.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link
                      to={`/platform/companies/${company.id}`}
                      className="font-medium text-ink hover:text-primary-hover"
                    >
                      {company.name}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{company.timezone}</td>
                  <td className="px-4 py-3 text-ink-muted">{company.currency}</td>
                  <td className="px-4 py-3">
                    <CompanyStatusBadge status={company.status} />
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{new Date(company.registeredAtUtc).toLocaleDateString()}</td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      {result && (
        <Pagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} onPageChange={setPage} />
      )}
    </div>
  )
}
