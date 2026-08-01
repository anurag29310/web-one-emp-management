import { useCallback, useEffect, useState } from 'react'
import type { PlatformAuditLogFilters } from '../api'
import type { AuditLog } from '@/app/features/audit-logs/api'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { platformAuditLogRepository } from '../api'

interface UsePlatformAuditLogsResult {
  result: PagedResult<AuditLog> | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function usePlatformAuditLogs(filters: PlatformAuditLogFilters = {}): UsePlatformAuditLogsResult {
  const [result, setResult] = useState<PagedResult<AuditLog> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { page, pageSize, companyId, entityName, action, dateFrom, dateTo } = filters

  useEffect(() => {
    let cancelled = false
    platformAuditLogRepository
      .list({ page, pageSize, companyId, entityName, action, dateFrom, dateTo })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load audit logs.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, companyId, entityName, action, dateFrom, dateTo, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { result, isLoading, error, refresh }
}
