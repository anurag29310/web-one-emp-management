import { useCallback, useEffect, useState } from 'react'
import type { Client, ClientListFilters } from '../api'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { clientRepository } from '../api'

interface UseClientsResult {
  result: PagedResult<Client> | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useClients(filters: ClientListFilters = {}): UseClientsResult {
  const [result, setResult] = useState<PagedResult<Client> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { page, pageSize, search, isActive } = filters

  useEffect(() => {
    let cancelled = false
    clientRepository
      .list({ page, pageSize, search, isActive })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load clients.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, search, isActive, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { result, isLoading, error, refresh }
}
