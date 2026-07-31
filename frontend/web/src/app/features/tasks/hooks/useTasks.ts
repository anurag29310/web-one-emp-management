import { useCallback, useEffect, useState } from 'react'
import type { TaskItem, TaskListFilters } from '../api'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { taskRepository } from '../api'

interface UseTasksResult {
  result: PagedResult<TaskItem> | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useTasks(filters: TaskListFilters = {}): UseTasksResult {
  const [result, setResult] = useState<PagedResult<TaskItem> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { page, pageSize, assignedEmployeeId, clientId, status, priority } = filters

  useEffect(() => {
    let cancelled = false
    taskRepository
      .list({ page, pageSize, assignedEmployeeId, clientId, status, priority })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load tasks.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, assignedEmployeeId, clientId, status, priority, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { result, isLoading, error, refresh }
}
