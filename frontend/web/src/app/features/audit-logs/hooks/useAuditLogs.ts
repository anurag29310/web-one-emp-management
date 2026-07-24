import { useEffect, useState } from 'react'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import type { AuditLog, AuditLogListFilters } from '../api'
import { auditLogRepository } from '../api'

interface UseAuditLogsResult {
  result: PagedResult<AuditLog> | null
  isLoading: boolean
  error: string | null
}

export function useAuditLogs(filters: AuditLogListFilters = {}): UseAuditLogsResult {
  const [result, setResult] = useState<PagedResult<AuditLog> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const { page, pageSize, userId, entityName, entityId, action, dateFrom, dateTo } = filters

  useEffect(() => {
    let cancelled = false
    auditLogRepository
      .list({ page, pageSize, userId, entityName, entityId, action, dateFrom, dateTo })
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
  }, [page, pageSize, userId, entityName, entityId, action, dateFrom, dateTo])

  return { result, isLoading, error }
}
