import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type {
  CreateUserInput,
  UpdateUserInput,
  UpdateUserStatusInput,
  User,
  UserListFilters,
  UserRepository,
} from './userRepository'

export const apiUserRepository: UserRepository = {
  async list(filters?: UserListFilters): Promise<User[]> {
    const response = await httpClient.get<{ data: User[] }>('/users', { params: filters })
    return unwrap(response)
  },

  async getById(id: string): Promise<User> {
    const response = await httpClient.get<{ data: User }>(`/users/${id}`)
    return unwrap(response)
  },

  async create(input: CreateUserInput): Promise<User> {
    const response = await httpClient.post<{ data: User }>('/users', input)
    return unwrap(response)
  },

  async update(input: UpdateUserInput): Promise<User> {
    const response = await httpClient.put<{ data: User }>(`/users/${input.id}`, input)
    return unwrap(response)
  },

  async updateStatus(input: UpdateUserStatusInput): Promise<User> {
    const response = await httpClient.patch<{ data: User }>(`/users/${input.id}/status`, {
      isActive: input.isActive,
    })
    return unwrap(response)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/users/${id}`)
  },

  async restore(id: string): Promise<void> {
    await httpClient.post(`/users/${id}/restore`)
  },
}
