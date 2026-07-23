import { selectRepository } from '@/app/core/config/selectRepository'
import { mockHolidayRepository } from './mockHolidayRepository'
import { apiHolidayRepository } from './apiHolidayRepository'
import type { HolidayRepository } from './holidayRepository'

export const holidayRepository: HolidayRepository = selectRepository({
  mock: mockHolidayRepository,
  api: apiHolidayRepository,
})

export type {
  Holiday,
  HolidayRepository,
  HolidayListFilters,
  CreateHolidayInput,
  UpdateHolidayInput,
} from './holidayRepository'
