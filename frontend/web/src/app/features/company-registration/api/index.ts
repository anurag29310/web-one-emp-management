import { selectRepository } from '@/app/core/config/selectRepository'
import { mockCompanyRegistrationRepository } from './mockCompanyRegistrationRepository'
import { apiCompanyRegistrationRepository } from './apiCompanyRegistrationRepository'
import type { CompanyRegistrationRepository } from './companyRegistrationRepository'

export const companyRegistrationRepository: CompanyRegistrationRepository = selectRepository({
  mock: mockCompanyRegistrationRepository,
  api: apiCompanyRegistrationRepository,
})

export type {
  CompanyRegistrationInput,
  CompanyRegistrationRepository,
  RegisterCompanyResult,
} from './companyRegistrationRepository'
