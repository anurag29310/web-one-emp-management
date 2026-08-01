import { delay } from '@/app/shared/utils/delay'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type { AuditLog } from '@/app/features/audit-logs/api'
import { mockAuditLogs } from '@/app/features/audit-logs/api/mockData'
import type { PlatformAuditLogFilters, PlatformAuditLogRepository } from './platformAuditLogRepository'

export const mockPlatformAuditLogRepository: PlatformAuditLogRepository = {
  async list(filters: PlatformAuditLogFilters = {}): Promise<PagedResult<AuditLog>> {
    await delay(300)
    const { page = 1, pageSize = 20, companyId, userId, entityName, entityId, action, dateFrom, dateTo } = filters

    let filtered = mockAuditLogs
    if (companyId) filtered = filtered.filter((log) => log.companyId === companyId)
    if (userId) filtered = filtered.filter((log) => log.userId === userId)
    if (entityName) filtered = filtered.filter((log) => log.entityName.toLowerCase() === entityName.toLowerCase())
    if (entityId) filtered = filtered.filter((log) => log.entityId === entityId)
    if (action) filtered = filtered.filter((log) => log.action.toLowerCase() === action.toLowerCase())
    if (dateFrom) {
      const from = new Date(dateFrom).getTime()
      filtered = filtered.filter((log) => new Date(log.createdAtUtc).getTime() >= from)
    }
    if (dateTo) {
      const to = new Date(dateTo).getTime()
      filtered = filtered.filter((log) => new Date(log.createdAtUtc).getTime() <= to)
    }

    filtered = [...filtered].sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime())

    const start = (page - 1) * pageSize
    const pageItems = filtered.slice(start, start + pageSize)

    return {
      data: pageItems,
      page,
      pageSize,
      totalCount: filtered.length,
      totalPages: Math.max(1, Math.ceil(filtered.length / pageSize)),
      correlationId: 'mock-correlation-id',
    }
  },
}
