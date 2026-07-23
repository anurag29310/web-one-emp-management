import type { PagedResult } from '@/app/shared/models/apiEnvelope'

/**
 * Contract for /audit-logs (api-specification.md §12), cross-checked against
 * backend/EMS.API/Controllers/AuditLogsController.cs and
 * EMS.Application/Features/AuditLogs/DTOs/AuditLogDto.cs. The list and
 * get-by-id endpoints both require the CanViewAuditLogs policy (Admin only) —
 * enforced server-side; the frontend additionally hides the nav entry and
 * route content for non-Admin users, see useAuth().user.role.
 */
export interface AuditLog {
  id: string
  userId: string | null
  entityName: string
  entityId: string | null
  action: string
  oldValuesJson: string | null
  newValuesJson: string | null
  ipAddress: string | null
  userAgent: string | null
  createdAtUtc: string
}

export interface AuditLogListFilters {
  userId?: string
  entityName?: string
  entityId?: string
  action?: string
  dateFrom?: string
  dateTo?: string
  page?: number
  pageSize?: number
}

export interface AuditLogRepository {
  list(filters?: AuditLogListFilters): Promise<PagedResult<AuditLog>>
  getById(id: string): Promise<AuditLog>
}
