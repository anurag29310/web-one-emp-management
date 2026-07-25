import { selectRepository } from '@/app/core/config/selectRepository'
import { mockDesignationRepository } from './mockDesignationRepository'
import { apiDesignationRepository } from './apiDesignationRepository'
import type { DesignationRepository } from './designationRepository'

export const designationRepository: DesignationRepository = selectRepository({
  mock: mockDesignationRepository,
  api: apiDesignationRepository,
})

export type {
  Designation,
  DesignationRepository,
  CreateDesignationInput,
  UpdateDesignationInput,
} from './designationRepository'
