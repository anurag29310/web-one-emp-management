import { useCallback, useEffect, useState } from 'react'
import type { AuthenticatedUser } from '@/app/shared/models/user'
import { AppError } from '@/app/shared/models/appError'
import { authRepository } from '../api'

interface UseCurrentUserResult {
  currentUser: AuthenticatedUser | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

/** Wraps GET /auth/me — used by ProfilePage to show account details (including isMfaEnabled) and to re-fetch after password/MFA changes. */
export function useCurrentUser(): UseCurrentUserResult {
  const [currentUser, setCurrentUser] = useState<AuthenticatedUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    authRepository
      .getCurrentUser()
      .then((data) => {
        if (!cancelled) setCurrentUser(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load your profile.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { currentUser, isLoading, error, refresh }
}
