import { httpClient } from '@/app/core/api/httpClient'
import type { ApiSuccessEnvelope, PagedResult } from '@/app/shared/models/apiEnvelope'
import type { AuditLog } from '@/app/features/audit-logs/api'
import type { PlatformAuditLogFilters, PlatformAuditLogRepository } from './platformAuditLogRepository'

/** Same double-wrapped PagedResult shape documented in apiCompanyRepository.ts. */
interface BackendPagedResult<T> {
  data: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

function unwrapPaged<T>(response: { data: ApiSuccessEnvelope<BackendPagedResult<T>> }): PagedResult<T> {
  const envelope = response.data
  const paged = envelope.data
  return {
    data: paged.data,
    page: paged.page,
    pageSize: paged.pageSize,
    totalCount: paged.totalCount,
    totalPages: paged.totalPages,
    correlationId: envelope.correlationId,
  }
}

export const apiPlatformAuditLogRepository: PlatformAuditLogRepository = {
  async list(filters: PlatformAuditLogFilters = {}): Promise<PagedResult<AuditLog>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<AuditLog>>>('/platform/audit-logs', {
      params: filters,
    })
    return unwrapPaged(response)
  },
}
