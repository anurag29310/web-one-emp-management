export interface Department {
  id: string
  name: string
  code: string | null
  description: string | null
  headEmployeeId: string | null
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface CreateDepartmentInput {
  name: string
  code?: string
  description?: string
  headEmployeeId?: string
}

export interface UpdateDepartmentInput extends CreateDepartmentInput {
  id: string
}

export interface DepartmentRepository {
  list(): Promise<Department[]>
  getById(id: string): Promise<Department>
  create(input: CreateDepartmentInput): Promise<Department>
  update(input: UpdateDepartmentInput): Promise<Department>
  remove(id: string): Promise<void>
  restore(id: string): Promise<void>
}
