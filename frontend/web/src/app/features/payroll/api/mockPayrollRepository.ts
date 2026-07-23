import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import { tokenStorage } from '@/app/core/auth/tokenStorage'
import { mockAccounts } from '@/app/features/auth/api/mockData'
import type {
  CreateSalaryStructureInput,
  PayrollRepository,
  PayrollRun,
  Payslip,
  PayslipDownload,
  PayslipPreview,
  ProcessPayrollInput,
  SalaryComponent,
  SalaryComponentInput,
  SalaryStructure,
  UpdateSalaryStructureInput,
} from './payrollRepository'
import { MOCK_USER_EMPLOYEE_ID, mockPayrollRuns, mockSalaryStructures } from './mockData'

let payrollRuns = mockPayrollRuns.map((run) => ({ ...run, payslips: [...run.payslips] }))
let salaryStructures = mockSalaryStructures.map((structure) => ({
  ...structure,
  allowances: [...structure.allowances],
  deductions: [...structure.deductions],
}))

let idSequence = 0
function nextId(prefix: string): string {
  idSequence += 1
  return `${prefix}-0000-0000-0000-${(Date.now() + idSequence).toString().padStart(12, '0')}`
}

// Mirrors resolving identity from a JWT server-side: mock access tokens are
// minted as `mock-access-<userId>-<timestamp>` by mockAuthRepository, so the
// user id can be recovered the same way the real API reads it off the token.
function getCurrentMockUserId(): string | null {
  const token = tokenStorage.getAccessToken()
  const match = token ? /^mock-access-([^-]+(?:-[^-]+){4})-\d+$/.exec(token) : null
  return match ? match[1] : null
}

function isCurrentMockUserPrivileged(): boolean {
  const account = mockAccounts.find((a) => a.user.id === getCurrentMockUserId())
  return account?.user.role === 'Admin' || account?.user.role === 'HR'
}

function getCurrentMockEmployeeId(): string | null {
  const userId = getCurrentMockUserId()
  return userId ? (MOCK_USER_EMPLOYEE_ID[userId] ?? null) : null
}

function allPayslips(): Payslip[] {
  return payrollRuns.flatMap((run) => run.payslips)
}

function calculateTotals(basicSalary: number, allowances: { amount: number }[], deductions: { amount: number }[]) {
  const totalAllowances = allowances.reduce((sum, a) => sum + a.amount, 0)
  const totalDeductions = deductions.reduce((sum, d) => sum + d.amount, 0)
  const grossPay = basicSalary + totalAllowances
  const netPay = grossPay - totalDeductions
  return { totalAllowances, totalDeductions, grossPay, netPay }
}

function toComponents(inputs: SalaryComponentInput[], prefix: string): SalaryComponent[] {
  return inputs.map((input) => ({ id: input.id ?? nextId(prefix), name: input.name, amount: input.amount }))
}

function structuresActiveDuring(periodStart: string, periodEnd: string): SalaryStructure[] {
  return salaryStructures.filter(
    (s) => s.effectiveFrom <= periodEnd && (!s.effectiveTo || s.effectiveTo >= periodStart),
  )
}

function validatePeriod(input: ProcessPayrollInput, requireEnded: boolean): void {
  if (input.periodStart > input.periodEnd) {
    throw new AppError('PeriodStart must be before or equal to PeriodEnd.', 400, 'VALIDATION_ERROR')
  }
  if (requireEnded) {
    const today = new Date().toISOString().slice(0, 10)
    if (input.periodEnd > today) {
      throw new AppError(
        'Payroll cannot be processed for a period that has not yet ended.',
        400,
        'VALIDATION_ERROR',
      )
    }
  }
}

export const mockPayrollRepository: PayrollRepository = {
  async listRuns(): Promise<PayrollRun[]> {
    await delay(300)
    return [...payrollRuns].sort((a, b) => b.periodStart.localeCompare(a.periodStart))
  },

  async getRun(id: string): Promise<PayrollRun> {
    await delay(200)
    const run = payrollRuns.find((r) => r.id === id)
    if (!run) {
      throw new AppError(`Payroll run ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return run
  },

  async processPayroll(input: ProcessPayrollInput): Promise<{ payrollRunId: string }> {
    await delay(600)
    validatePeriod(input, true)

    const applicable = structuresActiveDuring(input.periodStart, input.periodEnd)
    const runId = nextId('90000000')
    const generatedAt = new Date().toISOString()

    const payslips: Payslip[] = applicable.map((structure) => {
      const totals = calculateTotals(structure.basicSalary, structure.allowances, structure.deductions)
      return {
        id: nextId('70000000'),
        payrollRunId: runId,
        employeeId: structure.employeeId,
        basic: structure.basicSalary,
        totalAllowances: totals.totalAllowances,
        totalDeductions: totals.totalDeductions,
        grossPay: totals.grossPay,
        netPay: totals.netPay,
        generatedAtUtc: generatedAt,
        hasDocument: true,
      }
    })

    const run: PayrollRun = {
      id: runId,
      periodStart: input.periodStart,
      periodEnd: input.periodEnd,
      processedAtUtc: generatedAt,
      processedBy: getCurrentMockUserId() ?? '00000000-0000-0000-0000-000000000000',
      status: 'Completed',
      payslipCount: payslips.length,
      totalNetPay: payslips.reduce((sum, p) => sum + p.netPay, 0),
      payslips,
    }

    payrollRuns = [run, ...payrollRuns]
    return { payrollRunId: runId }
  },

  async dryRunPayroll(input: ProcessPayrollInput): Promise<PayslipPreview[]> {
    await delay(400)
    validatePeriod(input, false)

    return structuresActiveDuring(input.periodStart, input.periodEnd).map((structure) => {
      const totals = calculateTotals(structure.basicSalary, structure.allowances, structure.deductions)
      return { employeeId: structure.employeeId, basic: structure.basicSalary, ...totals }
    })
  },

  async approveRun(id: string): Promise<void> {
    await delay(300)
    const run = payrollRuns.find((r) => r.id === id)
    if (!run) {
      throw new AppError(`Payroll run ${id} was not found.`, 404, 'NOT_FOUND')
    }
    if (run.status !== 'Completed') {
      throw new AppError('Only completed payroll runs can be approved.', 409, 'CONFLICT')
    }
    payrollRuns = payrollRuns.map((r) => (r.id === id ? { ...r, status: 'Approved' } : r))
  },

  async listSalaryStructures(): Promise<SalaryStructure[]> {
    await delay(250)
    return [...salaryStructures]
  },

  async getSalaryStructure(id: string): Promise<SalaryStructure> {
    await delay(200)
    const structure = salaryStructures.find((s) => s.id === id)
    if (!structure) {
      throw new AppError(`Salary structure ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return structure
  },

  async createSalaryStructure(input: CreateSalaryStructureInput): Promise<{ id: string }> {
    await delay(300)
    const structure: SalaryStructure = {
      id: nextId('50000000'),
      employeeId: input.employeeId,
      basicSalary: input.basicSalary,
      allowances: toComponents(input.allowances, '51000000'),
      deductions: toComponents(input.deductions, '52000000'),
      effectiveFrom: input.effectiveFrom,
      effectiveTo: input.effectiveTo ?? null,
    }
    salaryStructures = [...salaryStructures, structure]
    return { id: structure.id }
  },

  async updateSalaryStructure(input: UpdateSalaryStructureInput): Promise<void> {
    await delay(300)
    const existing = salaryStructures.find((s) => s.id === input.id)
    if (!existing) {
      throw new AppError(`Salary structure ${input.id} was not found.`, 404, 'NOT_FOUND')
    }
    const updated: SalaryStructure = {
      ...existing,
      basicSalary: input.basicSalary,
      allowances: toComponents(input.allowances, '51000000'),
      deductions: toComponents(input.deductions, '52000000'),
      effectiveFrom: input.effectiveFrom,
      effectiveTo: input.effectiveTo ?? null,
    }
    salaryStructures = salaryStructures.map((s) => (s.id === input.id ? updated : s))
  },

  async deleteSalaryStructure(id: string): Promise<void> {
    await delay(200)
    if (!salaryStructures.some((s) => s.id === id)) {
      throw new AppError(`Salary structure ${id} was not found.`, 404, 'NOT_FOUND')
    }
    salaryStructures = salaryStructures.filter((s) => s.id !== id)
  },

  async listPayslips(employeeId?: string): Promise<Payslip[]> {
    await delay(250)
    const privileged = isCurrentMockUserPrivileged()

    let scopedEmployeeId = employeeId
    if (!privileged) {
      // Non-privileged callers are always scoped to their own employee
      // record, regardless of any employeeId filter supplied.
      scopedEmployeeId = getCurrentMockEmployeeId() ?? undefined
    } else if (!employeeId) {
      throw new AppError('The employeeId query parameter is required.', 400, 'VALIDATION_ERROR')
    }

    if (!scopedEmployeeId) return []
    return allPayslips()
      .filter((p) => p.employeeId === scopedEmployeeId)
      .sort((a, b) => b.generatedAtUtc.localeCompare(a.generatedAtUtc))
  },

  async downloadPayslip(payslipId: string): Promise<PayslipDownload> {
    await delay(300)
    const payslip = allPayslips().find((p) => p.id === payslipId)
    if (!payslip) {
      throw new AppError(`Payslip ${payslipId} was not found.`, 404, 'NOT_FOUND')
    }

    if (!isCurrentMockUserPrivileged() && payslip.employeeId !== getCurrentMockEmployeeId()) {
      throw new AppError('You may only download your own payslip.', 403, 'FORBIDDEN')
    }

    const content = [
      'Mock payslip PDF (simulated binary content — mock data source).',
      `Payslip ID: ${payslip.id}`,
      `Payroll run: ${payslip.payrollRunId}`,
      `Employee: ${payslip.employeeId}`,
      `Basic: ${payslip.basic}`,
      `Total allowances: ${payslip.totalAllowances}`,
      `Total deductions: ${payslip.totalDeductions}`,
      `Gross pay: ${payslip.grossPay}`,
      `Net pay: ${payslip.netPay}`,
    ].join('\n')

    return {
      blob: new Blob([content], { type: 'application/pdf' }),
      fileName: `payslip-${payslip.id}.pdf`,
    }
  },
}
