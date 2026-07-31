import { useCallback, useEffect, useState } from 'react'
import type { Client } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { clientRepository } from '../api'

interface UseClientResult {
  client: Client | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useClient(id: string | undefined): UseClientResult {
  const [client, setClient] = useState<Client | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    clientRepository
      .getById(id)
      .then((data) => {
        if (!cancelled) setClient(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load client.')
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

  return { client, isLoading, error, refresh }
}
