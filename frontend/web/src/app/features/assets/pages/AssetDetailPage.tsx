import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useAsset } from '../hooks/useAsset'
import { useAssetAssignments } from '../hooks/useAssetAssignments'
import { assetRepository } from '../api'
import {
  assetFormSchema,
  assetStatusChangeFormSchema,
  assignAssetFormSchema,
  returnAssetFormSchema,
  type AssetFormInput,
  type AssetFormValues,
  type AssetStatusChangeFormValues,
  type AssignAssetFormValues,
  type ReturnAssetFormValues,
} from '../types/assetSchema'
import { AssetStatusBadge } from '../components/AssetStatusBadge'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
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

const RETURN_STATUS_OPTIONS = ['Available', 'UnderRepair', 'Retired', 'Lost'] as const

export function AssetDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { asset, isLoading, error, refresh } = useAsset(id)
  const { assignments, refresh: refreshAssignments } = useAssetAssignments(id)
  const { result: employeeResult } = useEmployees({ pageSize: 100 })
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const [isEditing, setIsEditing] = useState(false)
  const [isChangingStatus, setIsChangingStatus] = useState(false)
  const [isAssigning, setIsAssigning] = useState(false)
  const [isReturning, setIsReturning] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  const editForm = useForm<AssetFormInput, unknown, AssetFormValues>({ resolver: zodResolver(assetFormSchema) })
  const resetEditForm = editForm.reset
  const statusForm = useForm<AssetStatusChangeFormValues>({
    resolver: zodResolver(assetStatusChangeFormSchema),
    defaultValues: { status: 'UnderRepair', notes: '' },
  })
  const assignForm = useForm<AssignAssetFormValues>({
    resolver: zodResolver(assignAssetFormSchema),
    defaultValues: { employeeId: '', expectedReturnDate: '', conditionAtAssignment: '', notes: '' },
  })
  const returnForm = useForm<ReturnAssetFormValues>({
    resolver: zodResolver(returnAssetFormSchema),
    defaultValues: { conditionAtReturn: '', resultingAssetStatus: 'Available', notes: '' },
  })

  useEffect(() => {
    if (asset) {
      resetEditForm({
        category: asset.category,
        brand: asset.brand,
        model: asset.model,
        serialNumber: asset.serialNumber,
        purchaseDate: asset.purchaseDate.slice(0, 10),
        purchaseCost: asset.purchaseCost,
        notes: asset.notes ?? '',
      })
    }
  }, [asset, resetEditForm])

  function employeeName(employeeId: string): string {
    return employeeResult?.data.find((e) => e.id === employeeId)?.fullName ?? employeeId
  }

  async function onSaveEdit(values: AssetFormValues) {
    if (!asset) return
    setFormError(null)
    try {
      await assetRepository.update(asset.id, {
        category: values.category.trim(),
        brand: values.brand.trim(),
        model: values.model.trim(),
        serialNumber: values.serialNumber.trim(),
        purchaseDate: values.purchaseDate,
        purchaseCost: values.purchaseCost,
        notes: values.notes?.trim() || undefined,
      })
      setIsEditing(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update asset.')
    }
  }

  async function onChangeStatus(values: AssetStatusChangeFormValues) {
    if (!asset) return
    setFormError(null)
    try {
      await assetRepository.changeStatus(asset.id, { status: values.status, notes: values.notes?.trim() || undefined })
      setIsChangingStatus(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to change asset status.')
    }
  }

  async function onAssign(values: AssignAssetFormValues) {
    if (!asset) return
    setFormError(null)
    try {
      await assetRepository.assign(asset.id, {
        employeeId: values.employeeId,
        expectedReturnDate: values.expectedReturnDate?.trim() || undefined,
        conditionAtAssignment: values.conditionAtAssignment?.trim() || undefined,
        notes: values.notes?.trim() || undefined,
      })
      assignForm.reset()
      setIsAssigning(false)
      refresh()
      refreshAssignments()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to assign asset.')
    }
  }

  async function onReturn(values: ReturnAssetFormValues) {
    if (!currentAssignment) return
    setFormError(null)
    try {
      await assetRepository.returnAssignment(currentAssignment.id, {
        conditionAtReturn: values.conditionAtReturn?.trim() || undefined,
        resultingAssetStatus: values.resultingAssetStatus,
        notes: values.notes?.trim() || undefined,
      })
      returnForm.reset()
      setIsReturning(false)
      refresh()
      refreshAssignments()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to record asset return.')
    }
  }

  async function handleDelete() {
    if (!asset) return
    setIsDeleting(true)
    setFormError(null)
    try {
      await assetRepository.remove(asset.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete asset.')
    } finally {
      setIsDeleting(false)
    }
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!asset) return null

  const currentAssignment = assignments.find((a) => a.returnedDate === null) ?? null

  return (
    <div className="space-y-4">
      <Link to="/assets" className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover">
        ← Back to assets
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
              {asset.brand} {asset.model}
            </h1>
            <p className="font-mono text-sm text-ink-subtle">{asset.assetTag}</p>
          </div>
          <div className="flex items-center gap-2">
            <AssetStatusBadge status={asset.status} />
            {canManage && (
              <>
                {asset.status === 'Available' && (
                  <button
                    type="button"
                    onClick={() => setIsAssigning((open) => !open)}
                    className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
                  >
                    {isAssigning ? 'Cancel' : 'Assign to employee'}
                  </button>
                )}
                {asset.status === 'Assigned' && currentAssignment && (
                  <button
                    type="button"
                    onClick={() => setIsReturning((open) => !open)}
                    className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
                  >
                    {isReturning ? 'Cancel' : 'Record return'}
                  </button>
                )}
                {asset.status !== 'Assigned' && (
                  <button
                    type="button"
                    onClick={() => setIsChangingStatus((open) => !open)}
                    className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                  >
                    {isChangingStatus ? 'Cancel' : 'Change status'}
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => setIsEditing((open) => !open)}
                  className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                >
                  {isEditing ? 'Cancel' : 'Edit'}
                </button>
                <button
                  type="button"
                  disabled={isDeleting || asset.status === 'Assigned'}
                  title={asset.status === 'Assigned' ? 'Return the asset before deleting it.' : undefined}
                  onClick={() => void handleDelete()}
                  className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {isDeleting ? 'Deleting…' : 'Delete'}
                </button>
              </>
            )}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Category" value={asset.category} />
          <Field label="Serial number" value={asset.serialNumber} />
          <Field label="Purchase date" value={new Date(asset.purchaseDate).toLocaleDateString()} />
          <Field label="Purchase cost" value={asset.purchaseCost.toLocaleString(undefined, { style: 'currency', currency: 'USD' })} />
          <Field label="Notes" value={asset.notes ?? '—'} />
          <Field
            label="Currently with"
            value={currentAssignment ? employeeName(currentAssignment.employeeId) : '—'}
          />
        </div>
        {formError && !isEditing && !isChangingStatus && !isAssigning && !isReturning && (
          <p role="alert" className="px-6 pb-6 text-sm text-danger">
            {formError}
          </p>
        )}
      </div>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="border-b border-hairline p-6">
          <h2 className="text-[18px] font-medium text-ink">Assignment history</h2>
        </div>
        <div className="overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
              <tr>
                <th className="px-4 py-3">Employee</th>
                <th className="px-4 py-3">Assigned</th>
                <th className="px-4 py-3">Expected return</th>
                <th className="px-4 py-3">Returned</th>
                <th className="px-4 py-3">Condition at return</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-hairline">
              {assignments.length === 0 && (
                <tr>
                  <td className="px-4 py-8 text-center text-ink-subtle" colSpan={5}>
                    This asset has never been assigned.
                  </td>
                </tr>
              )}
              {assignments.map((assignment) => (
                <tr key={assignment.id}>
                  <td className="px-4 py-3 font-medium text-ink">{employeeName(assignment.employeeId)}</td>
                  <td className="px-4 py-3 text-ink-muted">{new Date(assignment.assignedDate).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {assignment.expectedReturnDate ? new Date(assignment.expectedReturnDate).toLocaleDateString() : '—'}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">
                    {assignment.returnedDate ? new Date(assignment.returnedDate).toLocaleDateString() : 'Outstanding'}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{assignment.conditionAtReturn ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {canManage && (
        <Modal isOpen={isEditing} onClose={() => setIsEditing(false)} title="Edit asset">
          <form onSubmit={editForm.handleSubmit(onSaveEdit)} noValidate className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-category" className="mb-1 block text-sm font-medium text-ink-muted">
                  Category
                </label>
                <input
                  id="edit-category"
                  aria-invalid={Boolean(editForm.formState.errors.category)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...editForm.register('category')}
                />
                {editForm.formState.errors.category && (
                  <p className="mt-1 text-xs text-danger">{editForm.formState.errors.category.message}</p>
                )}
              </div>
              <div>
                <label htmlFor="edit-brand" className="mb-1 block text-sm font-medium text-ink-muted">
                  Brand
                </label>
                <input
                  id="edit-brand"
                  aria-invalid={Boolean(editForm.formState.errors.brand)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...editForm.register('brand')}
                />
                {editForm.formState.errors.brand && (
                  <p className="mt-1 text-xs text-danger">{editForm.formState.errors.brand.message}</p>
                )}
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-model" className="mb-1 block text-sm font-medium text-ink-muted">
                  Model
                </label>
                <input
                  id="edit-model"
                  aria-invalid={Boolean(editForm.formState.errors.model)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...editForm.register('model')}
                />
                {editForm.formState.errors.model && (
                  <p className="mt-1 text-xs text-danger">{editForm.formState.errors.model.message}</p>
                )}
              </div>
              <div>
                <label htmlFor="edit-serial" className="mb-1 block text-sm font-medium text-ink-muted">
                  Serial number
                </label>
                <input
                  id="edit-serial"
                  aria-invalid={Boolean(editForm.formState.errors.serialNumber)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...editForm.register('serialNumber')}
                />
                {editForm.formState.errors.serialNumber && (
                  <p className="mt-1 text-xs text-danger">{editForm.formState.errors.serialNumber.message}</p>
                )}
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-purchase-date" className="mb-1 block text-sm font-medium text-ink-muted">
                  Purchase date
                </label>
                <input
                  id="edit-purchase-date"
                  type="date"
                  aria-invalid={Boolean(editForm.formState.errors.purchaseDate)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...editForm.register('purchaseDate')}
                />
                {editForm.formState.errors.purchaseDate && (
                  <p className="mt-1 text-xs text-danger">{editForm.formState.errors.purchaseDate.message}</p>
                )}
              </div>
              <div>
                <label htmlFor="edit-cost" className="mb-1 block text-sm font-medium text-ink-muted">
                  Purchase cost
                </label>
                <input
                  id="edit-cost"
                  type="number"
                  step="0.01"
                  min="0"
                  aria-invalid={Boolean(editForm.formState.errors.purchaseCost)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...editForm.register('purchaseCost')}
                />
                {editForm.formState.errors.purchaseCost && (
                  <p className="mt-1 text-xs text-danger">{editForm.formState.errors.purchaseCost.message}</p>
                )}
              </div>
            </div>
            <div>
              <label htmlFor="edit-notes" className="mb-1 block text-sm font-medium text-ink-muted">
                Notes
              </label>
              <textarea
                id="edit-notes"
                rows={2}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...editForm.register('notes')}
              />
            </div>
            {formError && (
              <p role="alert" className="text-sm text-danger">
                {formError}
              </p>
            )}
            <button
              type="submit"
              disabled={editForm.formState.isSubmitting}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {editForm.formState.isSubmitting ? 'Saving…' : 'Save changes'}
            </button>
          </form>
        </Modal>
      )}

      {canManage && (
        <Modal isOpen={isChangingStatus} onClose={() => setIsChangingStatus(false)} title="Change asset status">
          <form onSubmit={statusForm.handleSubmit(onChangeStatus)} noValidate className="space-y-3">
            <div>
              <label htmlFor="status-select" className="mb-1 block text-sm font-medium text-ink-muted">
                New status
              </label>
              <select
                id="status-select"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...statusForm.register('status')}
              >
                {RETURN_STATUS_OPTIONS.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="status-notes" className="mb-1 block text-sm font-medium text-ink-muted">
                Notes
              </label>
              <textarea
                id="status-notes"
                rows={2}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...statusForm.register('notes')}
              />
            </div>
            {formError && (
              <p role="alert" className="text-sm text-danger">
                {formError}
              </p>
            )}
            <button
              type="submit"
              disabled={statusForm.formState.isSubmitting}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {statusForm.formState.isSubmitting ? 'Saving…' : 'Update status'}
            </button>
          </form>
        </Modal>
      )}

      {canManage && (
        <Modal isOpen={isAssigning} onClose={() => setIsAssigning(false)} title="Assign asset">
          <form onSubmit={assignForm.handleSubmit(onAssign)} noValidate className="space-y-3">
            <div>
              <label htmlFor="assign-employee" className="mb-1 block text-sm font-medium text-ink-muted">
                Employee
              </label>
              <select
                id="assign-employee"
                aria-invalid={Boolean(assignForm.formState.errors.employeeId)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...assignForm.register('employeeId')}
              >
                <option value="">Select employee…</option>
                {employeeResult?.data.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.fullName}
                  </option>
                ))}
              </select>
              {assignForm.formState.errors.employeeId && (
                <p className="mt-1 text-xs text-danger">{assignForm.formState.errors.employeeId.message}</p>
              )}
            </div>
            <div>
              <label htmlFor="assign-return-date" className="mb-1 block text-sm font-medium text-ink-muted">
                Expected return date
              </label>
              <input
                id="assign-return-date"
                type="date"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...assignForm.register('expectedReturnDate')}
              />
            </div>
            <div>
              <label htmlFor="assign-condition" className="mb-1 block text-sm font-medium text-ink-muted">
                Condition at assignment
              </label>
              <input
                id="assign-condition"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...assignForm.register('conditionAtAssignment')}
              />
            </div>
            {formError && (
              <p role="alert" className="text-sm text-danger">
                {formError}
              </p>
            )}
            <button
              type="submit"
              disabled={assignForm.formState.isSubmitting}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {assignForm.formState.isSubmitting ? 'Assigning…' : 'Assign'}
            </button>
          </form>
        </Modal>
      )}

      {canManage && (
        <Modal isOpen={isReturning} onClose={() => setIsReturning(false)} title="Record asset return">
          <form onSubmit={returnForm.handleSubmit(onReturn)} noValidate className="space-y-3">
            <div>
              <label htmlFor="return-condition" className="mb-1 block text-sm font-medium text-ink-muted">
                Condition at return
              </label>
              <input
                id="return-condition"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...returnForm.register('conditionAtReturn')}
              />
            </div>
            <div>
              <label htmlFor="return-status" className="mb-1 block text-sm font-medium text-ink-muted">
                Resulting asset status
              </label>
              <select
                id="return-status"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...returnForm.register('resultingAssetStatus')}
              >
                {RETURN_STATUS_OPTIONS.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="return-notes" className="mb-1 block text-sm font-medium text-ink-muted">
                Notes
              </label>
              <textarea
                id="return-notes"
                rows={2}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...returnForm.register('notes')}
              />
            </div>
            {formError && (
              <p role="alert" className="text-sm text-danger">
                {formError}
              </p>
            )}
            <button
              type="submit"
              disabled={returnForm.formState.isSubmitting}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {returnForm.formState.isSubmitting ? 'Saving…' : 'Record return'}
            </button>
          </form>
        </Modal>
      )}
    </div>
  )
}
