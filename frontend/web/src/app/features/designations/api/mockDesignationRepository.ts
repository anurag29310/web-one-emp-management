import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type {
  CreateDesignationInput,
  Designation,
  DesignationRepository,
  UpdateDesignationInput,
} from './designationRepository'
import { mockDesignations } from './mockData'

let designations = [...mockDesignations]

function nextId(): string {
  return `20000000-0000-0000-0001-${Date.now().toString().padStart(12, '0')}`
}

export const mockDesignationRepository: DesignationRepository = {
  async list(): Promise<Designation[]> {
    await delay(250)
    return designations.filter((d) => !d.isDeleted)
  },

  async getById(id: string): Promise<Designation> {
    await delay(200)
    const designation = designations.find((d) => d.id === id)
    if (!designation) {
      throw new AppError(`Designation ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return designation
  },

  async create(input: CreateDesignationInput): Promise<Designation> {
    await delay(300)
    const designation: Designation = {
      id: nextId(),
      name: input.name,
      code: input.code,
      level: input.level ?? null,
      isDeleted: false,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    designations = [...designations, designation]
    return designation
  },

  async update(input: UpdateDesignationInput): Promise<Designation> {
    await delay(300)
    const existing = designations.find((d) => d.id === input.id)
    if (!existing) {
      throw new AppError(`Designation ${input.id} was not found.`, 404, 'NOT_FOUND')
    }
    const updated: Designation = {
      ...existing,
      name: input.name,
      code: input.code,
      level: input.level ?? null,
      updatedAtUtc: new Date().toISOString(),
    }
    designations = designations.map((d) => (d.id === input.id ? updated : d))
    return updated
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    const existing = designations.find((d) => d.id === id)
    if (!existing) {
      throw new AppError(`Designation ${id} was not found.`, 404, 'NOT_FOUND')
    }
    designations = designations.map((d) => (d.id === id ? { ...d, isDeleted: true } : d))
  },
}
