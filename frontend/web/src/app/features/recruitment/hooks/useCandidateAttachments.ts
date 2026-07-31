import { useCallback, useEffect, useState } from 'react'
import type { CandidateAttachment } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { recruitmentRepository } from '../api'

interface UseCandidateAttachmentsResult {
  attachments: CandidateAttachment[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useCandidateAttachments(candidateId: string | undefined): UseCandidateAttachmentsResult {
  const [attachments, setAttachments] = useState<CandidateAttachment[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!candidateId) return
    let cancelled = false
    recruitmentRepository
      .getCandidateAttachments(candidateId)
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
  }, [candidateId, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { attachments, isLoading, error, refresh }
}
