import { useCallback, useEffect, useState } from 'react'
import type { TaskAttachment } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { taskRepository } from '../api'

interface UseTaskAttachmentsResult {
  attachments: TaskAttachment[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useTaskAttachments(taskId: string | undefined): UseTaskAttachmentsResult {
  const [attachments, setAttachments] = useState<TaskAttachment[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!taskId) return
    let cancelled = false
    taskRepository
      .getAttachments(taskId)
      .then((data) => {
        if (!cancelled) setAttachments(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load attachments.')
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

  return { attachments, isLoading, error, refresh }
}
