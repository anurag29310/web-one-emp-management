import { useCallback, useEffect, useState } from 'react'
import type { ReimbursementAttachment } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { reimbursementRepository } from '../api'

interface UseReimbursementAttachmentsResult {
  attachments: ReimbursementAttachment[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useReimbursementAttachments(reimbursementId: string | undefined): UseReimbursementAttachmentsResult {
  const [attachments, setAttachments] = useState<ReimbursementAttachment[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!reimbursementId) return
    let cancelled = false
    reimbursementRepository
      .getAttachments(reimbursementId)
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
  }, [reimbursementId, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { attachments, isLoading, error, refresh }
}
