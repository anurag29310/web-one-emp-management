import { useCallback, useEffect, useState } from 'react'
import type { Role, RoleListFilters } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { roleRepository } from '../api'

interface UseRolesResult {
  roles: Role[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useRoles(filters: RoleListFilters = {}): UseRolesResult {
  const [roles, setRoles] = useState<Role[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { includeDeleted } = filters

  useEffect(() => {
    let cancelled = false
    roleRepository
      .list({ includeDeleted })
      .then((data) => {
        if (!cancelled) setRoles(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load roles.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [includeDeleted, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { roles, isLoading, error, refresh }
}
