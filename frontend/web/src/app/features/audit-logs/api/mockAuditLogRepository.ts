import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type { AuditLog, AuditLogListFilters, AuditLogRepository } from './auditLogRepository'
import { mockAuditLogs } from './mockData'

export const mockAuditLogRepository: AuditLogRepository = {
  async list(filters: AuditLogListFilters = {}): Promise<PagedResult<AuditLog>> {
    await delay(300)
    const { page = 1, pageSize = 20, userId, entityName, entityId, action, dateFrom, dateTo } = filters

    let filtered = mockAuditLogs
    if (userId) {
      filtered = filtered.filter((log) => log.userId === userId)
    }
    if (entityName) {
      filtered = filtered.filter((log) => log.entityName.toLowerCase() === entityName.toLowerCase())
    }
    if (entityId) {
      filtered = filtered.filter((log) => log.entityId === entityId)
    }
    if (action) {
      filtered = filtered.filter((log) => log.action.toLowerCase() === action.toLowerCase())
    }
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

  async getById(id: string): Promise<AuditLog> {
    await delay(200)
    const log = mockAuditLogs.find((l) => l.id === id)
    if (!log) {
      throw new AppError(`Audit log ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return log
  },
}
