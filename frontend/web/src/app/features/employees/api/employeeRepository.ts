import type {
  CreateEmployeeInput,
  Employee,
  EmployeeListFilters,
  UpdateEmployeeInput,
  UpdateEmployeeStatusInput,
} from '@/app/shared/models/employee'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'

export interface EmployeeRepository {
  list(filters?: EmployeeListFilters): Promise<PagedResult<Employee>>
  getById(id: string): Promise<Employee>
  create(input: CreateEmployeeInput): Promise<Employee>
  update(input: UpdateEmployeeInput): Promise<Employee>
  updateStatus(input: UpdateEmployeeStatusInput): Promise<void>
  remove(id: string): Promise<void>
  restore(id: string): Promise<void>
}
