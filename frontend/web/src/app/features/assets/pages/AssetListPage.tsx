import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useAssets } from '../hooks/useAssets'
import { assetRepository, type AssetStatus } from '../api'
import { assetFormSchema, type AssetFormInput, type AssetFormValues } from '../types/assetSchema'
import { AssetStatusBadge } from '../components/AssetStatusBadge'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

const STATUS_OPTIONS: AssetStatus[] = ['Available', 'Assigned', 'UnderRepair', 'Retired', 'Lost']

function RegisterAssetForm({ onCreated }: { onCreated: () => void }) {
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<AssetFormInput, unknown, AssetFormValues>({
    resolver: zodResolver(assetFormSchema),
    defaultValues: {
      category: '',
      brand: '',
      model: '',
      serialNumber: '',
      purchaseDate: '',
      purchaseCost: 0,
      notes: '',
    },
  })

  async function onSubmit(values: AssetFormValues) {
    setFormError(null)
    try {
      await assetRepository.create({
        category: values.category.trim(),
        brand: values.brand.trim(),
        model: values.model.trim(),
        serialNumber: values.serialNumber.trim(),
        purchaseDate: values.purchaseDate,
        purchaseCost: values.purchaseCost,
        notes: values.notes?.trim() || undefined,
      })
      reset()
      onCreated()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to register asset.')
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="asset-category" className="mb-1 block text-sm font-medium text-ink-muted">
            Category
          </label>
          <input
            id="asset-category"
            placeholder="Laptop, Mobile, Monitor…"
            aria-invalid={Boolean(errors.category)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('category')}
          />
          {errors.category && <p className="mt-1 text-xs text-danger">{errors.category.message}</p>}
        </div>
        <div>
          <label htmlFor="asset-brand" className="mb-1 block text-sm font-medium text-ink-muted">
            Brand
          </label>
          <input
            id="asset-brand"
            aria-invalid={Boolean(errors.brand)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('brand')}
          />
          {errors.brand && <p className="mt-1 text-xs text-danger">{errors.brand.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="asset-model" className="mb-1 block text-sm font-medium text-ink-muted">
            Model
          </label>
          <input
            id="asset-model"
            aria-invalid={Boolean(errors.model)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('model')}
          />
          {errors.model && <p className="mt-1 text-xs text-danger">{errors.model.message}</p>}
        </div>
        <div>
          <label htmlFor="asset-serial" className="mb-1 block text-sm font-medium text-ink-muted">
            Serial number
          </label>
          <input
            id="asset-serial"
            aria-invalid={Boolean(errors.serialNumber)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('serialNumber')}
          />
          {errors.serialNumber && <p className="mt-1 text-xs text-danger">{errors.serialNumber.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="asset-purchase-date" className="mb-1 block text-sm font-medium text-ink-muted">
            Purchase date
          </label>
          <input
            id="asset-purchase-date"
            type="date"
            aria-invalid={Boolean(errors.purchaseDate)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('purchaseDate')}
          />
          {errors.purchaseDate && <p className="mt-1 text-xs text-danger">{errors.purchaseDate.message}</p>}
        </div>
        <div>
          <label htmlFor="asset-cost" className="mb-1 block text-sm font-medium text-ink-muted">
            Purchase cost
          </label>
          <input
            id="asset-cost"
            type="number"
            step="0.01"
            min="0"
            aria-invalid={Boolean(errors.purchaseCost)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('purchaseCost')}
          />
          {errors.purchaseCost && <p className="mt-1 text-xs text-danger">{errors.purchaseCost.message}</p>}
        </div>
      </div>

      <div>
        <label htmlFor="asset-notes" className="mb-1 block text-sm font-medium text-ink-muted">
          Notes
        </label>
        <textarea
          id="asset-notes"
          rows={2}
          aria-invalid={Boolean(errors.notes)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('notes')}
        />
        {errors.notes && <p className="mt-1 text-xs text-danger">{errors.notes.message}</p>}
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
        {isSubmitting ? 'Registering…' : 'Register asset'}
      </button>
    </form>
  )
}

export function AssetListPage() {
  const [status, setStatus] = useState<AssetStatus | ''>('')
  const [search, setSearch] = useState('')
  const { result, isLoading, error, refresh } = useAssets({
    pageSize: 50,
    status: status || undefined,
    search: search || undefined,
  })
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'
  const [isFormOpen, setIsFormOpen] = useState(false)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Assets</h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsFormOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isFormOpen ? 'Cancel' : 'Register asset'}
          </button>
        )}
      </div>

      {canManage && (
        <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="Register asset">
          <RegisterAssetForm
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
          aria-label="Search assets"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search tag, brand, model, serial…"
          className="w-72 rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        />
        <select
          aria-label="Filter by status"
          value={status}
          onChange={(e) => setStatus(e.target.value as AssetStatus | '')}
          className="rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        >
          <option value="">All statuses</option>
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
              <th className="px-4 py-3">Asset tag</th>
              <th className="px-4 py-3">Category</th>
              <th className="px-4 py-3">Brand / Model</th>
              <th className="px-4 py-3">Serial number</th>
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
                  No assets found.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((asset) => (
                <tr key={asset.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link
                      to={`/assets/${asset.id}`}
                      className="font-mono font-medium text-ink hover:text-primary-hover"
                    >
                      {asset.assetTag}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{asset.category}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {asset.brand} {asset.model}
                  </td>
                  <td className="px-4 py-3 font-mono text-ink-subtle">{asset.serialNumber}</td>
                  <td className="px-4 py-3">
                    <AssetStatusBadge status={asset.status} />
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
