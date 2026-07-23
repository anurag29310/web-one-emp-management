import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { ApiSuccessEnvelope, PagedResult } from '@/app/shared/models/apiEnvelope'
import type {
  AttendanceCorrection,
  AttendanceCorrectionListFilters,
  AttendanceRecord,
  AttendanceRecordListFilters,
  AttendanceRepository,
  CheckInInput,
  CheckOutInput,
  CorrectionDecisionInput,
  CreateAttendanceRecordInput,
  RequestCorrectionInput,
  UpdateAttendanceRecordInput,
} from './attendanceRepository'

/**
 * Shape of EMS.Application.Common.DTOs.PagedResult<T> as it actually serializes: the backend
 * wraps it a second time inside ApiResponse<T>, so a list endpoint's JSON body is
 * `{ data: { data: [...], page, pageSize, totalCount, totalPages }, message, correlationId }`
 * — the pagination fields live one level deeper than the flat `{ data, page, pageSize, ... }`
 * shape documented in api-specification.md §2.3. Confirmed against
 * AttendanceController.GetAll / GetCorrections, both of which return
 * `ApiResponse<PagedResult<TDto>>.Success(result)`.
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

export const apiAttendanceRepository: AttendanceRepository = {
  async checkIn(input: CheckInInput): Promise<AttendanceRecord> {
    const response = await httpClient.post<{ data: AttendanceRecord }>('/attendance/check-in', input)
    return unwrap(response)
  },

  async checkOut(input: CheckOutInput): Promise<AttendanceRecord> {
    const response = await httpClient.post<{ data: AttendanceRecord }>('/attendance/check-out', input)
    return unwrap(response)
  },

  async list(filters: AttendanceRecordListFilters = {}): Promise<PagedResult<AttendanceRecord>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<AttendanceRecord>>>(
      '/attendance',
      { params: filters },
    )
    return unwrapPaged(response)
  },

  async getById(id: string): Promise<AttendanceRecord> {
    const response = await httpClient.get<{ data: AttendanceRecord }>(`/attendance/${id}`)
    return unwrap(response)
  },

  async create(input: CreateAttendanceRecordInput): Promise<AttendanceRecord> {
    const response = await httpClient.post<{ data: AttendanceRecord }>('/attendance', input)
    return unwrap(response)
  },

  async update(input: UpdateAttendanceRecordInput): Promise<AttendanceRecord> {
    const { id, ...body } = input
    const response = await httpClient.put<{ data: AttendanceRecord }>(`/attendance/${id}`, body)
    return unwrap(response)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/attendance/${id}`)
  },

  async listCorrections(filters: AttendanceCorrectionListFilters = {}): Promise<PagedResult<AttendanceCorrection>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<AttendanceCorrection>>>(
      '/attendance/corrections',
      { params: filters },
    )
    return unwrapPaged(response)
  },

  async requestCorrection(input: RequestCorrectionInput): Promise<AttendanceCorrection> {
    const response = await httpClient.post<{ data: AttendanceCorrection }>('/attendance/corrections', input)
    return unwrap(response)
  },

  async approveCorrection(id: string, input?: CorrectionDecisionInput): Promise<void> {
    await httpClient.post(`/attendance/corrections/${id}/approve`, input ?? {})
  },

  async rejectCorrection(id: string, input?: CorrectionDecisionInput): Promise<void> {
    await httpClient.post(`/attendance/corrections/${id}/reject`, input ?? {})
  },
}
