import { useCallback, useEffect, useState } from 'react'
import type { Team } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { teamRepository } from '../api'

interface UseTeamResult {
  team: Team | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useTeam(id: string | undefined): UseTeamResult {
  const [team, setTeam] = useState<Team | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    teamRepository
      .getById(id)
      .then((data) => {
        if (!cancelled) setTeam(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load team.')
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

  return { team, isLoading, error, refresh }
}
