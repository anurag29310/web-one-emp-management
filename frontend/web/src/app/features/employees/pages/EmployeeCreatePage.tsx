import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { employeeRepository } from '../api'
import { employeeFormSchema, type EmployeeFormValues } from '../types/employeeFormSchema'
import { useEmployees } from '../hooks/useEmployees'
import { useDepartments } from '@/app/features/departments/hooks/useDepartments'
import { useTeams } from '@/app/features/teams/hooks/useTeams'
import { useDesignations } from '@/app/features/designations/hooks/useDesignations'
import { useOfficeLocations } from '@/app/features/office-locations/hooks/useOfficeLocations'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'

const inputClasses =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClasses = 'mb-1 block text-sm font-medium text-ink-muted'

export function EmployeeCreatePage() {
  const { user: currentUser } = useAuth()
  const canManage = currentUser?.role === 'Admin' || currentUser?.role === 'HR'
  const navigate = useNavigate()

  const { departments } = useDepartments()
  const { teams } = useTeams()
  const { designations } = useDesignations()
  const { officeLocations } = useOfficeLocations()
  const { result: employeeResult } = useEmployees({ pageSize: 100 })

  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<EmployeeFormValues>({
    resolver: zodResolver(employeeFormSchema),
    defaultValues: {
      employeeCode: '',
      firstName: '',
      middleName: '',
      lastName: '',
      email: '',
      phoneNumber: '',
      dateOfBirth: '',
      gender: '',
      addressLine1: '',
      addressLine2: '',
      city: '',
      state: '',
      postalCode: '',
      country: '',
      emergencyContactName: '',
      emergencyContactPhone: '',
      emergencyContactRelation: '',
      joinDate: new Date().toISOString().slice(0, 10),
      departmentId: '',
      teamId: '',
      designationId: '',
      managerId: '',
      officeLocationId: '',
      employmentStatus: 'Active',
    },
  })

  const selectedDepartmentId = watch('departmentId')
  const availableTeams = useMemo(
    () => teams.filter((team) => !selectedDepartmentId || team.departmentId === selectedDepartmentId),
    [teams, selectedDepartmentId],
  )

  if (!canManage) {
    return (
      <div className="rounded-lg border border-hairline bg-surface-1 p-6">
        <p className="text-sm text-danger">You don't have permission to create employees.</p>
      </div>
    )
  }

  async function onSubmit(values: EmployeeFormValues) {
    setFormError(null)
    try {
      const created = await employeeRepository.create({
        employeeCode: values.employeeCode.trim(),
        firstName: values.firstName.trim(),
        middleName: values.middleName?.trim() || undefined,
        lastName: values.lastName.trim(),
        email: values.email?.trim() || undefined,
        phoneNumber: values.phoneNumber?.trim() || undefined,
        dateOfBirth: values.dateOfBirth || undefined,
        gender: values.gender || undefined,
        address: {
          addressLine1: values.addressLine1?.trim() || undefined,
          addressLine2: values.addressLine2?.trim() || undefined,
          city: values.city?.trim() || undefined,
          state: values.state?.trim() || undefined,
          postalCode: values.postalCode?.trim() || undefined,
          country: values.country?.trim() || undefined,
        },
        emergencyContact: {
          name: values.emergencyContactName?.trim() || undefined,
          phone: values.emergencyContactPhone?.trim() || undefined,
          relation: values.emergencyContactRelation?.trim() || undefined,
        },
        joinDate: values.joinDate,
        departmentId: values.departmentId || undefined,
        teamId: values.teamId || undefined,
        designationId: values.designationId,
        managerId: values.managerId || undefined,
        officeLocationId: values.officeLocationId,
        employmentStatus: values.employmentStatus,
      })
      navigate(`/employees/${created.id}`, { replace: true })
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to create employee.')
    }
  }

  return (
    <div className="space-y-4">
      <Link to="/employees" className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover">
        ← Back to employees
      </Link>

      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">New employee</h1>
        <p className="text-sm text-ink-subtle">Add a new employee record.</p>
      </div>

      <div className="max-w-3xl rounded-lg border border-hairline bg-surface-1 p-6">
        <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-6">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="create-employeeCode" className={labelClasses}>
                Employee code
              </label>
              <input
                id="create-employeeCode"
                aria-invalid={Boolean(errors.employeeCode)}
                className={inputClasses}
                {...register('employeeCode')}
              />
              {errors.employeeCode && <p className="mt-1 text-xs text-danger">{errors.employeeCode.message}</p>}
            </div>
            <div>
              <label htmlFor="create-joinDate" className={labelClasses}>
                Join date
              </label>
              <input
                id="create-joinDate"
                type="date"
                aria-invalid={Boolean(errors.joinDate)}
                className={inputClasses}
                {...register('joinDate')}
              />
              {errors.joinDate && <p className="mt-1 text-xs text-danger">{errors.joinDate.message}</p>}
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div>
              <label htmlFor="create-firstName" className={labelClasses}>
                First name
              </label>
              <input
                id="create-firstName"
                aria-invalid={Boolean(errors.firstName)}
                className={inputClasses}
                {...register('firstName')}
              />
              {errors.firstName && <p className="mt-1 text-xs text-danger">{errors.firstName.message}</p>}
            </div>
            <div>
              <label htmlFor="create-middleName" className={labelClasses}>
                Middle name
              </label>
              <input id="create-middleName" className={inputClasses} {...register('middleName')} />
            </div>
            <div>
              <label htmlFor="create-lastName" className={labelClasses}>
                Last name
              </label>
              <input
                id="create-lastName"
                aria-invalid={Boolean(errors.lastName)}
                className={inputClasses}
                {...register('lastName')}
              />
              {errors.lastName && <p className="mt-1 text-xs text-danger">{errors.lastName.message}</p>}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="create-email" className={labelClasses}>
                Email
              </label>
              <input
                id="create-email"
                type="email"
                aria-invalid={Boolean(errors.email)}
                className={inputClasses}
                {...register('email')}
              />
              {errors.email && <p className="mt-1 text-xs text-danger">{errors.email.message}</p>}
            </div>
            <div>
              <label htmlFor="create-phoneNumber" className={labelClasses}>
                Phone number
              </label>
              <input id="create-phoneNumber" className={inputClasses} {...register('phoneNumber')} />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="create-dateOfBirth" className={labelClasses}>
                Date of birth
              </label>
              <input id="create-dateOfBirth" type="date" className={inputClasses} {...register('dateOfBirth')} />
            </div>
            <div>
              <label htmlFor="create-gender" className={labelClasses}>
                Gender
              </label>
              <input id="create-gender" className={inputClasses} {...register('gender')} />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3 border-t border-hairline pt-4">
            <div>
              <label htmlFor="create-department" className={labelClasses}>
                Department
              </label>
              <select id="create-department" className={inputClasses} {...register('departmentId')}>
                <option value="">No department</option>
                {departments.map((department) => (
                  <option key={department.id} value={department.id}>
                    {department.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="create-team" className={labelClasses}>
                Team
              </label>
              <select id="create-team" className={inputClasses} {...register('teamId')}>
                <option value="">No team</option>
                {availableTeams.map((team) => (
                  <option key={team.id} value={team.id}>
                    {team.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="create-designation" className={labelClasses}>
                Designation
              </label>
              <select
                id="create-designation"
                aria-invalid={Boolean(errors.designationId)}
                className={inputClasses}
                {...register('designationId')}
              >
                <option value="">Select designation</option>
                {designations.map((designation) => (
                  <option key={designation.id} value={designation.id}>
                    {designation.name}
                  </option>
                ))}
              </select>
              {errors.designationId && (
                <p className="mt-1 text-xs text-danger">{errors.designationId.message}</p>
              )}
            </div>
            <div>
              <label htmlFor="create-officeLocation" className={labelClasses}>
                Office location
              </label>
              <select
                id="create-officeLocation"
                aria-invalid={Boolean(errors.officeLocationId)}
                className={inputClasses}
                {...register('officeLocationId')}
              >
                <option value="">Select office location</option>
                {officeLocations.map((location) => (
                  <option key={location.id} value={location.id}>
                    {location.name}
                  </option>
                ))}
              </select>
              {errors.officeLocationId && (
                <p className="mt-1 text-xs text-danger">{errors.officeLocationId.message}</p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="create-manager" className={labelClasses}>
                Manager
              </label>
              <select id="create-manager" className={inputClasses} {...register('managerId')}>
                <option value="">No manager</option>
                {employeeResult?.data.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.fullName}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="create-status" className={labelClasses}>
                Employment status
              </label>
              <select id="create-status" className={inputClasses} {...register('employmentStatus')}>
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
                <option value="OnLeave">On leave</option>
                <option value="Terminated">Terminated</option>
              </select>
            </div>
          </div>

          <div className="space-y-3 border-t border-hairline pt-4">
            <h2 className="text-sm font-medium text-ink">Address</h2>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="create-addressLine1" className={labelClasses}>
                  Address line 1
                </label>
                <input id="create-addressLine1" className={inputClasses} {...register('addressLine1')} />
              </div>
              <div>
                <label htmlFor="create-addressLine2" className={labelClasses}>
                  Address line 2
                </label>
                <input id="create-addressLine2" className={inputClasses} {...register('addressLine2')} />
              </div>
            </div>
            <div className="grid grid-cols-4 gap-3">
              <div>
                <label htmlFor="create-city" className={labelClasses}>
                  City
                </label>
                <input id="create-city" className={inputClasses} {...register('city')} />
              </div>
              <div>
                <label htmlFor="create-state" className={labelClasses}>
                  State
                </label>
                <input id="create-state" className={inputClasses} {...register('state')} />
              </div>
              <div>
                <label htmlFor="create-postalCode" className={labelClasses}>
                  Postal code
                </label>
                <input id="create-postalCode" className={inputClasses} {...register('postalCode')} />
              </div>
              <div>
                <label htmlFor="create-country" className={labelClasses}>
                  Country
                </label>
                <input id="create-country" className={inputClasses} {...register('country')} />
              </div>
            </div>
          </div>

          <div className="space-y-3 border-t border-hairline pt-4">
            <h2 className="text-sm font-medium text-ink">Emergency contact</h2>
            <div className="grid grid-cols-3 gap-3">
              <div>
                <label htmlFor="create-emergencyName" className={labelClasses}>
                  Name
                </label>
                <input id="create-emergencyName" className={inputClasses} {...register('emergencyContactName')} />
              </div>
              <div>
                <label htmlFor="create-emergencyPhone" className={labelClasses}>
                  Phone
                </label>
                <input id="create-emergencyPhone" className={inputClasses} {...register('emergencyContactPhone')} />
              </div>
              <div>
                <label htmlFor="create-emergencyRelation" className={labelClasses}>
                  Relation
                </label>
                <input
                  id="create-emergencyRelation"
                  className={inputClasses}
                  {...register('emergencyContactRelation')}
                />
              </div>
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
            {isSubmitting ? 'Creating…' : 'Create employee'}
          </button>
        </form>
      </div>
    </div>
  )
}
