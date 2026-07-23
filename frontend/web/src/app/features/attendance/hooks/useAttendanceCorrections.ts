import { useCallback, useEffect, useState } from 'react'
import type { AttendanceCorrection, AttendanceCorrectionListFilters } from '../api'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { attendanceRepository } from '../api'

interface UseAttendanceCorrectionsResult {
  result: PagedResult<AttendanceCorrection> | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

/** GET /attendance/corrections — requires CanReviewAttendanceCorrections (Admin, HR, Manager). */
export function useAttendanceCorrections(filters: AttendanceCorrectionListFilters = {}): UseAttendanceCorrectionsResult {
  const [result, setResult] = useState<PagedResult<AttendanceCorrection> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { page, pageSize, employeeId, status } = filters

  useEffect(() => {
    let cancelled = false
    attendanceRepository
      .listCorrections({ page, pageSize, employeeId, status })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load correction requests.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, employeeId, status, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { result, isLoading, error, refresh }
}
