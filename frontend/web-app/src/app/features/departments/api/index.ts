import { selectRepository } from '@/app/core/config/selectRepository'
import { mockDepartmentRepository } from './mockDepartmentRepository'
import { apiDepartmentRepository } from './apiDepartmentRepository'
import type { DepartmentRepository } from './departmentRepository'

export const departmentRepository: DepartmentRepository = selectRepository({
  mock: mockDepartmentRepository,
  api: apiDepartmentRepository,
})

export type {
  Department,
  DepartmentRepository,
  CreateDepartmentInput,
  UpdateDepartmentInput,
} from './departmentRepository'
