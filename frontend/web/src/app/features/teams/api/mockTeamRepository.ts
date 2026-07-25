import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { Employee } from '@/app/shared/models/employee'
import { mockEmployees } from '@/app/features/employees/api/mockData'
import { mockDepartments } from '@/app/features/departments/api/mockData'
import type { CreateTeamInput, Team, TeamRepository, UpdateTeamInput } from './teamRepository'
import { mockTeamEmployeeIds, mockTeams } from './mockData'

let teams = [...mockTeams]

function nextId(): string {
  return `20000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

function codeConflict(departmentId: string, code: string, excludeId?: string): boolean {
  return teams.some(
    (t) =>
      !t.isDeleted &&
      t.id !== excludeId &&
      t.departmentId === departmentId &&
      t.code.toLowerCase() === code.toLowerCase(),
  )
}

export const mockTeamRepository: TeamRepository = {
  async list(): Promise<Team[]> {
    await delay(250)
    return teams.filter((t) => !t.isDeleted)
  },

  async getById(id: string): Promise<Team> {
    await delay(200)
    const team = teams.find((t) => t.id === id)
    if (!team) {
      throw new AppError(`Team ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return team
  },

  async create(input: CreateTeamInput): Promise<Team> {
    await delay(300)
    if (codeConflict(input.departmentId, input.code)) {
      throw new AppError('Team code already exists in this department.', 409, 'CONFLICT')
    }
    const department = mockDepartments.find((d) => d.id === input.departmentId)
    const team: Team = {
      id: nextId(),
      departmentId: input.departmentId,
      departmentName: department?.name ?? null,
      name: input.name,
      code: input.code,
      leadEmployeeId: input.leadEmployeeId ?? null,
      isDeleted: false,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    teams = [...teams, team]
    return team
  },

  async update(input: UpdateTeamInput): Promise<Team> {
    await delay(300)
    const existing = teams.find((t) => t.id === input.id)
    if (!existing) {
      throw new AppError(`Team ${input.id} was not found.`, 404, 'NOT_FOUND')
    }
    if (codeConflict(input.departmentId, input.code, input.id)) {
      throw new AppError('Team code already exists in this department.', 409, 'CONFLICT')
    }
    const department = mockDepartments.find((d) => d.id === input.departmentId)
    const updated: Team = {
      ...existing,
      departmentId: input.departmentId,
      departmentName: department?.name ?? null,
      name: input.name,
      code: input.code,
      leadEmployeeId: input.leadEmployeeId ?? null,
      updatedAtUtc: new Date().toISOString(),
    }
    teams = teams.map((t) => (t.id === input.id ? updated : t))
    return updated
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    const existing = teams.find((t) => t.id === id)
    if (!existing) {
      throw new AppError(`Team ${id} was not found.`, 404, 'NOT_FOUND')
    }
    teams = teams.map((t) => (t.id === id ? { ...t, isDeleted: true } : t))
  },

  async listEmployees(teamId: string): Promise<Employee[]> {
    await delay(200)
    const ids = mockTeamEmployeeIds[teamId] ?? []
    return mockEmployees.filter((e) => ids.includes(e.id))
  },
}
