import { Link, useParams } from 'react-router-dom'
import { useEmployee } from '../hooks/useEmployee'
import { Avatar } from '@/app/shared/components/Avatar'
import { StatusBadge } from '@/app/shared/components/StatusBadge'
import { EmployeeDocumentsPanel } from '@/app/features/documents/components/EmployeeDocumentsPanel'

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
  const { employee, isLoading, error } = useEmployee(id)

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!employee) return null

  return (
    <div className="space-y-4">
      <Link to="/employees" className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover">
        ← Back to employees
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center gap-4 border-b border-hairline p-6">
          <Avatar name={employee.fullName} size="lg" />
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
              {employee.fullName}
            </h1>
            <p className="text-sm text-ink-subtle">
              <span className="font-mono">{employee.employeeCode}</span> ·{' '}
              {employee.designation ?? 'No designation'}
            </p>
            <div className="mt-2">
              <StatusBadge status={employee.employmentStatus ?? 'Inactive'} />
            </div>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Email" value={employee.email ?? '—'} />
          <Field label="Phone" value={employee.phoneNumber ?? '—'} />
          <Field label="Department" value={employee.departmentName ?? '—'} />
          <Field label="Designation" value={employee.designation ?? '—'} />
          <Field label="Join Date" value={employee.joinDate} />
          <Field label="Status" value={employee.employmentStatus ?? '—'} />
          <Field label="Emergency Contact" value={employee.emergencyContactName ?? '—'} />
          <Field label="Emergency Number" value={employee.emergencyContactNumber ?? '—'} />
        </div>
      </div>

      <EmployeeDocumentsPanel employeeId={employee.id} />
    </div>
  )
}
