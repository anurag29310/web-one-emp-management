import type { PayrollRun, Payslip, SalaryStructure } from './payrollRepository'

/**
 * Mock accounts (see auth/api/mockData.ts) aren't linked to employee records
 * anywhere else in the mock layer. For payroll self-service to demo sensibly,
 * this maps each mock account 1:1 to the mock employee sharing its position —
 * e.g. the "employee" account (…0004) is treated as Noah Williams (…0004).
 */
export const MOCK_USER_EMPLOYEE_ID: Record<string, string> = {
  '00000000-0000-0000-0000-000000000001': '10000000-0000-0000-0000-000000000001', // admin -> Ava Patel
  '00000000-0000-0000-0000-000000000002': '10000000-0000-0000-0000-000000000002', // hr -> Liam Chen
  '00000000-0000-0000-0000-000000000003': '10000000-0000-0000-0000-000000000003', // manager -> Sofia Garcia
  '00000000-0000-0000-0000-000000000004': '10000000-0000-0000-0000-000000000004', // employee -> Noah Williams
}

export const mockSalaryStructures: SalaryStructure[] = [
  {
    id: '00000000-0000-0000-0000-000000000501',
    employeeId: '10000000-0000-0000-0000-000000000001',
    basicSalary: 6000,
    allowances: [
      { id: '00000000-0000-0000-0000-000000000511', name: 'House', amount: 800 },
      { id: '00000000-0000-0000-0000-000000000512', name: 'Transport', amount: 200 },
    ],
    deductions: [{ id: '00000000-0000-0000-0000-000000000521', name: 'Tax', amount: 900 }],
    effectiveFrom: '2022-03-01',
    effectiveTo: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000502',
    employeeId: '10000000-0000-0000-0000-000000000002',
    basicSalary: 7000,
    allowances: [{ id: '00000000-0000-0000-0000-000000000513', name: 'House', amount: 900 }],
    deductions: [
      { id: '00000000-0000-0000-0000-000000000522', name: 'Tax', amount: 1100 },
      { id: '00000000-0000-0000-0000-000000000523', name: 'Insurance', amount: 150 },
    ],
    effectiveFrom: '2021-07-12',
    effectiveTo: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000503',
    employeeId: '10000000-0000-0000-0000-000000000003',
    basicSalary: 4800,
    allowances: [{ id: '00000000-0000-0000-0000-000000000514', name: 'Commission', amount: 600 }],
    deductions: [{ id: '00000000-0000-0000-0000-000000000524', name: 'Tax', amount: 620 }],
    effectiveFrom: '2023-01-09',
    effectiveTo: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000504',
    employeeId: '10000000-0000-0000-0000-000000000004',
    basicSalary: 5200,
    allowances: [{ id: '00000000-0000-0000-0000-000000000515', name: 'House', amount: 400 }],
    deductions: [{ id: '00000000-0000-0000-0000-000000000525', name: 'Tax', amount: 780 }],
    effectiveFrom: '2019-05-20',
    effectiveTo: '2026-06-30',
  },
]

const runOnePayslips: Payslip[] = [
  {
    id: '00000000-0000-0000-0000-000000000701',
    payrollRunId: '00000000-0000-0000-0000-000000000901',
    employeeId: '10000000-0000-0000-0000-000000000001',
    basic: 6000,
    totalAllowances: 1000,
    totalDeductions: 900,
    grossPay: 7000,
    netPay: 6100,
    generatedAtUtc: '2026-06-01T02:00:01Z',
    hasDocument: true,
  },
  {
    id: '00000000-0000-0000-0000-000000000702',
    payrollRunId: '00000000-0000-0000-0000-000000000901',
    employeeId: '10000000-0000-0000-0000-000000000002',
    basic: 7000,
    totalAllowances: 900,
    totalDeductions: 1250,
    grossPay: 7900,
    netPay: 6650,
    generatedAtUtc: '2026-06-01T02:00:02Z',
    hasDocument: true,
  },
  {
    id: '00000000-0000-0000-0000-000000000703',
    payrollRunId: '00000000-0000-0000-0000-000000000901',
    employeeId: '10000000-0000-0000-0000-000000000003',
    basic: 4800,
    totalAllowances: 600,
    totalDeductions: 620,
    grossPay: 5400,
    netPay: 4780,
    generatedAtUtc: '2026-06-01T02:00:03Z',
    hasDocument: true,
  },
  {
    id: '00000000-0000-0000-0000-000000000704',
    payrollRunId: '00000000-0000-0000-0000-000000000901',
    employeeId: '10000000-0000-0000-0000-000000000004',
    basic: 5200,
    totalAllowances: 400,
    totalDeductions: 780,
    grossPay: 5600,
    netPay: 4820,
    generatedAtUtc: '2026-06-01T02:00:04Z',
    hasDocument: true,
  },
]

const runTwoPayslips: Payslip[] = [
  {
    id: '00000000-0000-0000-0000-000000000705',
    payrollRunId: '00000000-0000-0000-0000-000000000902',
    employeeId: '10000000-0000-0000-0000-000000000001',
    basic: 6000,
    totalAllowances: 1000,
    totalDeductions: 900,
    grossPay: 7000,
    netPay: 6100,
    generatedAtUtc: '2026-07-01T02:00:01Z',
    hasDocument: true,
  },
  {
    id: '00000000-0000-0000-0000-000000000706',
    payrollRunId: '00000000-0000-0000-0000-000000000902',
    employeeId: '10000000-0000-0000-0000-000000000002',
    basic: 7000,
    totalAllowances: 900,
    totalDeductions: 1250,
    grossPay: 7900,
    netPay: 6650,
    generatedAtUtc: '2026-07-01T02:00:02Z',
    hasDocument: true,
  },
  {
    id: '00000000-0000-0000-0000-000000000707',
    payrollRunId: '00000000-0000-0000-0000-000000000902',
    employeeId: '10000000-0000-0000-0000-000000000003',
    basic: 4800,
    totalAllowances: 600,
    totalDeductions: 620,
    grossPay: 5400,
    netPay: 4780,
    generatedAtUtc: '2026-07-01T02:00:03Z',
    hasDocument: true,
  },
]

export const mockPayrollRuns: PayrollRun[] = [
  {
    id: '00000000-0000-0000-0000-000000000901',
    periodStart: '2026-05-01',
    periodEnd: '2026-05-31',
    processedAtUtc: '2026-06-01T02:00:00Z',
    processedBy: '00000000-0000-0000-0000-000000000001',
    status: 'Approved',
    payslipCount: runOnePayslips.length,
    totalNetPay: runOnePayslips.reduce((sum, p) => sum + p.netPay, 0),
    payslips: runOnePayslips,
  },
  {
    id: '00000000-0000-0000-0000-000000000902',
    periodStart: '2026-06-01',
    periodEnd: '2026-06-30',
    processedAtUtc: '2026-07-01T02:00:00Z',
    processedBy: '00000000-0000-0000-0000-000000000002',
    status: 'Completed',
    payslipCount: runTwoPayslips.length,
    totalNetPay: runTwoPayslips.reduce((sum, p) => sum + p.netPay, 0),
    payslips: runTwoPayslips,
  },
]
