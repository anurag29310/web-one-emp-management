import { useCallback, useEffect, useState } from 'react'
import type { TaskComment } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { taskRepository } from '../api'

interface UseTaskCommentsResult {
  comments: TaskComment[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useTaskComments(taskId: string | undefined): UseTaskCommentsResult {
  const [comments, setComments] = useState<TaskComment[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!taskId) return
    let cancelled = false
    taskRepository
      .getComments(taskId)
      .then((data) => {
        if (!cancelled) setComments(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load comments.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [taskId, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { comments, isLoading, error, refresh }
}
