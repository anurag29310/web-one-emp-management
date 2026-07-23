import { useState } from 'react'
import { useAuth } from '@/app/core/auth/useAuth'
import { useAuditLogs } from '../hooks/useAuditLogs'
import type { AuditLog } from '../api'
import { Pagination } from '../components/Pagination'
import { Modal } from '@/app/shared/components/Modal'

const inputClass =
  'rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'

function shortId(id: string | null): string {
  if (!id) return '—'
  return `${id.slice(0, 8)}…`
}

function formatJson(value: string | null): string {
  if (!value) return '—'
  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

function AuditLogDetail({ log }: { log: AuditLog }) {
  return (
    <div className="space-y-4 text-sm">
      <dl className="grid grid-cols-2 gap-x-4 gap-y-2">
        <dt className="text-ink-subtle">Actor (user ID)</dt>
        <dd className="font-mono text-ink" title={log.userId ?? undefined}>
          {log.userId ?? 'System'}
        </dd>
        <dt className="text-ink-subtle">Action</dt>
        <dd className="text-ink">{log.action}</dd>
        <dt className="text-ink-subtle">Entity</dt>
        <dd className="text-ink">{log.entityName}</dd>
        <dt className="text-ink-subtle">Entity ID</dt>
        <dd className="font-mono text-ink" title={log.entityId ?? undefined}>
          {log.entityId ?? '—'}
        </dd>
        <dt className="text-ink-subtle">Timestamp</dt>
        <dd className="text-ink">{new Date(log.createdAtUtc).toLocaleString()}</dd>
        <dt className="text-ink-subtle">IP address</dt>
        <dd className="text-ink">{log.ipAddress ?? '—'}</dd>
        <dt className="text-ink-subtle">User agent</dt>
        <dd className="break-words text-ink">{log.userAgent ?? '—'}</dd>
      </dl>

      <div>
        <p className="mb-1 text-xs font-medium uppercase tracking-[0.4px] text-ink-subtle">Old values</p>
        <pre className="overflow-x-auto rounded-md bg-surface-2 p-3 text-xs text-ink-muted">
          {formatJson(log.oldValuesJson)}
        </pre>
      </div>
      <div>
        <p className="mb-1 text-xs font-medium uppercase tracking-[0.4px] text-ink-subtle">New values</p>
        <pre className="overflow-x-auto rounded-md bg-surface-2 p-3 text-xs text-ink-muted">
          {formatJson(log.newValuesJson)}
        </pre>
      </div>
    </div>
  )
}

export function AuditLogListPage() {
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'

  const [page, setPage] = useState(1)
  const [entityName, setEntityName] = useState('')
  const [action, setAction] = useState('')
  const [userId, setUserId] = useState('')
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const [selectedLog, setSelectedLog] = useState<AuditLog | null>(null)

  const { result, isLoading, error } = useAuditLogs({
    page,
    pageSize: 20,
    entityName: entityName || undefined,
    action: action || undefined,
    userId: userId || undefined,
    dateFrom: dateFrom ? `${dateFrom}T00:00:00.000Z` : undefined,
    dateTo: dateTo ? `${dateTo}T23:59:59.999Z` : undefined,
  })

  if (!isAdmin) {
    return (
      <div className="rounded-lg border border-hairline bg-surface-1 p-8 text-center">
        <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">Access restricted</h1>
        <p className="mt-2 text-sm text-ink-subtle">Audit logs are only available to Administrators.</p>
      </div>
    )
  }

  function resetPage<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value)
      setPage(1)
    }
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Audit Logs</h1>
        <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
      </div>

      <div className="flex flex-wrap gap-3 rounded-lg border border-hairline bg-surface-1 p-4">
        <input
          type="text"
          placeholder="Entity name (e.g. Employee)…"
          value={entityName}
          onChange={(e) => resetPage(setEntityName)(e.target.value)}
          className={inputClass}
        />
        <input
          type="text"
          placeholder="Action (e.g. Update)…"
          value={action}
          onChange={(e) => resetPage(setAction)(e.target.value)}
          className={inputClass}
        />
        <input
          type="text"
          placeholder="User ID…"
          value={userId}
          onChange={(e) => resetPage(setUserId)(e.target.value)}
          className={`${inputClass} font-mono`}
        />
        <input
          type="date"
          value={dateFrom}
          onChange={(e) => resetPage(setDateFrom)(e.target.value)}
          className={inputClass}
        />
        <input
          type="date"
          value={dateTo}
          onChange={(e) => resetPage(setDateTo)(e.target.value)}
          className={inputClass}
        />
      </div>

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}

      <Modal isOpen={selectedLog !== null} onClose={() => setSelectedLog(null)} title="Audit log entry">
        {selectedLog && <AuditLogDetail log={selectedLog} />}
      </Modal>

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Timestamp</th>
              <th className="px-4 py-3">Actor</th>
              <th className="px-4 py-3">Action</th>
              <th className="px-4 py-3">Entity</th>
              <th className="px-4 py-3">Entity ID</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 5 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={6}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && result?.data.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={6}>
                  No audit log entries match your filters.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((log) => (
                <tr key={log.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3 text-ink-muted">{new Date(log.createdAtUtc).toLocaleString()}</td>
                  <td className="px-4 py-3 font-mono text-ink-muted" title={log.userId ?? undefined}>
                    {log.userId ? shortId(log.userId) : 'System'}
                  </td>
                  <td className="px-4 py-3 font-medium text-ink">{log.action}</td>
                  <td className="px-4 py-3 text-ink-muted">{log.entityName}</td>
                  <td className="px-4 py-3 font-mono text-ink-muted" title={log.entityId ?? undefined}>
                    {shortId(log.entityId)}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      type="button"
                      onClick={() => setSelectedLog(log)}
                      className="text-xs font-medium text-ink-muted hover:text-ink"
                    >
                      View
                    </button>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      {result && (
        <Pagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} onPageChange={setPage} />
      )}
    </div>
  )
}
