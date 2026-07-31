import { useCallback, useEffect, useState } from 'react'
import type { TaskItem } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { taskRepository } from '../api'

interface UseTaskResult {
  task: TaskItem | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useTask(id: string | undefined): UseTaskResult {
  const [task, setTask] = useState<TaskItem | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    taskRepository
      .getById(id)
      .then((data) => {
        if (!cancelled) setTask(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load task.')
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

  return { task, isLoading, error, refresh }
}
