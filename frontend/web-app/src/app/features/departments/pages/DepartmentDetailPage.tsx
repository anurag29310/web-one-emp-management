import { Link, useParams } from 'react-router-dom'
import { useDepartment } from '../hooks/useDepartment'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 text-sm text-ink">{value}</p>
    </div>
  )
}

export function DepartmentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { department, isLoading, error } = useDepartment(id)

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!department) return null

  return (
    <div className="space-y-4">
      <Link
        to="/departments"
        className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover"
      >
        ← Back to departments
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
              {department.name}
            </h1>
            <p className="text-sm text-ink-subtle">{department.code ?? 'No code'}</p>
          </div>
          <Link
            to={`/employees?departmentId=${department.id}`}
            className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
          >
            View employees
          </Link>
        </div>

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Description" value={department.description ?? '—'} />
          <Field label="Created" value={new Date(department.createdAtUtc).toLocaleDateString()} />
        </div>
      </div>
    </div>
  )
}
