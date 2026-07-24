import { useCallback, useEffect, useState } from 'react'
import type { EmployeeDocument } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { documentRepository } from '../api'

interface UseEmployeeDocumentsResult {
  documents: EmployeeDocument[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useEmployeeDocuments(employeeId: string | undefined): UseEmployeeDocumentsResult {
  const [documents, setDocuments] = useState<EmployeeDocument[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!employeeId) return
    let cancelled = false
    documentRepository
      .list(employeeId)
      .then((data) => {
        if (!cancelled) setDocuments(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load documents.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [employeeId, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { documents, isLoading, error, refresh }
}
