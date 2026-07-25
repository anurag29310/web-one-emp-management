import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type {
  CreateOfficeLocationInput,
  OfficeLocation,
  OfficeLocationRepository,
  UpdateOfficeLocationInput,
} from './officeLocationRepository'
import { mockOfficeLocations } from './mockData'

let officeLocations = [...mockOfficeLocations]

function nextId(): string {
  return `20000000-0000-0000-0005-${Date.now().toString().padStart(12, '0')}`
}

export const mockOfficeLocationRepository: OfficeLocationRepository = {
  async list(): Promise<OfficeLocation[]> {
    await delay(200)
    return officeLocations.filter((location) => !location.isDeleted)
  },

  async getById(id: string): Promise<OfficeLocation> {
    await delay(200)
    const location = officeLocations.find((l) => l.id === id)
    if (!location) {
      throw new AppError(`Office location ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return location
  },

  async create(input: CreateOfficeLocationInput): Promise<OfficeLocation> {
    await delay(300)
    const location: OfficeLocation = {
      id: nextId(),
      name: input.name,
      code: input.code,
      addressLine1: input.addressLine1 ?? null,
      addressLine2: input.addressLine2 ?? null,
      city: input.city,
      state: input.state ?? null,
      country: input.country,
      timeZoneId: input.timeZoneId,
      isDeleted: false,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    officeLocations = [...officeLocations, location]
    return location
  },

  async update(input: UpdateOfficeLocationInput): Promise<OfficeLocation> {
    await delay(300)
    const existing = officeLocations.find((l) => l.id === input.id)
    if (!existing) {
      throw new AppError(`Office location ${input.id} was not found.`, 404, 'NOT_FOUND')
    }
    const updated: OfficeLocation = {
      ...existing,
      name: input.name,
      code: input.code,
      addressLine1: input.addressLine1 ?? null,
      addressLine2: input.addressLine2 ?? null,
      city: input.city,
      state: input.state ?? null,
      country: input.country,
      timeZoneId: input.timeZoneId,
      updatedAtUtc: new Date().toISOString(),
    }
    officeLocations = officeLocations.map((l) => (l.id === input.id ? updated : l))
    return updated
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    const existing = officeLocations.find((l) => l.id === id)
    if (!existing) {
      throw new AppError(`Office location ${id} was not found.`, 404, 'NOT_FOUND')
    }
    officeLocations = officeLocations.map((l) => (l.id === id ? { ...l, isDeleted: true } : l))
  },
}
