import type { Employee, EmployeeListFilters } from '@/app/shared/models/employee'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'

export interface EmployeeRepository {
  list(filters?: EmployeeListFilters): Promise<PagedResult<Employee>>
  getById(id: string): Promise<Employee>
}
