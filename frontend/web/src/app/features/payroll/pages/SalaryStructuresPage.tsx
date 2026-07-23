import { useState } from 'react'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useSalaryStructures } from '../hooks/useSalaryStructures'
import { payrollRepository, type SalaryStructure } from '../api'
import {
  salaryStructureFormSchema,
  type SalaryStructureFormInput,
  type SalaryStructureFormValues,
} from '../types/payrollSchema'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

function formatCurrency(amount: number): string {
  return amount.toLocaleString(undefined, { style: 'currency', currency: 'USD' })
}

const EMPTY_VALUES: SalaryStructureFormInput = {
  employeeId: '',
  basicSalary: 0,
  effectiveFrom: '',
  effectiveTo: '',
  allowances: [],
  deductions: [],
}

function ComponentFieldArray({
  label,
  name,
  fields,
  append,
  remove,
  register,
  errors,
}: {
  label: string
  name: 'allowances' | 'deductions'
  fields: { id: string }[]
  append: (value: { name: string; amount: number }) => void
  remove: (index: number) => void
  register: ReturnType<typeof useForm<SalaryStructureFormInput, unknown, SalaryStructureFormValues>>['register']
  errors: ReturnType<
    typeof useForm<SalaryStructureFormInput, unknown, SalaryStructureFormValues>
  >['formState']['errors']
}) {
  return (
    <div>
      <div className="mb-1 flex items-center justify-between">
        <label className="block text-sm font-medium text-ink-muted">{label}</label>
        <button
          type="button"
          onClick={() => append({ name: '', amount: 0 })}
          className="text-xs font-medium text-primary-hover hover:underline"
        >
          + Add
        </button>
      </div>
      <div className="space-y-2">
        {fields.length === 0 && <p className="text-xs text-ink-subtle">None added.</p>}
        {fields.map((field, index) => (
          <div key={field.id} className="flex items-start gap-2">
            <div className="flex-1">
              <input
                placeholder="Name"
                aria-invalid={Boolean(errors[name]?.[index]?.name)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...register(`${name}.${index}.name` as const)}
              />
            </div>
            <div className="w-32">
              <input
                type="number"
                step="0.01"
                placeholder="Amount"
                aria-invalid={Boolean(errors[name]?.[index]?.amount)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...register(`${name}.${index}.amount` as const)}
              />
            </div>
            <button
              type="button"
              onClick={() => remove(index)}
              aria-label={`Remove ${label.toLowerCase()} row`}
              className="rounded-md px-2 py-2 text-xs font-medium text-danger hover:bg-danger/10"
            >
              Remove
            </button>
          </div>
        ))}
      </div>
    </div>
  )
}

export function SalaryStructuresPage() {
  const { user } = useAuth()
  const canManagePayroll = user?.role === 'Admin' || user?.role === 'HR'

  const { salaryStructures, isLoading, error, refresh } = useSalaryStructures()
  const { result: employeesResult } = useEmployees({ pageSize: 100 })

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)
  const [listError, setListError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<SalaryStructureFormInput, unknown, SalaryStructureFormValues>({
    resolver: zodResolver(salaryStructureFormSchema),
    defaultValues: EMPTY_VALUES,
  })

  const allowancesArray = useFieldArray({ control, name: 'allowances' })
  const deductionsArray = useFieldArray({ control, name: 'deductions' })

  function employeeName(employeeId: string): string {
    return employeesResult?.data.find((e) => e.id === employeeId)?.fullName ?? employeeId
  }

  function openCreate() {
    setEditingId(null)
    setFormError(null)
    reset(EMPTY_VALUES)
    setIsFormOpen(true)
  }

  function openEdit(structure: SalaryStructure) {
    setEditingId(structure.id)
    setFormError(null)
    reset({
      employeeId: structure.employeeId,
      basicSalary: structure.basicSalary,
      effectiveFrom: structure.effectiveFrom,
      effectiveTo: structure.effectiveTo ?? '',
      allowances: structure.allowances.map((a) => ({ id: a.id, name: a.name, amount: a.amount })),
      deductions: structure.deductions.map((d) => ({ id: d.id, name: d.name, amount: d.amount })),
    })
    setIsFormOpen(true)
  }

  async function onSubmit(values: SalaryStructureFormValues) {
    setFormError(null)
    try {
      const shared = {
        basicSalary: values.basicSalary,
        allowances: values.allowances.map((a) => ({ id: a.id, name: a.name.trim(), amount: a.amount })),
        deductions: values.deductions.map((d) => ({ id: d.id, name: d.name.trim(), amount: d.amount })),
        effectiveFrom: values.effectiveFrom,
        effectiveTo: values.effectiveTo || undefined,
      }
      if (editingId) {
        await payrollRepository.updateSalaryStructure({ id: editingId, ...shared })
      } else {
        await payrollRepository.createSalaryStructure({ employeeId: values.employeeId, ...shared })
      }
      setIsFormOpen(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to save salary structure.')
    }
  }

  async function handleDelete(structure: SalaryStructure) {
    if (!window.confirm(`Delete the salary structure for ${employeeName(structure.employeeId)}?`)) return
    setListError(null)
    setPendingDeleteId(structure.id)
    try {
      await payrollRepository.deleteSalaryStructure(structure.id)
      refresh()
    } catch (err) {
      setListError(err instanceof AppError ? err.message : 'Failed to delete salary structure.')
    } finally {
      setPendingDeleteId(null)
    }
  }

  if (!canManagePayroll) {
    return (
      <div className="rounded-lg border border-hairline bg-surface-1 p-6 text-sm text-ink-subtle">
        You don&apos;t have access to salary structure management.
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Salary Structures</h1>
          <p className="text-sm text-ink-subtle">{salaryStructures.length} structures</p>
        </div>
        <button
          type="button"
          onClick={openCreate}
          className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
        >
          New salary structure
        </button>
      </div>

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}
      {listError && (
        <p role="alert" className="text-sm text-danger">
          {listError}
        </p>
      )}

      <Modal
        isOpen={isFormOpen}
        onClose={() => setIsFormOpen(false)}
        title={editingId ? 'Edit salary structure' : 'New salary structure'}
      >
        <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
          <div>
            <label htmlFor="ss-employee" className="mb-1 block text-sm font-medium text-ink-muted">
              Employee
            </label>
            <select
              id="ss-employee"
              disabled={Boolean(editingId)}
              aria-invalid={Boolean(errors.employeeId)}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50 disabled:cursor-not-allowed disabled:opacity-60"
              {...register('employeeId')}
            >
              <option value="">Select employee…</option>
              {employeesResult?.data.map((employee) => (
                <option key={employee.id} value={employee.id}>
                  {employee.fullName}
                </option>
              ))}
            </select>
            {errors.employeeId && <p className="mt-1 text-xs text-danger">{errors.employeeId.message}</p>}
          </div>

          <div>
            <label htmlFor="ss-basic" className="mb-1 block text-sm font-medium text-ink-muted">
              Basic salary
            </label>
            <input
              id="ss-basic"
              type="number"
              step="0.01"
              aria-invalid={Boolean(errors.basicSalary)}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...register('basicSalary')}
            />
            {errors.basicSalary && <p className="mt-1 text-xs text-danger">{errors.basicSalary.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="ss-from" className="mb-1 block text-sm font-medium text-ink-muted">
                Effective from
              </label>
              <input
                id="ss-from"
                type="date"
                aria-invalid={Boolean(errors.effectiveFrom)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...register('effectiveFrom')}
              />
              {errors.effectiveFrom && <p className="mt-1 text-xs text-danger">{errors.effectiveFrom.message}</p>}
            </div>
            <div>
              <label htmlFor="ss-to" className="mb-1 block text-sm font-medium text-ink-muted">
                Effective to
              </label>
              <input
                id="ss-to"
                type="date"
                aria-invalid={Boolean(errors.effectiveTo)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...register('effectiveTo')}
              />
              {errors.effectiveTo && <p className="mt-1 text-xs text-danger">{errors.effectiveTo.message}</p>}
            </div>
          </div>

          <ComponentFieldArray
            label="Allowances"
            name="allowances"
            fields={allowancesArray.fields}
            append={allowancesArray.append}
            remove={allowancesArray.remove}
            register={register}
            errors={errors}
          />
          <ComponentFieldArray
            label="Deductions"
            name="deductions"
            fields={deductionsArray.fields}
            append={deductionsArray.append}
            remove={deductionsArray.remove}
            register={register}
            errors={errors}
          />

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
            {isSubmitting ? 'Saving…' : editingId ? 'Save changes' : 'Create salary structure'}
          </button>
        </form>
      </Modal>

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Employee</th>
              <th className="px-4 py-3">Basic</th>
              <th className="px-4 py-3">Allowances</th>
              <th className="px-4 py-3">Deductions</th>
              <th className="px-4 py-3">Effective</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={6}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && salaryStructures.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={6}>
                  No salary structures yet.
                </td>
              </tr>
            )}

            {!isLoading &&
              salaryStructures.map((structure) => (
                <tr key={structure.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3 font-medium text-ink">{employeeName(structure.employeeId)}</td>
                  <td className="px-4 py-3 text-ink-muted">{formatCurrency(structure.basicSalary)}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {formatCurrency(structure.allowances.reduce((sum, a) => sum + a.amount, 0))}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">
                    {formatCurrency(structure.deductions.reduce((sum, d) => sum + d.amount, 0))}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">
                    {structure.effectiveFrom} → {structure.effectiveTo ?? 'ongoing'}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex justify-end gap-3">
                      <button
                        type="button"
                        onClick={() => openEdit(structure)}
                        className="text-xs font-medium text-primary-hover hover:underline"
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        disabled={pendingDeleteId === structure.id}
                        onClick={() => void handleDelete(structure)}
                        className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        {pendingDeleteId === structure.id ? 'Deleting…' : 'Delete'}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
