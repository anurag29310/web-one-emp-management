import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useUsers } from '../hooks/useUsers'
import { useRoles } from '../hooks/useRoles'
import { userRepository } from '../api'
import { AdminSubNav } from '../components/AdminSubNav'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { StatusBadge } from '@/app/shared/components/StatusBadge'
import { Avatar } from '@/app/shared/components/Avatar'

const ACTIVE_FILTER_OPTIONS = [
  { value: '', label: 'All statuses' },
  { value: 'true', label: 'Active' },
  { value: 'false', label: 'Inactive' },
] as const

export function UserListPage() {
  const { user: currentUser } = useAuth()
  const isAdmin = currentUser?.role === 'Admin'

  const [search, setSearch] = useState('')
  const [roleId, setRoleId] = useState('')
  const [activeFilter, setActiveFilter] = useState<(typeof ACTIVE_FILTER_OPTIONS)[number]['value']>('')
  const [actionError, setActionError] = useState<string | null>(null)
  const [pendingId, setPendingId] = useState<string | null>(null)

  const { roles } = useRoles()
  const { users, isLoading, error, refresh } = useUsers({
    roleId: roleId || undefined,
    isActive: activeFilter === '' ? undefined : activeFilter === 'true',
  })

  if (!isAdmin) {
    return (
      <div className="space-y-4">
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">
          Administration
        </h1>
        <div className="rounded-lg border border-hairline bg-surface-1 p-6">
          <p className="text-sm text-danger">You don't have permission to view user accounts.</p>
        </div>
      </div>
    )
  }

  const filteredUsers = search
    ? users.filter((u) => {
        const term = search.toLowerCase()
        return u.userName.toLowerCase().includes(term) || u.email.toLowerCase().includes(term)
      })
    : users

  async function handleStatusToggle(userId: string, nextActive: boolean) {
    setActionError(null)
    setPendingId(userId)
    try {
      await userRepository.updateStatus({ id: userId, isActive: nextActive })
      refresh()
    } catch (err) {
      setActionError(err instanceof AppError ? err.message : 'Failed to update user status.')
    } finally {
      setPendingId(null)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">
            Administration
          </h1>
          <p className="text-sm text-ink-subtle">{users.length} user accounts</p>
        </div>
        <Link
          to="/admin/users/new"
          className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
        >
          New user
        </Link>
      </div>

      <AdminSubNav />

      <div className="flex flex-wrap items-center gap-3">
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search by username or email…"
          className="w-64 rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        />
        <select
          value={roleId}
          onChange={(e) => setRoleId(e.target.value)}
          className="rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          aria-label="Filter by role"
        >
          <option value="">All roles</option>
          {roles.map((role) => (
            <option key={role.id} value={role.id}>
              {role.name}
            </option>
          ))}
        </select>
        <select
          value={activeFilter}
          onChange={(e) => setActiveFilter(e.target.value as (typeof ACTIVE_FILTER_OPTIONS)[number]['value'])}
          className="rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          aria-label="Filter by status"
        >
          {ACTIVE_FILTER_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>

      {(error || actionError) && (
        <p role="alert" className="text-sm text-danger">
          {error ?? actionError}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">User</th>
              <th className="px-4 py-3">Role</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 4 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={4}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && filteredUsers.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={4}>
                  No users match your filters.
                </td>
              </tr>
            )}

            {!isLoading &&
              filteredUsers.map((u) => (
                <tr key={u.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link to={`/admin/users/${u.id}/edit`} className="flex items-center gap-3">
                      <Avatar name={u.userName} size="sm" />
                      <div className="min-w-0">
                        <p className="font-medium text-ink hover:text-primary-hover">{u.userName}</p>
                        <p className="truncate text-xs text-ink-subtle">{u.email}</p>
                      </div>
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{u.roleName ?? '—'}</td>
                  <td className="px-4 py-3">
                    <StatusBadge status={u.isActive ? 'Active' : 'Inactive'} />
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex items-center justify-end gap-3">
                      <Link
                        to={`/admin/users/${u.id}/edit`}
                        className="text-xs font-medium text-primary-hover hover:underline"
                      >
                        Edit
                      </Link>
                      <button
                        type="button"
                        disabled={pendingId === u.id}
                        onClick={() => void handleStatusToggle(u.id, !u.isActive)}
                        className="text-xs font-medium text-ink-subtle hover:text-ink disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        {pendingId === u.id ? 'Saving…' : u.isActive ? 'Deactivate' : 'Activate'}
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
