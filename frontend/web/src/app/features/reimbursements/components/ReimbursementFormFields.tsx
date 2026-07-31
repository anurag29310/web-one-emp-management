import type { FieldErrors, UseFormRegister, UseFormWatch } from 'react-hook-form'
import type { ReimbursementFormValues } from '../types/reimbursementSchema'

const inputClass =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClass = 'mb-1 block text-sm font-medium text-ink-muted'
const errorClass = 'mt-1 text-xs text-danger'

interface ReimbursementFormFieldsProps {
  register: UseFormRegister<ReimbursementFormValues>
  watch: UseFormWatch<ReimbursementFormValues>
  errors: FieldErrors<ReimbursementFormValues>
  idPrefix: string
}

export function ReimbursementFormFields({ register, watch, errors, idPrefix }: ReimbursementFormFieldsProps) {
  const id = (name: string) => `${idPrefix}-${name}`
  const claimType = watch('claimType')

  return (
    <div className="space-y-3">
      <div>
        <label htmlFor={id('expenseTitle')} className={labelClass}>
          Expense title
        </label>
        <input
          id={id('expenseTitle')}
          aria-invalid={Boolean(errors.expenseTitle)}
          className={inputClass}
          {...register('expenseTitle')}
        />
        {errors.expenseTitle && <p className={errorClass}>{errors.expenseTitle.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor={id('expenseCategory')} className={labelClass}>
            Category
          </label>
          <input
            id={id('expenseCategory')}
            placeholder="Meals, Travel, Mileage…"
            aria-invalid={Boolean(errors.expenseCategory)}
            className={inputClass}
            {...register('expenseCategory')}
          />
          {errors.expenseCategory && <p className={errorClass}>{errors.expenseCategory.message}</p>}
        </div>
        <div>
          <label htmlFor={id('expenseDate')} className={labelClass}>
            Expense date
          </label>
          <input
            id={id('expenseDate')}
            type="date"
            aria-invalid={Boolean(errors.expenseDate)}
            className={inputClass}
            {...register('expenseDate')}
          />
          {errors.expenseDate && <p className={errorClass}>{errors.expenseDate.message}</p>}
        </div>
      </div>

      <div>
        <span className={labelClass}>Claim type</span>
        <div className="flex gap-4 text-sm text-ink">
          <label className="flex items-center gap-2">
            <input type="radio" value="Amount" {...register('claimType')} />
            Flat amount
          </label>
          <label className="flex items-center gap-2">
            <input type="radio" value="Mileage" {...register('claimType')} />
            Mileage
          </label>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        {claimType === 'Mileage' ? (
          <div>
            <label htmlFor={id('distanceKm')} className={labelClass}>
              Distance (km)
            </label>
            <input
              id={id('distanceKm')}
              type="number"
              step="0.1"
              min="0"
              aria-invalid={Boolean(errors.distanceKm)}
              className={inputClass}
              {...register('distanceKm')}
            />
            {errors.distanceKm && <p className={errorClass}>{errors.distanceKm.message}</p>}
          </div>
        ) : (
          <div>
            <label htmlFor={id('amount')} className={labelClass}>
              Amount
            </label>
            <input
              id={id('amount')}
              type="number"
              step="0.01"
              min="0"
              aria-invalid={Boolean(errors.amount)}
              className={inputClass}
              {...register('amount')}
            />
            {errors.amount && <p className={errorClass}>{errors.amount.message}</p>}
          </div>
        )}
        <div>
          <label htmlFor={id('currency')} className={labelClass}>
            Currency
          </label>
          <input id={id('currency')} aria-invalid={Boolean(errors.currency)} className={inputClass} {...register('currency')} />
          {errors.currency && <p className={errorClass}>{errors.currency.message}</p>}
        </div>
      </div>

      <div>
        <label htmlFor={id('description')} className={labelClass}>
          Description
        </label>
        <textarea id={id('description')} rows={2} className={inputClass} {...register('description')} />
      </div>
      <div>
        <label htmlFor={id('notes')} className={labelClass}>
          Notes
        </label>
        <textarea id={id('notes')} rows={2} className={inputClass} {...register('notes')} />
      </div>
    </div>
  )
}
