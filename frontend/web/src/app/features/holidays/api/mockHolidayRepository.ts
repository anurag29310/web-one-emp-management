import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type {
  CreateHolidayInput,
  Holiday,
  HolidayListFilters,
  HolidayRepository,
  UpdateHolidayInput,
} from './holidayRepository'
import { mockHolidays } from './mockData'

let holidays = [...mockHolidays]
const deletedIds = new Set<string>()

function nextId(): string {
  return `60000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

export const mockHolidayRepository: HolidayRepository = {
  async list(filters: HolidayListFilters = {}): Promise<Holiday[]> {
    await delay(250)
    const { officeLocationId, year, isOptional } = filters
    let filtered = holidays.filter((h) => !deletedIds.has(h.id))
    if (officeLocationId) filtered = filtered.filter((h) => h.officeLocationId === officeLocationId)
    if (year) filtered = filtered.filter((h) => new Date(h.holidayDate).getFullYear() === year)
    if (isOptional !== undefined) filtered = filtered.filter((h) => h.isOptional === isOptional)
    return [...filtered].sort((a, b) => a.holidayDate.localeCompare(b.holidayDate))
  },

  async getById(id: string): Promise<Holiday> {
    await delay(200)
    const holiday = holidays.find((h) => h.id === id)
    if (!holiday) {
      throw new AppError(`Holiday ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return holiday
  },

  async create(input: CreateHolidayInput): Promise<Holiday> {
    await delay(300)
    const holiday: Holiday = {
      id: nextId(),
      name: input.name,
      officeLocationId: input.officeLocationId ?? null,
      holidayDate: input.holidayDate,
      isOptional: input.isOptional,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    holidays = [...holidays, holiday]
    return holiday
  },

  async update(input: UpdateHolidayInput): Promise<Holiday> {
    await delay(300)
    const existing = holidays.find((h) => h.id === input.id)
    if (!existing) {
      throw new AppError(`Holiday ${input.id} was not found.`, 404, 'NOT_FOUND')
    }
    const updated: Holiday = {
      ...existing,
      name: input.name,
      officeLocationId: input.officeLocationId ?? null,
      holidayDate: input.holidayDate,
      isOptional: input.isOptional,
      updatedAtUtc: new Date().toISOString(),
    }
    holidays = holidays.map((h) => (h.id === input.id ? updated : h))
    return updated
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    deletedIds.add(id)
  },
}
