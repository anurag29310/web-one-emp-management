import { useCallback, useRef } from 'react'
import { attendanceRepository } from '../api'
import { startOfDayIso, endOfDayIso } from '../utils/date'

/**
 * Best-effort resolution of the current user's own employeeId, for the two self-service
 * attendance actions (check-in/check-out) whose request bodies require it explicitly.
 *
 * There is currently no accessible way to look this up directly: `GET /auth/me`
 * (CurrentUserDto) does not expose an employeeId, and a plain Employee-role caller cannot
 * call `GET /employees` to find their own record (CanViewEmployees requires Admin/HR/Manager).
 * Both of those live in the auth/employees features and are out of scope to change here.
 *
 * As a workaround: `GET /attendance` always scopes a non-privileged caller's results to
 * themselves (a Manager's results also include their direct reports), so an unambiguous
 * single-record response reveals the caller's real employeeId. When the result is ambiguous
 * (e.g. a Manager whose team already has records for today) or empty (no attendance history
 * at all yet), this resolves to null and callers should fall back to asking the user directly.
 */
export function useOwnEmployeeId() {
  const cache = useRef<string | null>(null)

  const resolve = useCallback(async (): Promise<string | null> => {
    if (cache.current) return cache.current

    const now = new Date()
    const todayResult = await attendanceRepository.list({
      dateFrom: startOfDayIso(now),
      dateTo: endOfDayIso(now),
      pageSize: 5,
    })
    if (todayResult.data.length === 1) {
      cache.current = todayResult.data[0].employeeId
      return cache.current
    }

    const recent = await attendanceRepository.list({ pageSize: 5 })
    if (recent.data.length === 1) {
      cache.current = recent.data[0].employeeId
      return cache.current
    }

    return null
  }, [])

  return { resolve }
}
