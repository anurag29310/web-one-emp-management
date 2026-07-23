import { useState } from 'react'
import { useMyPayslips } from '../hooks/useMyPayslips'
import { payrollRepository, type Payslip } from '../api'
import { AppError } from '@/app/shared/models/appError'

function formatCurrency(amount: number): string {
  return amount.toLocaleString(undefined, { style: 'currency', currency: 'USD' })
}

export function MyPayslipsPage() {
  const { payslips, isLoading, error, refresh } = useMyPayslips()
  const [downloadingId, setDownloadingId] = useState<string | null>(null)
  const [downloadError, setDownloadError] = useState<string | null>(null)

  async function handleDownload(payslip: Payslip) {
    setDownloadError(null)
    setDownloadingId(payslip.id)
    try {
      const { blob, fileName } = await payrollRepository.downloadPayslip(payslip.id)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (err) {
      setDownloadError(err instanceof AppError ? err.message : 'Failed to download payslip.')
    } finally {
      setDownloadingId(null)
    }
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">My Payslips</h1>
        <p className="text-sm text-ink-subtle">{payslips.length} payslips</p>
      </div>

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}
      {downloadError && (
        <p role="alert" className="text-sm text-danger">
          {downloadError}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Generated</th>
              <th className="px-4 py-3">Basic</th>
              <th className="px-4 py-3">Allowances</th>
              <th className="px-4 py-3">Deductions</th>
              <th className="px-4 py-3">Gross pay</th>
              <th className="px-4 py-3">Net pay</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={7}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && payslips.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={7}>
                  No payslips yet.
                </td>
              </tr>
            )}

            {!isLoading &&
              payslips.map((payslip) => (
                <tr key={payslip.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3 text-ink-muted">
                    {new Date(payslip.generatedAtUtc).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{formatCurrency(payslip.basic)}</td>
                  <td className="px-4 py-3 text-ink-muted">{formatCurrency(payslip.totalAllowances)}</td>
                  <td className="px-4 py-3 text-ink-muted">{formatCurrency(payslip.totalDeductions)}</td>
                  <td className="px-4 py-3 text-ink-muted">{formatCurrency(payslip.grossPay)}</td>
                  <td className="px-4 py-3 font-medium text-ink">{formatCurrency(payslip.netPay)}</td>
                  <td className="px-4 py-3 text-right">
                    {payslip.hasDocument ? (
                      <button
                        type="button"
                        disabled={downloadingId === payslip.id}
                        onClick={() => void handleDownload(payslip)}
                        className="text-xs font-medium text-primary-hover hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        {downloadingId === payslip.id ? 'Downloading…' : 'Download PDF'}
                      </button>
                    ) : (
                      <span className="text-xs text-ink-subtle">No document</span>
                    )}
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      {!isLoading && payslips.length === 0 && !error && (
        <button
          type="button"
          onClick={refresh}
          className="text-sm font-medium text-primary-hover hover:underline"
        >
          Refresh
        </button>
      )}
    </div>
  )
}
