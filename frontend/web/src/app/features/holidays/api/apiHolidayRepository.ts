import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type {
  CreateHolidayInput,
  Holiday,
  HolidayListFilters,
  HolidayRepository,
  UpdateHolidayInput,
} from './holidayRepository'

export const apiHolidayRepository: HolidayRepository = {
  async list(filters?: HolidayListFilters): Promise<Holiday[]> {
    const response = await httpClient.get<{ data: Holiday[] }>('/holidays', { params: filters })
    return unwrap(response)
  },

  async getById(id: string): Promise<Holiday> {
    const response = await httpClient.get<{ data: Holiday }>(`/holidays/${id}`)
    return unwrap(response)
  },

  async create(input: CreateHolidayInput): Promise<Holiday> {
    const response = await httpClient.post<{ data: Holiday }>('/holidays', input)
    return unwrap(response)
  },

  async update(input: UpdateHolidayInput): Promise<Holiday> {
    const response = await httpClient.put<{ data: Holiday }>(`/holidays/${input.id}`, input)
    return unwrap(response)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/holidays/${id}`)
  },
}
