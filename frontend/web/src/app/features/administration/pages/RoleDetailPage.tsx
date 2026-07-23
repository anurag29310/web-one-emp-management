import { Link, useParams } from 'react-router-dom'
import { useRole } from '../hooks/useRole'
import { useUsers } from '../hooks/useUsers'
import { useAuth } from '@/app/core/auth/useAuth'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 text-sm text-ink">{value}</p>
    </div>
  )
}

export function RoleDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { user } = useAuth()
  // GET /roles/{id} is Admin-only (CanManageUsers) — unlike GET /roles (list),
  // which HR can also reach — see docs/api-specification.md section 4.2.
  const isAdmin = user?.role === 'Admin'

  const { role, isLoading, error } = useRole(id)
  const { users: assignedUsers } = useUsers({ roleId: id }, isAdmin)

  if (!isAdmin) {
    return (
      <div className="rounded-lg border border-hairline bg-surface-1 p-6">
        <p className="text-sm text-danger">You don't have permission to view role details.</p>
      </div>
    )
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!role) return null

  return (
    <div className="space-y-4">
      <Link
        to="/admin/roles"
        className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover"
      >
        ← Back to roles
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="border-b border-hairline p-6">
          <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">{role.name}</h1>
          <p className="text-sm text-ink-subtle">{role.description ?? 'No description provided.'}</p>
        </div>

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Status" value={role.isDeleted ? 'Deleted' : 'Active'} />
          <Field label="Created" value={new Date(role.createdAtUtc).toLocaleDateString()} />
          <Field
            label="Last updated"
            value={role.updatedAtUtc ? new Date(role.updatedAtUtc).toLocaleDateString() : '—'}
          />
          <Field label="Assigned users" value={String(assignedUsers.length)} />
        </div>
      </div>

      {assignedUsers.length > 0 && (
        <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
          <div className="border-b border-hairline px-4 py-3 text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            Users with this role
          </div>
          <ul className="divide-y divide-hairline">
            {assignedUsers.map((u) => (
              <li key={u.id} className="px-4 py-3 text-sm">
                <Link to={`/admin/users/${u.id}/edit`} className="font-medium text-ink hover:text-primary-hover">
                  {u.userName}
                </Link>
                <span className="ml-2 text-ink-subtle">{u.email}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
