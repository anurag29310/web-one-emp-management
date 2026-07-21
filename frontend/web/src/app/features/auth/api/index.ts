import { selectRepository } from '@/app/core/config/selectRepository'
import { mockAuthRepository } from './mockAuthRepository'
import { apiAuthRepository } from './apiAuthRepository'
import type { AuthRepository } from './authRepository'

export const authRepository: AuthRepository = selectRepository({
  mock: mockAuthRepository,
  api: apiAuthRepository,
})

export type { AuthRepository, LoginCredentials } from './authRepository'
