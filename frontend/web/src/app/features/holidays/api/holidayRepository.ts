export interface Holiday {
  id: string
  name: string
  officeLocationId: string | null
  holidayDate: string
  isOptional: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface HolidayListFilters {
  officeLocationId?: string
  year?: number
  isOptional?: boolean
}

export interface CreateHolidayInput {
  name: string
  officeLocationId?: string
  holidayDate: string
  isOptional: boolean
}

export interface UpdateHolidayInput extends CreateHolidayInput {
  id: string
}

export interface HolidayRepository {
  list(filters?: HolidayListFilters): Promise<Holiday[]>
  getById(id: string): Promise<Holiday>
  create(input: CreateHolidayInput): Promise<Holiday>
  update(input: UpdateHolidayInput): Promise<Holiday>
  remove(id: string): Promise<void>
}
