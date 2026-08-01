import { useCallback, useEffect, useState } from 'react'
import type { CompanyDetail } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { companyRepository } from '../api'

interface UseCompanyResult {
  company: CompanyDetail | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useCompany(id: string | undefined): UseCompanyResult {
  const [company, setCompany] = useState<CompanyDetail | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    companyRepository
      .getById(id)
      .then((data) => {
        if (!cancelled) setCompany(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load company.')
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

  return { company, isLoading, error, refresh }
}
