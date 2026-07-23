/** Start-of-day UTC ISO instant for a given calendar date (local Date object). */
export function startOfDayIso(date: Date): string {
  const d = new Date(date)
  d.setUTCHours(0, 0, 0, 0)
  return d.toISOString()
}

/** End-of-day UTC ISO instant for a given calendar date (local Date object). */
export function endOfDayIso(date: Date): string {
  const d = new Date(date)
  d.setUTCHours(23, 59, 59, 999)
  return d.toISOString()
}

/** YYYY-MM-DD for a Date, using UTC fields so it lines up with the *AtUtc timestamps we filter against. */
export function toDateOnly(date: Date): string {
  return date.toISOString().slice(0, 10)
}
