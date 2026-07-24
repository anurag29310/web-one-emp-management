import { selectRepository } from '@/app/core/config/selectRepository'
import { mockAuditLogRepository } from './mockAuditLogRepository'
import { apiAuditLogRepository } from './apiAuditLogRepository'
import type { AuditLogRepository } from './auditLogRepository'

export const auditLogRepository: AuditLogRepository = selectRepository({
  mock: mockAuditLogRepository,
  api: apiAuditLogRepository,
})

export type { AuditLog, AuditLogListFilters, AuditLogRepository } from './auditLogRepository'
