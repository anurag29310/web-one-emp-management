import { useCallback, useEffect, useState } from 'react'
import type { Department } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { departmentRepository } from '../api'

interface UseDepartmentsResult {
  departments: Department[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useDepartments(): UseDepartmentsResult {
  const [departments, setDepartments] = useState<Department[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    departmentRepository
      .list()
      .then((data) => {
        if (!cancelled) setDepartments(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load departments.')
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

  return { departments, isLoading, error, refresh }
}
