import { useCallback, useEffect, useState } from 'react'
import type { User } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { userRepository } from '../api'

interface UseUserResult {
  user: User | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useUser(id: string | undefined): UseUserResult {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    userRepository
      .getById(id)
      .then((data) => {
        if (!cancelled) setUser(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load user.')
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

  return { user, isLoading, error, refresh }
}
