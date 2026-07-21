/**
 * Format date to ISO string
 */
export function formatDateISO(date: Date): string {
  return date.toISOString().split('T')[0]
}

/**
 * Format date for display
 */
export function formatDateDisplay(dateString: string, locale: string = 'en-US'): string {
  return new Date(dateString).toLocaleDateString(locale, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

/**
 * Format time for display
 */
export function formatTime(timeString: string, locale: string = 'en-US'): string {
  return new Date(`1970-01-01T${timeString}`).toLocaleTimeString(locale, {
    hour: '2-digit',
    minute: '2-digit',
  })
}

/**
 * Get initials from a name
 */
export function getInitials(name: string): string {
  return name
    .split(' ')
    .map((word) => word[0])
    .join('')
    .toUpperCase()
    .substring(0, 2)
}

/**
 * Delay execution (for testing)
 */
export function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms))
}

/**
 * Check if a string is a valid email
 */
export function isValidEmail(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)
}

/**
 * Check if a string is a valid phone number
 */
export function isValidPhoneNumber(phone: string): boolean {
  return /^\+?[\d\s\-\(\)]{10,}$/.test(phone)
}

/**
 * Group array items by key
 */
export function groupBy<T>(
  items: T[],
  keyFn: (item: T) => string,
): Record<string, T[]> {
  return items.reduce(
    (acc, item) => {
      const key = keyFn(item)
      if (!acc[key]) {
        acc[key] = []
      }
      acc[key].push(item)
      return acc
    },
    {} as Record<string, T[]>,
  )
}

/**
 * Sort array by multiple keys
 */
export function sortBy<T>(
  items: T[],
  ...keys: Array<{
    key: keyof T
    direction?: 'asc' | 'desc'
  }>
): T[] {
  return [...items].sort((a, b) => {
    for (const { key, direction = 'asc' } of keys) {
      const aVal = a[key]
      const bVal = b[key]

      if (aVal < bVal) {
        return direction === 'asc' ? -1 : 1
      }
      if (aVal > bVal) {
        return direction === 'asc' ? 1 : -1
      }
    }
    return 0
  })
}
