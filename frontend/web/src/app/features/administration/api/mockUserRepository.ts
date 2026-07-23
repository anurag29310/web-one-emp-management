import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type {
  CreateUserInput,
  UpdateUserInput,
  UpdateUserStatusInput,
  User,
  UserListFilters,
  UserRepository,
} from './userRepository'
import { mockUsers } from './mockUserData'
import { mockRoles } from './mockRoleData'

let users = [...mockUsers]

function nextId(): string {
  return `00000000-0000-0000-0000-6${Date.now().toString().padStart(11, '0')}`
}

function roleName(roleId: string | undefined): string | null {
  if (!roleId) return null
  return mockRoles.find((r) => r.id === roleId)?.name ?? null
}

export const mockUserRepository: UserRepository = {
  async list(filters: UserListFilters = {}): Promise<User[]> {
    await delay(250)
    const { includeDeleted = false, roleId, isActive } = filters
    let filtered = includeDeleted ? users : users.filter((u) => !u.isDeleted)
    if (roleId) {
      filtered = filtered.filter((u) => u.roleId === roleId)
    }
    if (isActive !== undefined) {
      filtered = filtered.filter((u) => u.isActive === isActive)
    }
    return filtered
  },

  async getById(id: string): Promise<User> {
    await delay(200)
    const user = users.find((u) => u.id === id)
    if (!user) {
      throw new AppError(`User ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return user
  },

  async create(input: CreateUserInput): Promise<User> {
    await delay(300)
    if (users.some((u) => u.userName.toLowerCase() === input.userName.toLowerCase())) {
      throw new AppError('Username already exists.', 409, 'CONFLICT')
    }
    if (users.some((u) => u.email.toLowerCase() === input.email.toLowerCase())) {
      throw new AppError('Email already exists.', 409, 'CONFLICT')
    }
    const user: User = {
      id: nextId(),
      userName: input.userName,
      email: input.email,
      isActive: input.isActive ?? true,
      roleId: input.roleId ?? null,
      roleName: roleName(input.roleId),
      employeeId: input.employeeId ?? null,
      isDeleted: false,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    users = [...users, user]
    return user
  },

  async update(input: UpdateUserInput): Promise<User> {
    await delay(300)
    const existing = users.find((u) => u.id === input.id)
    if (!existing) {
      throw new AppError(`User ${input.id} was not found.`, 404, 'NOT_FOUND')
    }
    if (
      users.some(
        (u) => u.id !== input.id && u.userName.toLowerCase() === input.userName.toLowerCase(),
      )
    ) {
      throw new AppError('Username already exists.', 409, 'CONFLICT')
    }
    if (users.some((u) => u.id !== input.id && u.email.toLowerCase() === input.email.toLowerCase())) {
      throw new AppError('Email already exists.', 409, 'CONFLICT')
    }
    const updated: User = {
      ...existing,
      userName: input.userName,
      email: input.email,
      roleId: input.roleId ?? null,
      roleName: roleName(input.roleId),
      employeeId: input.employeeId ?? null,
      updatedAtUtc: new Date().toISOString(),
    }
    users = users.map((u) => (u.id === input.id ? updated : u))
    return updated
  },

  async updateStatus(input: UpdateUserStatusInput): Promise<User> {
    await delay(200)
    const existing = users.find((u) => u.id === input.id)
    if (!existing) {
      throw new AppError(`User ${input.id} was not found.`, 404, 'NOT_FOUND')
    }
    const updated: User = { ...existing, isActive: input.isActive, updatedAtUtc: new Date().toISOString() }
    users = users.map((u) => (u.id === input.id ? updated : u))
    return updated
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    users = users.map((u) => (u.id === id ? { ...u, isDeleted: true } : u))
  },

  async restore(id: string): Promise<void> {
    await delay(200)
    users = users.map((u) => (u.id === id ? { ...u, isDeleted: false } : u))
  },
}
