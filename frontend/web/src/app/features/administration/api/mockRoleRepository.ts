import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { Role, RoleListFilters, RoleRepository } from './roleRepository'
import { mockRoles } from './mockRoleData'

const roles = [...mockRoles]

export const mockRoleRepository: RoleRepository = {
  async list(filters: RoleListFilters = {}): Promise<Role[]> {
    await delay(200)
    const { includeDeleted = false } = filters
    return includeDeleted ? roles : roles.filter((r) => !r.isDeleted)
  },

  async getById(id: string): Promise<Role> {
    await delay(150)
    const role = roles.find((r) => r.id === id)
    if (!role) {
      throw new AppError(`Role ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return role
  },
}
