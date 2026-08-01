import { useCallback, useEffect, useState } from 'react'
import type { PlatformSettings } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { platformSettingsRepository } from '../api'

interface UsePlatformSettingsResult {
  settings: PlatformSettings | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function usePlatformSettings(): UsePlatformSettingsResult {
  const [settings, setSettings] = useState<PlatformSettings | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    platformSettingsRepository
      .get()
      .then((data) => {
        if (!cancelled) setSettings(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load platform settings.')
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

  return { settings, isLoading, error, refresh }
}
