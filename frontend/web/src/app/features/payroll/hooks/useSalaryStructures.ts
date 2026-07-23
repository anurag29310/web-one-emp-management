import { useCallback, useEffect, useState } from 'react'
import type { SalaryStructure } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { payrollRepository } from '../api'

interface UseSalaryStructuresResult {
  salaryStructures: SalaryStructure[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useSalaryStructures(): UseSalaryStructuresResult {
  const [salaryStructures, setSalaryStructures] = useState<SalaryStructure[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    payrollRepository
      .listSalaryStructures()
      .then((data) => {
        if (!cancelled) setSalaryStructures(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load salary structures.')
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

  return { salaryStructures, isLoading, error, refresh }
}
