import { selectRepository } from '@/app/core/config/selectRepository'
import { mockUserRepository } from './mockUserRepository'
import { apiUserRepository } from './apiUserRepository'
import { mockRoleRepository } from './mockRoleRepository'
import { apiRoleRepository } from './apiRoleRepository'
import type { UserRepository } from './userRepository'
import type { RoleRepository } from './roleRepository'

export const userRepository: UserRepository = selectRepository({
  mock: mockUserRepository,
  api: apiUserRepository,
})

export const roleRepository: RoleRepository = selectRepository({
  mock: mockRoleRepository,
  api: apiRoleRepository,
})

export type {
  User,
  UserRepository,
  UserListFilters,
  CreateUserInput,
  UpdateUserInput,
  UpdateUserStatusInput,
} from './userRepository'

export type { Role, RoleRepository, RoleListFilters } from './roleRepository'
