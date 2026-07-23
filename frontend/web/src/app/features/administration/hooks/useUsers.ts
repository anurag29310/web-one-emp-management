import { useCallback, useEffect, useState } from 'react'
import type { User, UserListFilters } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { userRepository } from '../api'

interface UseUsersResult {
  users: User[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

/**
 * @param enabled Set to `false` to skip the request entirely (e.g. when the
 * current user lacks the `CanManageUsers` policy that `GET /users` requires).
 */
export function useUsers(filters: UserListFilters = {}, enabled = true): UseUsersResult {
  const [users, setUsers] = useState<User[]>([])
  const [isLoading, setIsLoading] = useState(enabled)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { includeDeleted, roleId, isActive } = filters

  useEffect(() => {
    if (!enabled) return

    let cancelled = false
    userRepository
      .list({ includeDeleted, roleId, isActive })
      .then((data) => {
        if (!cancelled) setUsers(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load users.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [includeDeleted, roleId, isActive, enabled, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { users, isLoading: enabled && isLoading, error, refresh }
}
