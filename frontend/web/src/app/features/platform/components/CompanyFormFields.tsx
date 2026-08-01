import type { FieldErrors, UseFormRegister } from 'react-hook-form'
import type { CompanyFormValues } from '../types/companySchema'

const inputClass =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClass = 'mb-1 block text-sm font-medium text-ink-muted'
const errorClass = 'mt-1 text-xs text-danger'

interface CompanyFormFieldsProps {
  register: UseFormRegister<CompanyFormValues>
  errors: FieldErrors<CompanyFormValues>
  idPrefix: string
}

export function CompanyFormFields({ register, errors, idPrefix }: CompanyFormFieldsProps) {
  const id = (name: string) => `${idPrefix}-${name}`

  return (
    <div className="space-y-3">
      <div>
        <label htmlFor={id('name')} className={labelClass}>
          Company name
        </label>
        <input id={id('name')} aria-invalid={Boolean(errors.name)} className={inputClass} {...register('name')} />
        {errors.name && <p className={errorClass}>{errors.name.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor={id('timezone')} className={labelClass}>
            Timezone
          </label>
          <input
            id={id('timezone')}
            aria-invalid={Boolean(errors.timezone)}
            className={inputClass}
            {...register('timezone')}
          />
          {errors.timezone && <p className={errorClass}>{errors.timezone.message}</p>}
        </div>
        <div>
          <label htmlFor={id('currency')} className={labelClass}>
            Currency
          </label>
          <input
            id={id('currency')}
            aria-invalid={Boolean(errors.currency)}
            className={inputClass}
            {...register('currency')}
          />
          {errors.currency && <p className={errorClass}>{errors.currency.message}</p>}
        </div>
      </div>

      <div>
        <label htmlFor={id('logoUrl')} className={labelClass}>
          Logo URL <span className="text-ink-tertiary">(optional)</span>
        </label>
        <input
          id={id('logoUrl')}
          aria-invalid={Boolean(errors.logoUrl)}
          className={inputClass}
          {...register('logoUrl')}
        />
        {errors.logoUrl && <p className={errorClass}>{errors.logoUrl.message}</p>}
      </div>
    </div>
  )
}
