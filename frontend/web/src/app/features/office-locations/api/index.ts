import { selectRepository } from '@/app/core/config/selectRepository'
import { mockOfficeLocationRepository } from './mockOfficeLocationRepository'
import { apiOfficeLocationRepository } from './apiOfficeLocationRepository'
import type { OfficeLocationRepository } from './officeLocationRepository'

export const officeLocationRepository: OfficeLocationRepository = selectRepository({
  mock: mockOfficeLocationRepository,
  api: apiOfficeLocationRepository,
})

export type {
  OfficeLocation,
  OfficeLocationRepository,
  CreateOfficeLocationInput,
  UpdateOfficeLocationInput,
} from './officeLocationRepository'
