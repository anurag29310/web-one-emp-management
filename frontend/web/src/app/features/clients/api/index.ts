import { selectRepository } from '@/app/core/config/selectRepository'
import { mockClientRepository } from './mockClientRepository'
import { apiClientRepository } from './apiClientRepository'
import type { ClientRepository } from './clientRepository'

export const clientRepository: ClientRepository = selectRepository({
  mock: mockClientRepository,
  api: apiClientRepository,
})

export type { Client, ClientInput, ClientListFilters, ClientRepository } from './clientRepository'
