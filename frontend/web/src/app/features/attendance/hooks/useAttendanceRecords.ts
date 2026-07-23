import { useCallback, useEffect, useState } from 'react'
import type { AttendanceRecord, AttendanceRecordListFilters } from '../api'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { attendanceRepository } from '../api'

interface UseAttendanceRecordsResult {
  result: PagedResult<AttendanceRecord> | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

/**
 * Backs both the privileged Attendance Records page (filters span employees/departments)
 * and the personal "My Attendance" history/calendar — for a non-privileged caller the
 * server always scopes results to self (and, for Managers, their team) regardless of the
 * employeeId filter supplied, per AttendanceController/GetAttendanceRecordsQueryHandler.
 */
export function useAttendanceRecords(filters: AttendanceRecordListFilters = {}): UseAttendanceRecordsResult {
  const [result, setResult] = useState<PagedResult<AttendanceRecord> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const {
    page,
    pageSize,
    employeeId,
    departmentId,
    managerId,
    dateFrom,
    dateTo,
    status,
    isLateArrival,
    isEarlyLeave,
  } = filters

  useEffect(() => {
    let cancelled = false
    attendanceRepository
      .list({ page, pageSize, employeeId, departmentId, managerId, dateFrom, dateTo, status, isLateArrival, isEarlyLeave })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load attendance records.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, employeeId, departmentId, managerId, dateFrom, dateTo, status, isLateArrival, isEarlyLeave, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { result, isLoading, error, refresh }
}
