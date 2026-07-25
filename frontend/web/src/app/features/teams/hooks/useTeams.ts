import { useCallback, useEffect, useState } from 'react'
import type { Team } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { teamRepository } from '../api'

interface UseTeamsResult {
  teams: Team[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useTeams(): UseTeamsResult {
  const [teams, setTeams] = useState<Team[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    teamRepository
      .list()
      .then((data) => {
        if (!cancelled) setTeams(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load teams.')
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

  return { teams, isLoading, error, refresh }
}
