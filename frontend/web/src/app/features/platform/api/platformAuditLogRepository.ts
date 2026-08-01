import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type { AuditLog } from '@/app/features/audit-logs/api'

/**
 * Contract for GET /platform/audit-logs (docs/api-specification.md §27.5) — the same shape as
 * the tenant-scoped GET /audit-logs (§12), but with an explicit optional companyId filter since a
 * Super Admin has no "own company" to scope to by default.
 */
export interface PlatformAuditLogFilters {
  companyId?: string
  userId?: string
  entityName?: string
  entityId?: string
  action?: string
  dateFrom?: string
  dateTo?: string
  page?: number
  pageSize?: number
}

export interface PlatformAuditLogRepository {
  list(filters?: PlatformAuditLogFilters): Promise<PagedResult<AuditLog>>
}
