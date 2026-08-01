import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type {
  CompanyRegistrationInput,
  CompanyRegistrationRepository,
  RegisterCompanyResult,
} from './companyRegistrationRepository'

export const apiCompanyRegistrationRepository: CompanyRegistrationRepository = {
  async getStatus(): Promise<boolean> {
    const response = await httpClient.get<{ data: boolean }>('/company-registration/status')
    return unwrap(response)
  },

  async register(input: CompanyRegistrationInput): Promise<RegisterCompanyResult> {
    const response = await httpClient.post<{ data: RegisterCompanyResult }>('/company-registration', input)
    return unwrap(response)
  },
}
