import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useEmployee } from '../hooks/useEmployee'
import { employeeRepository } from '../api'
import {
  employeeFormSchema,
  employeeStatusFormSchema,
  type EmployeeFormValues,
  type EmployeeStatusFormValues,
} from '../types/employeeFormSchema'
import { useDepartments } from '@/app/features/departments/hooks/useDepartments'
import { useTeams } from '@/app/features/teams/hooks/useTeams'
import { useDesignations } from '@/app/features/designations/hooks/useDesignations'
import { useOfficeLocations } from '@/app/features/office-locations/hooks/useOfficeLocations'
import { useEmployees } from '../hooks/useEmployees'
import { Avatar } from '@/app/shared/components/Avatar'
import { StatusBadge } from '@/app/shared/components/StatusBadge'
import { EmployeeDocumentsPanel } from '@/app/features/documents/components/EmployeeDocumentsPanel'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'

const inputClasses =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClasses = 'mb-1 block text-sm font-medium text-ink-muted'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 text-sm text-ink">{value}</p>
    </div>
  )
}

export function EmployeeProfilePage() {
  const { id } = useParams<{ id: string }>()
  const { user: currentUser } = useAuth()
  const canManage = currentUser?.role === 'Admin' || currentUser?.role === 'HR'
  const navigate = useNavigate()

  const { employee, isLoading, error, refresh } = useEmployee(id)
  const { departments } = useDepartments()
  const { teams } = useTeams()
  const { designations } = useDesignations()
  const { officeLocations } = useOfficeLocations()
  const { result: employeeResult } = useEmployees({ pageSize: 100 })

  const [isEditing, setIsEditing] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [statusError, setStatusError] = useState<string | null>(null)
  const [isSavingStatus, setIsSavingStatus] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<EmployeeFormValues>({ resolver: zodResolver(employeeFormSchema) })

  const {
    register: registerStatus,
    handleSubmit: handleStatusSubmit,
    reset: resetStatus,
    formState: { errors: statusErrors },
  } = useForm<EmployeeStatusFormValues>({ resolver: zodResolver(employeeStatusFormSchema) })

  useEffect(() => {
    if (employee) {
      reset({
        employeeCode: employee.employeeCode,
        firstName: employee.firstName,
        middleName: employee.middleName ?? '',
        lastName: employee.lastName,
        email: employee.email ?? '',
        phoneNumber: employee.phoneNumber ?? '',
        dateOfBirth: employee.dateOfBirth ?? '',
        gender: employee.gender ?? '',
        addressLine1: employee.address.addressLine1 ?? '',
        addressLine2: employee.address.addressLine2 ?? '',
        city: employee.address.city ?? '',
        state: employee.address.state ?? '',
        postalCode: employee.address.postalCode ?? '',
        country: employee.address.country ?? '',
        emergencyContactName: employee.emergencyContact.name ?? '',
        emergencyContactPhone: employee.emergencyContact.phone ?? '',
        emergencyContactRelation: employee.emergencyContact.relation ?? '',
        joinDate: employee.joinDate.slice(0, 10),
        departmentId: employee.departmentId ?? '',
        teamId: employee.teamId ?? '',
        designationId: employee.designationId,
        managerId: employee.managerId ?? '',
        officeLocationId: employee.officeLocationId,
        employmentStatus: employee.employmentStatus ?? 'Active',
      })
      resetStatus({
        status: employee.employmentStatus ?? 'Active',
        exitDate: employee.exitDate?.slice(0, 10) ?? '',
        reason: '',
      })
    }
  }, [employee, reset, resetStatus])

  const selectedDepartmentId = watch('departmentId')
  const availableTeams = useMemo(
    () => teams.filter((team) => !selectedDepartmentId || team.departmentId === selectedDepartmentId),
    [teams, selectedDepartmentId],
  )

  async function onSave(values: EmployeeFormValues) {
    if (!employee) return
    setFormError(null)
    try {
      await employeeRepository.update({
        id: employee.id,
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
      setIsEditing(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update employee.')
    }
  }

  async function onUpdateStatus(values: EmployeeStatusFormValues) {
    if (!employee) return
    setStatusError(null)
    setIsSavingStatus(true)
    try {
      await employeeRepository.updateStatus({
        id: employee.id,
        status: values.status,
        exitDate: values.exitDate || undefined,
        reason: values.reason?.trim() || undefined,
      })
      refresh()
    } catch (err) {
      setStatusError(err instanceof AppError ? err.message : 'Failed to update employment status.')
    } finally {
      setIsSavingStatus(false)
    }
  }

  async function handleDelete() {
    if (!employee) return
    setFormError(null)
    setIsDeleting(true)
    try {
      await employeeRepository.remove(employee.id)
      navigate('/employees')
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete employee.')
      setIsDeleting(false)
    }
  }

  async function handleRestore() {
    if (!employee) return
    setFormError(null)
    try {
      await employeeRepository.restore(employee.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to restore employee.')
    }
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!employee) return null

  const manager = employeeResult?.data.find((e) => e.id === employee.managerId)

  return (
    <div className="space-y-4">
      <Link to="/employees" className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover">
        ← Back to employees
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between gap-4 border-b border-hairline p-6">
          <div className="flex items-center gap-4">
            <Avatar name={employee.fullName} size="lg" />
            <div>
              <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
                {employee.fullName}
              </h1>
              <p className="text-sm text-ink-subtle">
                <span className="font-mono">{employee.employeeCode}</span> ·{' '}
                {employee.designationName ?? 'No designation'}
              </p>
              <div className="mt-2">
                <StatusBadge status={employee.isDeleted ? 'Terminated' : employee.employmentStatus ?? 'Inactive'} />
              </div>
            </div>
          </div>
          {canManage && (
            <div className="flex items-center gap-2">
              {employee.isDeleted ? (
                <button
                  type="button"
                  onClick={() => void handleRestore()}
                  className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                >
                  Restore
                </button>
              ) : (
                <>
                  <button
                    type="button"
                    onClick={() => setIsEditing((editing) => !editing)}
                    className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                  >
                    {isEditing ? 'Cancel' : 'Edit'}
                  </button>
                  <button
                    type="button"
                    disabled={isDeleting}
                    onClick={() => void handleDelete()}
                    className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    {isDeleting ? 'Deleting…' : 'Delete'}
                  </button>
                </>
              )}
            </div>
          )}
        </div>

        {!isEditing && (
          <div className="grid grid-cols-2 gap-6 p-6">
            <Field label="Email" value={employee.email ?? '—'} />
            <Field label="Phone" value={employee.phoneNumber ?? '—'} />
            <Field label="Department" value={employee.departmentName ?? '—'} />
            <Field label="Team" value={employee.teamName ?? '—'} />
            <Field label="Designation" value={employee.designationName ?? '—'} />
            <Field label="Office location" value={employee.officeLocationName ?? '—'} />
            <Field label="Manager" value={manager?.fullName ?? '—'} />
            <Field label="Join date" value={employee.joinDate.slice(0, 10)} />
            <Field label="Exit date" value={employee.exitDate?.slice(0, 10) ?? '—'} />
            <Field label="Status" value={employee.employmentStatus ?? '—'} />
            <Field
              label="Address"
              value={
                [
                  employee.address.addressLine1,
                  employee.address.addressLine2,
                  employee.address.city,
                  employee.address.state,
                  employee.address.postalCode,
                  employee.address.country,
                ]
                  .filter(Boolean)
                  .join(', ') || '—'
              }
            />
            <Field
              label="Emergency contact"
              value={
                employee.emergencyContact.name
                  ? `${employee.emergencyContact.name} (${employee.emergencyContact.relation ?? 'n/a'}) · ${employee.emergencyContact.phone ?? '—'}`
                  : '—'
              }
            />
          </div>
        )}

        {isEditing && (
          <form onSubmit={handleSubmit(onSave)} noValidate className="space-y-6 p-6">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-employeeCode" className={labelClasses}>
                  Employee code
                </label>
                <input
                  id="edit-employeeCode"
                  aria-invalid={Boolean(errors.employeeCode)}
                  className={inputClasses}
                  {...register('employeeCode')}
                />
                {errors.employeeCode && <p className="mt-1 text-xs text-danger">{errors.employeeCode.message}</p>}
              </div>
              <div>
                <label htmlFor="edit-joinDate" className={labelClasses}>
                  Join date
                </label>
                <input
                  id="edit-joinDate"
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
                <label htmlFor="edit-firstName" className={labelClasses}>
                  First name
                </label>
                <input
                  id="edit-firstName"
                  aria-invalid={Boolean(errors.firstName)}
                  className={inputClasses}
                  {...register('firstName')}
                />
                {errors.firstName && <p className="mt-1 text-xs text-danger">{errors.firstName.message}</p>}
              </div>
              <div>
                <label htmlFor="edit-middleName" className={labelClasses}>
                  Middle name
                </label>
                <input id="edit-middleName" className={inputClasses} {...register('middleName')} />
              </div>
              <div>
                <label htmlFor="edit-lastName" className={labelClasses}>
                  Last name
                </label>
                <input
                  id="edit-lastName"
                  aria-invalid={Boolean(errors.lastName)}
                  className={inputClasses}
                  {...register('lastName')}
                />
                {errors.lastName && <p className="mt-1 text-xs text-danger">{errors.lastName.message}</p>}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-email" className={labelClasses}>
                  Email
                </label>
                <input
                  id="edit-email"
                  type="email"
                  aria-invalid={Boolean(errors.email)}
                  className={inputClasses}
                  {...register('email')}
                />
                {errors.email && <p className="mt-1 text-xs text-danger">{errors.email.message}</p>}
              </div>
              <div>
                <label htmlFor="edit-phoneNumber" className={labelClasses}>
                  Phone number
                </label>
                <input id="edit-phoneNumber" className={inputClasses} {...register('phoneNumber')} />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-dateOfBirth" className={labelClasses}>
                  Date of birth
                </label>
                <input id="edit-dateOfBirth" type="date" className={inputClasses} {...register('dateOfBirth')} />
              </div>
              <div>
                <label htmlFor="edit-gender" className={labelClasses}>
                  Gender
                </label>
                <input id="edit-gender" className={inputClasses} {...register('gender')} />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3 border-t border-hairline pt-4">
              <div>
                <label htmlFor="edit-department" className={labelClasses}>
                  Department
                </label>
                <select id="edit-department" className={inputClasses} {...register('departmentId')}>
                  <option value="">No department</option>
                  {departments.map((department) => (
                    <option key={department.id} value={department.id}>
                      {department.name}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label htmlFor="edit-team" className={labelClasses}>
                  Team
                </label>
                <select id="edit-team" className={inputClasses} {...register('teamId')}>
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
                <label htmlFor="edit-designation" className={labelClasses}>
                  Designation
                </label>
                <select
                  id="edit-designation"
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
                <label htmlFor="edit-officeLocation" className={labelClasses}>
                  Office location
                </label>
                <select
                  id="edit-officeLocation"
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

            <div>
              <label htmlFor="edit-manager" className={labelClasses}>
                Manager
              </label>
              <select id="edit-manager" className={inputClasses} {...register('managerId')}>
                <option value="">No manager</option>
                {employeeResult?.data
                  .filter((e) => e.id !== employee.id)
                  .map((e) => (
                    <option key={e.id} value={e.id}>
                      {e.fullName}
                    </option>
                  ))}
              </select>
            </div>

            <div className="space-y-3 border-t border-hairline pt-4">
              <h2 className="text-sm font-medium text-ink">Address</h2>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label htmlFor="edit-addressLine1" className={labelClasses}>
                    Address line 1
                  </label>
                  <input id="edit-addressLine1" className={inputClasses} {...register('addressLine1')} />
                </div>
                <div>
                  <label htmlFor="edit-addressLine2" className={labelClasses}>
                    Address line 2
                  </label>
                  <input id="edit-addressLine2" className={inputClasses} {...register('addressLine2')} />
                </div>
              </div>
              <div className="grid grid-cols-4 gap-3">
                <div>
                  <label htmlFor="edit-city" className={labelClasses}>
                    City
                  </label>
                  <input id="edit-city" className={inputClasses} {...register('city')} />
                </div>
                <div>
                  <label htmlFor="edit-state" className={labelClasses}>
                    State
                  </label>
                  <input id="edit-state" className={inputClasses} {...register('state')} />
                </div>
                <div>
                  <label htmlFor="edit-postalCode" className={labelClasses}>
                    Postal code
                  </label>
                  <input id="edit-postalCode" className={inputClasses} {...register('postalCode')} />
                </div>
                <div>
                  <label htmlFor="edit-country" className={labelClasses}>
                    Country
                  </label>
                  <input id="edit-country" className={inputClasses} {...register('country')} />
                </div>
              </div>
            </div>

            <div className="space-y-3 border-t border-hairline pt-4">
              <h2 className="text-sm font-medium text-ink">Emergency contact</h2>
              <div className="grid grid-cols-3 gap-3">
                <div>
                  <label htmlFor="edit-emergencyName" className={labelClasses}>
                    Name
                  </label>
                  <input id="edit-emergencyName" className={inputClasses} {...register('emergencyContactName')} />
                </div>
                <div>
                  <label htmlFor="edit-emergencyPhone" className={labelClasses}>
                    Phone
                  </label>
                  <input id="edit-emergencyPhone" className={inputClasses} {...register('emergencyContactPhone')} />
                </div>
                <div>
                  <label htmlFor="edit-emergencyRelation" className={labelClasses}>
                    Relation
                  </label>
                  <input
                    id="edit-emergencyRelation"
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

            <div className="flex justify-end border-t border-hairline pt-4">
              <button
                type="submit"
                disabled={isSubmitting}
                className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isSubmitting ? 'Saving…' : 'Save changes'}
              </button>
            </div>
          </form>
        )}

        {!isEditing && formError && (
          <p role="alert" className="px-6 pb-6 text-sm text-danger">
            {formError}
          </p>
        )}
      </div>

      {canManage && !employee.isDeleted && (
        <div className="rounded-lg border border-hairline bg-surface-1 p-6">
          <h2 className="mb-3 text-sm font-medium text-ink">Employment status</h2>
          <form
            onSubmit={handleStatusSubmit(onUpdateStatus)}
            noValidate
            className="flex flex-wrap items-end gap-3"
          >
            <div>
              <label htmlFor="status-select" className={labelClasses}>
                Status
              </label>
              <select id="status-select" className={inputClasses} {...registerStatus('status')}>
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
                <option value="OnLeave">On leave</option>
                <option value="Terminated">Terminated</option>
              </select>
            </div>
            <div>
              <label htmlFor="status-exitDate" className={labelClasses}>
                Exit date
              </label>
              <input id="status-exitDate" type="date" className={inputClasses} {...registerStatus('exitDate')} />
            </div>
            <div className="min-w-[200px] flex-1">
              <label htmlFor="status-reason" className={labelClasses}>
                Reason
              </label>
              <input id="status-reason" className={inputClasses} {...registerStatus('reason')} />
            </div>
            <button
              type="submit"
              disabled={isSavingStatus}
              className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSavingStatus ? 'Saving…' : 'Update status'}
            </button>
          </form>
          {statusErrors.status && <p className="mt-1 text-xs text-danger">{statusErrors.status.message}</p>}
          {statusError && (
            <p role="alert" className="mt-2 text-sm text-danger">
              {statusError}
            </p>
          )}
        </div>
      )}

      <EmployeeDocumentsPanel employeeId={employee.id} />
    </div>
  )
}
