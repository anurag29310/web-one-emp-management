import { useCallback, useEffect, useState } from 'react'
import type { Role } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { roleRepository } from '../api'

interface UseRoleResult {
  role: Role | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useRole(id: string | undefined): UseRoleResult {
  const [role, setRole] = useState<Role | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    roleRepository
      .getById(id)
      .then((data) => {
        if (!cancelled) setRole(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load role.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [id, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { role, isLoading, error, refresh }
}
