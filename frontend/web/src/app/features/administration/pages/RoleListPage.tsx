import { Link } from 'react-router-dom'
import { useRoles } from '../hooks/useRoles'
import { AdminSubNav } from '../components/AdminSubNav'
import { useAuth } from '@/app/core/auth/useAuth'

export function RoleListPage() {
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'
  const canView = isAdmin || user?.role === 'HR'

  const { roles, isLoading, error } = useRoles()

  if (!canView) {
    return (
      <div className="space-y-4">
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">
          Administration
        </h1>
        <div className="rounded-lg border border-hairline bg-surface-1 p-6">
          <p className="text-sm text-danger">You don't have permission to view roles.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Administration</h1>
        <p className="text-sm text-ink-subtle">{roles.length} roles</p>
      </div>

      {isAdmin && <AdminSubNav />}

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Description</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 4 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={2}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && roles.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={2}>
                  No roles found.
                </td>
              </tr>
            )}

            {!isLoading &&
              roles.map((role) => (
                <tr key={role.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3 font-medium text-ink">
                    {isAdmin ? (
                      <Link to={`/admin/roles/${role.id}`} className="hover:text-primary-hover">
                        {role.name}
                      </Link>
                    ) : (
                      role.name
                    )}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{role.description ?? '—'}</td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
