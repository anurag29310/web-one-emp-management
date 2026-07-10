import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type {
  CreateDepartmentInput,
  Department,
  DepartmentRepository,
  UpdateDepartmentInput,
} from './departmentRepository'
import { mockDepartments } from './mockData'

let departments = [...mockDepartments]

function nextId(): string {
  return `20000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

export const mockDepartmentRepository: DepartmentRepository = {
  async list(): Promise<Department[]> {
    await delay(250)
    return departments.filter((d) => !d.isDeleted)
  },

  async getById(id: string): Promise<Department> {
    await delay(200)
    const department = departments.find((d) => d.id === id)
    if (!department) {
      throw new AppError(`Department ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return department
  },

  async create(input: CreateDepartmentInput): Promise<Department> {
    await delay(300)
    const department: Department = {
      id: nextId(),
      name: input.name,
      code: input.code ?? null,
      description: input.description ?? null,
      headEmployeeId: input.headEmployeeId ?? null,
      isDeleted: false,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    departments = [...departments, department]
    return department
  },

  async update(input: UpdateDepartmentInput): Promise<Department> {
    await delay(300)
    const existing = departments.find((d) => d.id === input.id)
    if (!existing) {
      throw new AppError(`Department ${input.id} was not found.`, 404, 'NOT_FOUND')
    }
    const updated: Department = {
      ...existing,
      name: input.name,
      code: input.code ?? null,
      description: input.description ?? null,
      headEmployeeId: input.headEmployeeId ?? null,
      updatedAtUtc: new Date().toISOString(),
    }
    departments = departments.map((d) => (d.id === input.id ? updated : d))
    return updated
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    departments = departments.map((d) => (d.id === id ? { ...d, isDeleted: true } : d))
  },

  async restore(id: string): Promise<void> {
    await delay(200)
    departments = departments.map((d) => (d.id === id ? { ...d, isDeleted: false } : d))
  },
}
