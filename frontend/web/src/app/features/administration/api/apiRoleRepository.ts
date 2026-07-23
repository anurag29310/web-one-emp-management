import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { Role, RoleListFilters, RoleRepository } from './roleRepository'

export const apiRoleRepository: RoleRepository = {
  async list(filters?: RoleListFilters): Promise<Role[]> {
    const response = await httpClient.get<{ data: Role[] }>('/roles', { params: filters })
    return unwrap(response)
  },

  async getById(id: string): Promise<Role> {
    const response = await httpClient.get<{ data: Role }>(`/roles/${id}`)
    return unwrap(response)
  },
}
