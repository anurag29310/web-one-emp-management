import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '@/app/core/auth/useAuth'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { useDepartments } from '@/app/features/departments/hooks/useDepartments'
import { useLeaveTypes } from '@/app/features/leave-types/hooks/useLeaveTypes'
import { useFileDownload } from '../hooks/useFileDownload'
import { exportsRepository } from '../api'

function ExportCard({
  title,
  description,
  onExport,
  isDownloading,
  error,
  children,
}: {
  title: string
  description: string
  onExport: () => void
  isDownloading: boolean
  error: string | null
  children?: ReactNode
}) {
  return (
    <section className="rounded-lg border border-hairline bg-surface-1 p-5">
      <div className="mb-4">
        <h2 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">{title}</h2>
        <p className="text-sm text-ink-subtle">{description}</p>
      </div>

      {children && <div className="mb-4 flex flex-wrap gap-3">{children}</div>}

      {error && (
        <p role="alert" className="mb-3 text-sm text-danger">
          {error}
        </p>
      )}

      <button
        type="button"
        disabled={isDownloading}
        onClick={onExport}
        className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isDownloading ? 'Exporting…' : 'Export'}
      </button>
    </section>
  )
}

const inputClass =
  'rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'

export function ExportsPage() {
  const { user } = useAuth()
  const canExportEmployees = user?.role === 'Admin' || user?.role === 'HR'

  const { result: employeesResult } = useEmployees({ pageSize: 200 })
  const { departments } = useDepartments()
  const { leaveTypes } = useLeaveTypes()

  const employeesExport = useFileDownload()
  const attendanceExport = useFileDownload()
  const leaveExport = useFileDownload()
  const dashboardExport = useFileDownload()

  const [employeeFilters, setEmployeeFilters] = useState({ search: '', departmentId: '', status: '' })
  const [attendanceFilters, setAttendanceFilters] = useState({
    employeeId: '',
    departmentId: '',
    dateFrom: '',
    dateTo: '',
    status: '',
  })
  const [leaveFilters, setLeaveFilters] = useState({ employeeId: '', leaveTypeId: '', year: '', status: '' })
  const [dashboardFilters, setDashboardFilters] = useState({ departmentId: '', date: '' })

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Data Exports</h1>
          <p className="text-sm text-ink-subtle">Download Excel and PDF exports of live system data</p>
        </div>
        <Link
          to="/reports"
          className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
        >
          Back to reports
        </Link>
      </div>

      {canExportEmployees && (
        <ExportCard
          title="Employees"
          description="Export the employee directory to Excel, matching the current employees list filters."
          isDownloading={employeesExport.isDownloading}
          error={employeesExport.error}
          onExport={() =>
            void employeesExport.trigger(() =>
              exportsRepository.exportEmployees({
                search: employeeFilters.search || undefined,
                departmentId: employeeFilters.departmentId || undefined,
                status: employeeFilters.status || undefined,
              }),
            )
          }
        >
          <input
            type="search"
            placeholder="Search by name or code…"
            value={employeeFilters.search}
            onChange={(e) => setEmployeeFilters((f) => ({ ...f, search: e.target.value }))}
            className={inputClass}
          />
          <select
            value={employeeFilters.departmentId}
            onChange={(e) => setEmployeeFilters((f) => ({ ...f, departmentId: e.target.value }))}
            className={inputClass}
          >
            <option value="">All departments</option>
            {departments.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}
              </option>
            ))}
          </select>
          <select
            value={employeeFilters.status}
            onChange={(e) => setEmployeeFilters((f) => ({ ...f, status: e.target.value }))}
            className={inputClass}
          >
            <option value="">All statuses</option>
            <option value="Active">Active</option>
            <option value="Inactive">Inactive</option>
            <option value="OnLeave">On leave</option>
            <option value="Terminated">Terminated</option>
          </select>
        </ExportCard>
      )}

      <ExportCard
        title="Attendance"
        description="Export attendance records to Excel. Managers are automatically scoped to their own team."
        isDownloading={attendanceExport.isDownloading}
        error={attendanceExport.error}
        onExport={() =>
          void attendanceExport.trigger(() =>
            exportsRepository.exportAttendance({
              employeeId: attendanceFilters.employeeId || undefined,
              departmentId: attendanceFilters.departmentId || undefined,
              dateFrom: attendanceFilters.dateFrom ? `${attendanceFilters.dateFrom}T00:00:00.000Z` : undefined,
              dateTo: attendanceFilters.dateTo ? `${attendanceFilters.dateTo}T23:59:59.999Z` : undefined,
              status: attendanceFilters.status || undefined,
            }),
          )
        }
      >
        <select
          value={attendanceFilters.employeeId}
          onChange={(e) => setAttendanceFilters((f) => ({ ...f, employeeId: e.target.value }))}
          className={inputClass}
        >
          <option value="">All employees</option>
          {employeesResult?.data.map((e) => (
            <option key={e.id} value={e.id}>
              {e.fullName}
            </option>
          ))}
        </select>
        <select
          value={attendanceFilters.departmentId}
          onChange={(e) => setAttendanceFilters((f) => ({ ...f, departmentId: e.target.value }))}
          className={inputClass}
        >
          <option value="">All departments</option>
          {departments.map((d) => (
            <option key={d.id} value={d.id}>
              {d.name}
            </option>
          ))}
        </select>
        <input
          type="date"
          value={attendanceFilters.dateFrom}
          onChange={(e) => setAttendanceFilters((f) => ({ ...f, dateFrom: e.target.value }))}
          className={inputClass}
        />
        <input
          type="date"
          value={attendanceFilters.dateTo}
          onChange={(e) => setAttendanceFilters((f) => ({ ...f, dateTo: e.target.value }))}
          className={inputClass}
        />
        <select
          value={attendanceFilters.status}
          onChange={(e) => setAttendanceFilters((f) => ({ ...f, status: e.target.value }))}
          className={inputClass}
        >
          <option value="">All statuses</option>
          <option value="Present">Present</option>
          <option value="Absent">Absent</option>
          <option value="Late">Late</option>
          <option value="HalfDay">Half day</option>
          <option value="OnLeave">On leave</option>
          <option value="Holiday">Holiday</option>
        </select>
      </ExportCard>

      <ExportCard
        title="Leave requests"
        description="Export leave requests to Excel."
        isDownloading={leaveExport.isDownloading}
        error={leaveExport.error}
        onExport={() =>
          void leaveExport.trigger(() =>
            exportsRepository.exportLeaveRequests({
              employeeId: leaveFilters.employeeId || undefined,
              leaveTypeId: leaveFilters.leaveTypeId || undefined,
              year: leaveFilters.year ? Number(leaveFilters.year) : undefined,
              status: leaveFilters.status || undefined,
            }),
          )
        }
      >
        <select
          value={leaveFilters.employeeId}
          onChange={(e) => setLeaveFilters((f) => ({ ...f, employeeId: e.target.value }))}
          className={inputClass}
        >
          <option value="">All employees</option>
          {employeesResult?.data.map((e) => (
            <option key={e.id} value={e.id}>
              {e.fullName}
            </option>
          ))}
        </select>
        <select
          value={leaveFilters.leaveTypeId}
          onChange={(e) => setLeaveFilters((f) => ({ ...f, leaveTypeId: e.target.value }))}
          className={inputClass}
        >
          <option value="">All leave types</option>
          {leaveTypes.map((t) => (
            <option key={t.id} value={t.id}>
              {t.name}
            </option>
          ))}
        </select>
        <input
          type="number"
          placeholder="Year"
          value={leaveFilters.year}
          onChange={(e) => setLeaveFilters((f) => ({ ...f, year: e.target.value }))}
          className={`${inputClass} w-28`}
        />
        <select
          value={leaveFilters.status}
          onChange={(e) => setLeaveFilters((f) => ({ ...f, status: e.target.value }))}
          className={inputClass}
        >
          <option value="">All statuses</option>
          <option value="Pending">Pending</option>
          <option value="Approved">Approved</option>
          <option value="Rejected">Rejected</option>
          <option value="Cancelled">Cancelled</option>
        </select>
      </ExportCard>

      <ExportCard
        title="Dashboard summary"
        description="Export a point-in-time dashboard summary to PDF."
        isDownloading={dashboardExport.isDownloading}
        error={dashboardExport.error}
        onExport={() =>
          void dashboardExport.trigger(() =>
            exportsRepository.exportDashboardSummary({
              departmentId: dashboardFilters.departmentId || undefined,
              date: dashboardFilters.date || undefined,
            }),
          )
        }
      >
        <select
          value={dashboardFilters.departmentId}
          onChange={(e) => setDashboardFilters((f) => ({ ...f, departmentId: e.target.value }))}
          className={inputClass}
        >
          <option value="">All departments</option>
          {departments.map((d) => (
            <option key={d.id} value={d.id}>
              {d.name}
            </option>
          ))}
        </select>
        <input
          type="date"
          value={dashboardFilters.date}
          onChange={(e) => setDashboardFilters((f) => ({ ...f, date: e.target.value }))}
          className={inputClass}
        />
      </ExportCard>
    </div>
  )
}
