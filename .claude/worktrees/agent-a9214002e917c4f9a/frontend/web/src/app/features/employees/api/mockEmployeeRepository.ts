import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { Employee, EmployeeListFilters } from '@/app/shared/models/employee'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type { EmployeeRepository } from './employeeRepository'
import { mockEmployees } from './mockData'

export const mockEmployeeRepository: EmployeeRepository = {
  async list(filters: EmployeeListFilters = {}): Promise<PagedResult<Employee>> {
    await delay(300)
    const { page = 1, pageSize = 20, search, departmentId, status } = filters

    let filtered = mockEmployees
    if (search) {
      const term = search.toLowerCase()
      filtered = filtered.filter(
        (e) =>
          e.fullName.toLowerCase().includes(term) || e.employeeCode.toLowerCase().includes(term),
      )
    }
    if (departmentId) {
      filtered = filtered.filter((e) => e.departmentId === departmentId)
    }
    if (status) {
      filtered = filtered.filter((e) => e.employmentStatus === status)
    }

    const start = (page - 1) * pageSize
    const pageItems = filtered.slice(start, start + pageSize)

    return {
      data: pageItems,
      page,
      pageSize,
      totalCount: filtered.length,
      totalPages: Math.max(1, Math.ceil(filtered.length / pageSize)),
      correlationId: 'mock-correlation-id',
    }
  },

  async getById(id: string): Promise<Employee> {
    await delay(200)
    const employee = mockEmployees.find((e) => e.id === id)
    if (!employee) {
      throw new AppError(`Employee ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return employee
  },
}
