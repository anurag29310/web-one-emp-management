import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type {
  CreateSalaryStructureInput,
  PayrollRepository,
  PayrollRun,
  Payslip,
  PayslipDownload,
  PayslipPreview,
  ProcessPayrollInput,
  SalaryStructure,
  UpdateSalaryStructureInput,
} from './payrollRepository'

// Backend uses ASP.NET's File() helper, which sets a standard
// `attachment; filename="name.pdf"` (or filename*=UTF-8''name.pdf) header.
function extractFileName(contentDisposition: string | undefined, fallback: string): string {
  if (!contentDisposition) return fallback
  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition)
  if (utf8Match) return decodeURIComponent(utf8Match[1])
  const quotedMatch = /filename="?([^";]+)"?/i.exec(contentDisposition)
  return quotedMatch ? quotedMatch[1] : fallback
}

export const apiPayrollRepository: PayrollRepository = {
  async listRuns(): Promise<PayrollRun[]> {
    const response = await httpClient.get<{ data: PayrollRun[] }>('/payroll/runs')
    return unwrap(response)
  },

  async getRun(id: string): Promise<PayrollRun> {
    const response = await httpClient.get<{ data: PayrollRun }>(`/payroll/runs/${id}`)
    return unwrap(response)
  },

  async processPayroll(input: ProcessPayrollInput): Promise<{ payrollRunId: string }> {
    const response = await httpClient.post<{ data: { payrollRunId: string } }>('/payroll/process', input)
    return unwrap(response)
  },

  async dryRunPayroll(input: ProcessPayrollInput): Promise<PayslipPreview[]> {
    const response = await httpClient.post<{ data: PayslipPreview[] }>('/payroll/dry-run', input)
    return unwrap(response)
  },

  async approveRun(id: string): Promise<void> {
    await httpClient.post(`/payroll/runs/${id}/approve`)
  },

  async listSalaryStructures(): Promise<SalaryStructure[]> {
    const response = await httpClient.get<{ data: SalaryStructure[] }>('/payroll/salary-structures')
    return unwrap(response)
  },

  async getSalaryStructure(id: string): Promise<SalaryStructure> {
    const response = await httpClient.get<{ data: SalaryStructure }>(`/payroll/salary-structures/${id}`)
    return unwrap(response)
  },

  async createSalaryStructure(input: CreateSalaryStructureInput): Promise<{ id: string }> {
    const response = await httpClient.post<{ data: { id: string } }>('/payroll/salary-structures', input)
    return unwrap(response)
  },

  async updateSalaryStructure(input: UpdateSalaryStructureInput): Promise<void> {
    await httpClient.put(`/payroll/salary-structures/${input.id}`, input)
  },

  async deleteSalaryStructure(id: string): Promise<void> {
    await httpClient.delete(`/payroll/salary-structures/${id}`)
  },

  async listPayslips(employeeId?: string): Promise<Payslip[]> {
    const response = await httpClient.get<{ data: Payslip[] }>('/payroll/payslips', {
      params: employeeId ? { employeeId } : undefined,
    })
    return unwrap(response)
  },

  async downloadPayslip(payslipId: string): Promise<PayslipDownload> {
    // responseType 'blob' is required here — the download endpoint returns a
    // raw application/pdf body, not the usual { data, message } JSON envelope.
    const response = await httpClient.get<Blob>(`/payroll/payslips/${payslipId}/download`, {
      responseType: 'blob',
    })
    return {
      blob: response.data,
      fileName: extractFileName(response.headers['content-disposition'], `payslip-${payslipId}.pdf`),
    }
  },
}
