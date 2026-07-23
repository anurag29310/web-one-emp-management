export type PayrollRunStatus = 'Processing' | 'Completed' | 'Approved'

export interface SalaryComponent {
  id: string
  name: string
  amount: number
}

export interface SalaryStructure {
  id: string
  employeeId: string
  basicSalary: number
  allowances: SalaryComponent[]
  deductions: SalaryComponent[]
  effectiveFrom: string
  effectiveTo: string | null
}

export interface SalaryComponentInput {
  id?: string
  name: string
  amount: number
}

export interface SalaryStructureFormInput {
  basicSalary: number
  allowances: SalaryComponentInput[]
  deductions: SalaryComponentInput[]
  effectiveFrom: string
  effectiveTo?: string
}

export interface CreateSalaryStructureInput extends SalaryStructureFormInput {
  employeeId: string
}

export interface UpdateSalaryStructureInput extends SalaryStructureFormInput {
  id: string
}

export interface Payslip {
  id: string
  payrollRunId: string
  employeeId: string
  basic: number
  totalAllowances: number
  totalDeductions: number
  grossPay: number
  netPay: number
  generatedAtUtc: string
  hasDocument: boolean
}

export interface PayslipPreview {
  employeeId: string
  basic: number
  totalAllowances: number
  totalDeductions: number
  grossPay: number
  netPay: number
}

export interface PayrollRun {
  id: string
  periodStart: string
  periodEnd: string
  processedAtUtc: string
  processedBy: string
  status: PayrollRunStatus
  payslipCount: number
  totalNetPay: number
  payslips: Payslip[]
}

export interface ProcessPayrollInput {
  periodStart: string
  periodEnd: string
}

export interface PayslipDownload {
  blob: Blob
  fileName: string
}

export interface PayrollRepository {
  // Payroll runs
  listRuns(): Promise<PayrollRun[]>
  getRun(id: string): Promise<PayrollRun>
  processPayroll(input: ProcessPayrollInput): Promise<{ payrollRunId: string }>
  dryRunPayroll(input: ProcessPayrollInput): Promise<PayslipPreview[]>
  approveRun(id: string): Promise<void>

  // Salary structures
  listSalaryStructures(): Promise<SalaryStructure[]>
  getSalaryStructure(id: string): Promise<SalaryStructure>
  createSalaryStructure(input: CreateSalaryStructureInput): Promise<{ id: string }>
  updateSalaryStructure(input: UpdateSalaryStructureInput): Promise<void>
  deleteSalaryStructure(id: string): Promise<void>

  // Payslips
  /**
   * Lists payslips for an employee. `employeeId` is required for privileged
   * (Admin/HR) callers and ignored — forced to the caller's own record — for
   * everyone else, mirroring the backend's GetPayslipsForEmployeeQuery.
   */
  listPayslips(employeeId?: string): Promise<Payslip[]>
  downloadPayslip(payslipId: string): Promise<PayslipDownload>
}
