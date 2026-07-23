import { z } from 'zod'

const timePattern = /^\d{2}:\d{2}$/

/** Matches CreateShiftCommand / UpdateShiftCommand. Times are HTML <input type="time"> values ("HH:mm"). */
export const shiftFormSchema = z.object({
  name: z.string().min(1, 'Name is required.').max(150, 'Name must be 150 characters or fewer.'),
  startTime: z.string().regex(timePattern, 'Enter a start time.'),
  endTime: z.string().regex(timePattern, 'Enter an end time.'),
  graceMinutes: z.number().int().min(0, 'Grace minutes cannot be negative.'),
  isNightShift: z.boolean(),
})

export type ShiftFormValues = z.infer<typeof shiftFormSchema>

/** Matches AssignEmployeeShiftCommand / UpdateEmployeeShiftCommand. */
export const shiftAssignmentFormSchema = z
  .object({
    shiftId: z.string().min(1, 'Shift is required.'),
    effectiveFrom: z.string().min(1, 'Effective-from date is required.'),
    effectiveTo: z.string().optional().or(z.literal('')),
  })
  .refine((data) => !data.effectiveTo || data.effectiveTo >= data.effectiveFrom, {
    message: 'Effective-to date must be on or after the effective-from date.',
    path: ['effectiveTo'],
  })

export type ShiftAssignmentFormValues = z.infer<typeof shiftAssignmentFormSchema>

/** "HH:mm:ss" (backend TimeSpan) -> "HH:mm" (<input type="time">). */
export function toTimeInputValue(timeSpan: string): string {
  return timeSpan.slice(0, 5)
}

/** "HH:mm" (<input type="time">) -> "HH:mm:ss" (backend TimeSpan). */
export function toTimeSpanValue(timeInput: string): string {
  return timeInput.length === 5 ? `${timeInput}:00` : timeInput
}
