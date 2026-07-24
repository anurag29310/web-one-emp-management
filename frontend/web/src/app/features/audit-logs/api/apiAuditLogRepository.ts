import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { ApiSuccessEnvelope, PagedResult } from '@/app/shared/models/apiEnvelope'
import type { AuditLog, AuditLogListFilters, AuditLogRepository } from './auditLogRepository'

/**
 * The backend wraps EMS.Application.Common.DTOs.PagedResult<T> a second time inside
 * ApiResponse<T> (see AuditLogsController.GetAll → ApiResponse<PagedResult<AuditLogDto>>.Success),
 * so a list response body is `{ data: { data: [...], page, pageSize, totalCount, totalPages },
 * message, correlationId }` — matches the shape confirmed against AttendanceController.
 */
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

export const apiAuditLogRepository: AuditLogRepository = {
  async list(filters: AuditLogListFilters = {}): Promise<PagedResult<AuditLog>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<AuditLog>>>('/audit-logs', {
      params: filters,
    })
    return unwrapPaged(response)
  },

  async getById(id: string): Promise<AuditLog> {
    const response = await httpClient.get<{ data: AuditLog }>(`/audit-logs/${id}`)
    return unwrap(response)
  },
}
