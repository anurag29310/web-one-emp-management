import { useCallback, useEffect, useState } from 'react'
import type { Company, CompanyListFilters } from '../api'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { companyRepository } from '../api'

interface UseCompaniesResult {
  result: PagedResult<Company> | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useCompanies(filters: CompanyListFilters = {}): UseCompaniesResult {
  const [result, setResult] = useState<PagedResult<Company> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { page, pageSize, search, status } = filters

  useEffect(() => {
    let cancelled = false
    companyRepository
      .list({ page, pageSize, search, status })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load companies.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, search, status, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { result, isLoading, error, refresh }
}
