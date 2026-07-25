import type { Employee } from '@/app/shared/models/employee'

export interface Team {
  id: string
  departmentId: string
  departmentName: string | null
  name: string
  code: string
  leadEmployeeId: string | null
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface CreateTeamInput {
  departmentId: string
  name: string
  code: string
  leadEmployeeId?: string
}

export interface UpdateTeamInput extends CreateTeamInput {
  id: string
}

export interface TeamRepository {
  list(): Promise<Team[]>
  getById(id: string): Promise<Team>
  create(input: CreateTeamInput): Promise<Team>
  update(input: UpdateTeamInput): Promise<Team>
  remove(id: string): Promise<void>
  listEmployees(teamId: string, page?: number, pageSize?: number): Promise<Employee[]>
}
