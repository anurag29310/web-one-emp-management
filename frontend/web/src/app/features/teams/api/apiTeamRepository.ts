import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { Employee } from '@/app/shared/models/employee'
import type { CreateTeamInput, Team, TeamRepository, UpdateTeamInput } from './teamRepository'

export const apiTeamRepository: TeamRepository = {
  async list(): Promise<Team[]> {
    const response = await httpClient.get<{ data: Team[] }>('/teams')
    return unwrap(response)
  },

  async getById(id: string): Promise<Team> {
    const response = await httpClient.get<{ data: Team }>(`/teams/${id}`)
    return unwrap(response)
  },

  async create(input: CreateTeamInput): Promise<Team> {
    const response = await httpClient.post<{ data: Team }>('/teams', input)
    return unwrap(response)
  },

  async update(input: UpdateTeamInput): Promise<Team> {
    const response = await httpClient.put<{ data: Team }>(`/teams/${input.id}`, input)
    return unwrap(response)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/teams/${id}`)
  },

  async listEmployees(teamId: string, page = 1, pageSize = 100): Promise<Employee[]> {
    const response = await httpClient.get<{ data: Employee[] }>(`/teams/${teamId}/employees`, {
      params: { page, pageSize },
    })
    return unwrap(response)
  },
}
