import { selectRepository } from '@/app/core/config/selectRepository'
import { mockPayrollRepository } from './mockPayrollRepository'
import { apiPayrollRepository } from './apiPayrollRepository'
import type { PayrollRepository } from './payrollRepository'

export const payrollRepository: PayrollRepository = selectRepository({
  mock: mockPayrollRepository,
  api: apiPayrollRepository,
})

export type {
  SalaryComponent,
  SalaryStructure,
  SalaryComponentInput,
  SalaryStructureFormInput,
  CreateSalaryStructureInput,
  UpdateSalaryStructureInput,
  Payslip,
  PayslipPreview,
  PayrollRun,
  PayrollRunStatus,
  ProcessPayrollInput,
  PayslipDownload,
  PayrollRepository,
} from './payrollRepository'
