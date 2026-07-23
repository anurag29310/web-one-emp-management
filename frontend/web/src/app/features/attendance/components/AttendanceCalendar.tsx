import type { AttendanceRecord } from '../api'
import { toDateOnly } from '../utils/date'

const STATUS_DOT: Record<AttendanceRecord['status'], string> = {
  Present: 'bg-success',
  Late: 'bg-warning',
  Absent: 'bg-danger',
  HalfDay: 'bg-warning',
  OnLeave: 'bg-primary',
  Holiday: 'bg-ink-tertiary',
}

const WEEKDAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']

interface AttendanceCalendarProps {
  monthDate: Date
  records: AttendanceRecord[]
  selectedDate: string | null
  onSelectDate: (date: string) => void
  onMonthChange: (monthDate: Date) => void
}

export function AttendanceCalendar({
  monthDate,
  records,
  selectedDate,
  onSelectDate,
  onMonthChange,
}: AttendanceCalendarProps) {
  const recordsByDate = new Map(records.map((r) => [r.attendanceDate.slice(0, 10), r]))
  const year = monthDate.getUTCFullYear()
  const month = monthDate.getUTCMonth()
  const firstOfMonth = new Date(Date.UTC(year, month, 1))
  const daysInMonth = new Date(Date.UTC(year, month + 1, 0)).getUTCDate()
  const leadingBlanks = firstOfMonth.getUTCDay()
  const todayKey = toDateOnly(new Date())

  const cells: Array<{ day: number; dateKey: string } | null> = []
  for (let i = 0; i < leadingBlanks; i++) cells.push(null)
  for (let day = 1; day <= daysInMonth; day++) {
    const dateKey = toDateOnly(new Date(Date.UTC(year, month, day)))
    cells.push({ day, dateKey })
  }

  return (
    <div className="rounded-lg border border-hairline bg-surface-1 p-4">
      <div className="mb-3 flex items-center justify-between">
        <button
          type="button"
          onClick={() => onMonthChange(new Date(Date.UTC(year, month - 1, 1)))}
          aria-label="Previous month"
          className="rounded-md p-1.5 text-ink-subtle transition hover:bg-surface-2 hover:text-ink"
        >
          ←
        </button>
        <p className="text-sm font-medium text-ink">
          {monthDate.toLocaleDateString(undefined, { month: 'long', year: 'numeric', timeZone: 'UTC' })}
        </p>
        <button
          type="button"
          onClick={() => onMonthChange(new Date(Date.UTC(year, month + 1, 1)))}
          aria-label="Next month"
          className="rounded-md p-1.5 text-ink-subtle transition hover:bg-surface-2 hover:text-ink"
        >
          →
        </button>
      </div>

      <div className="grid grid-cols-7 gap-1 text-center text-[11px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
        {WEEKDAY_LABELS.map((label) => (
          <span key={label} className="py-1">
            {label}
          </span>
        ))}
      </div>

      <div className="grid grid-cols-7 gap-1">
        {cells.map((cell, i) => {
          if (!cell) return <div key={`blank-${i}`} />
          const record = recordsByDate.get(cell.dateKey)
          const isSelected = selectedDate === cell.dateKey
          const isToday = cell.dateKey === todayKey
          return (
            <button
              key={cell.dateKey}
              type="button"
              onClick={() => onSelectDate(cell.dateKey)}
              className={`flex aspect-square flex-col items-center justify-center gap-0.5 rounded-md text-xs transition ${
                isSelected ? 'bg-primary text-white' : isToday ? 'bg-surface-2 text-ink' : 'text-ink-muted hover:bg-surface-2'
              }`}
            >
              <span>{cell.day}</span>
              {record && (
                <span className={`h-1.5 w-1.5 rounded-full ${isSelected ? 'bg-white' : STATUS_DOT[record.status]}`} />
              )}
            </button>
          )
        })}
      </div>
    </div>
  )
}
