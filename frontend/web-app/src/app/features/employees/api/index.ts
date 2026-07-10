import { selectRepository } from '@/app/core/config/selectRepository'
import { mockEmployeeRepository } from './mockEmployeeRepository'
import { apiEmployeeRepository } from './apiEmployeeRepository'
import type { EmployeeRepository } from './employeeRepository'

export const employeeRepository: EmployeeRepository = selectRepository({
  mock: mockEmployeeRepository,
  api: apiEmployeeRepository,
})

export type { EmployeeRepository } from './employeeRepository'
