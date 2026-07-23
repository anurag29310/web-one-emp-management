import { useCallback, useEffect, useState } from 'react'
import type { Department } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { departmentRepository } from '../api'

interface UseDepartmentResult {
  department: Department | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useDepartment(id: string | undefined): UseDepartmentResult {
  const [department, setDepartment] = useState<Department | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    departmentRepository
      .getById(id)
      .then((data) => {
        if (!cancelled) setDepartment(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load department.')
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

  return { department, isLoading, error, refresh }
}
