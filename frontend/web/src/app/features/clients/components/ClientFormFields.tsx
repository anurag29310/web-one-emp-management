import type { FieldErrors, UseFormRegister } from 'react-hook-form'
import type { ClientFormValues } from '../types/clientSchema'

const inputClass =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClass = 'mb-1 block text-sm font-medium text-ink-muted'
const errorClass = 'mt-1 text-xs text-danger'

interface ClientFormFieldsProps {
  register: UseFormRegister<ClientFormValues>
  errors: FieldErrors<ClientFormValues>
  idPrefix: string
}

export function ClientFormFields({ register, errors, idPrefix }: ClientFormFieldsProps) {
  const id = (name: string) => `${idPrefix}-${name}`

  return (
    <div className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor={id('clientName')} className={labelClass}>
            Client name
          </label>
          <input id={id('clientName')} aria-invalid={Boolean(errors.clientName)} className={inputClass} {...register('clientName')} />
          {errors.clientName && <p className={errorClass}>{errors.clientName.message}</p>}
        </div>
        <div>
          <label htmlFor={id('companyName')} className={labelClass}>
            Company name
          </label>
          <input id={id('companyName')} aria-invalid={Boolean(errors.companyName)} className={inputClass} {...register('companyName')} />
          {errors.companyName && <p className={errorClass}>{errors.companyName.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor={id('contactPerson')} className={labelClass}>
            Contact person
          </label>
          <input id={id('contactPerson')} aria-invalid={Boolean(errors.contactPerson)} className={inputClass} {...register('contactPerson')} />
          {errors.contactPerson && <p className={errorClass}>{errors.contactPerson.message}</p>}
        </div>
        <div>
          <label htmlFor={id('email')} className={labelClass}>
            Email
          </label>
          <input id={id('email')} type="email" aria-invalid={Boolean(errors.email)} className={inputClass} {...register('email')} />
          {errors.email && <p className={errorClass}>{errors.email.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor={id('mobileNumber')} className={labelClass}>
            Mobile number
          </label>
          <input id={id('mobileNumber')} aria-invalid={Boolean(errors.mobileNumber)} className={inputClass} {...register('mobileNumber')} />
          {errors.mobileNumber && <p className={errorClass}>{errors.mobileNumber.message}</p>}
        </div>
        <div>
          <label htmlFor={id('alternateMobile')} className={labelClass}>
            Alternate mobile <span className="text-ink-tertiary">(optional)</span>
          </label>
          <input id={id('alternateMobile')} className={inputClass} {...register('alternateMobile')} />
        </div>
      </div>

      <div>
        <label htmlFor={id('gstNumber')} className={labelClass}>
          GST number <span className="text-ink-tertiary">(optional)</span>
        </label>
        <input id={id('gstNumber')} className={inputClass} {...register('gstNumber')} />
      </div>

      <div>
        <label htmlFor={id('addressLine1')} className={labelClass}>
          Address line 1
        </label>
        <input id={id('addressLine1')} aria-invalid={Boolean(errors.addressLine1)} className={inputClass} {...register('addressLine1')} />
        {errors.addressLine1 && <p className={errorClass}>{errors.addressLine1.message}</p>}
      </div>
      <div>
        <label htmlFor={id('addressLine2')} className={labelClass}>
          Address line 2 <span className="text-ink-tertiary">(optional)</span>
        </label>
        <input id={id('addressLine2')} className={inputClass} {...register('addressLine2')} />
      </div>

      <div className="grid grid-cols-3 gap-3">
        <div>
          <label htmlFor={id('city')} className={labelClass}>
            City
          </label>
          <input id={id('city')} aria-invalid={Boolean(errors.city)} className={inputClass} {...register('city')} />
          {errors.city && <p className={errorClass}>{errors.city.message}</p>}
        </div>
        <div>
          <label htmlFor={id('state')} className={labelClass}>
            State <span className="text-ink-tertiary">(optional)</span>
          </label>
          <input id={id('state')} className={inputClass} {...register('state')} />
        </div>
        <div>
          <label htmlFor={id('postalCode')} className={labelClass}>
            Postal code
          </label>
          <input id={id('postalCode')} aria-invalid={Boolean(errors.postalCode)} className={inputClass} {...register('postalCode')} />
          {errors.postalCode && <p className={errorClass}>{errors.postalCode.message}</p>}
        </div>
      </div>

      <div>
        <label htmlFor={id('country')} className={labelClass}>
          Country
        </label>
        <input id={id('country')} aria-invalid={Boolean(errors.country)} className={inputClass} {...register('country')} />
        {errors.country && <p className={errorClass}>{errors.country.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor={id('latitude')} className={labelClass}>
            Latitude <span className="text-ink-tertiary">(optional)</span>
          </label>
          <input id={id('latitude')} aria-invalid={Boolean(errors.latitude)} className={inputClass} {...register('latitude')} />
          {errors.latitude && <p className={errorClass}>{errors.latitude.message}</p>}
        </div>
        <div>
          <label htmlFor={id('longitude')} className={labelClass}>
            Longitude <span className="text-ink-tertiary">(optional)</span>
          </label>
          <input id={id('longitude')} aria-invalid={Boolean(errors.longitude)} className={inputClass} {...register('longitude')} />
          {errors.longitude && <p className={errorClass}>{errors.longitude.message}</p>}
        </div>
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
