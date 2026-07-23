import { z } from 'zod'
import type { AttendanceStatus } from '../api'

/** Mirrors backend/EMS.Domain/Enums/AttendanceStatus.cs. */
export const attendanceStatusOptions: AttendanceStatus[] = [
  'Present',
  'Absent',
  'Late',
  'HalfDay',
  'OnLeave',
  'Holiday',
]

const attendanceStatusEnum = z.enum(['Present', 'Absent', 'Late', 'HalfDay', 'OnLeave', 'Holiday'])

/** Matches CreateAttendanceRecordCommand (Admin/HR only). */
export const createAttendanceRecordSchema = z
  .object({
    employeeId: z.string().min(1, 'Employee is required.'),
    shiftId: z.string().optional().or(z.literal('')),
    attendanceDate: z.string().min(1, 'Date is required.'),
    checkInAt: z.string().optional().or(z.literal('')),
    checkOutAt: z.string().optional().or(z.literal('')),
    status: attendanceStatusEnum,
    notes: z.string().max(500, 'Notes must be 500 characters or fewer.').optional().or(z.literal('')),
  })
  .refine((data) => !data.checkInAt || !data.checkOutAt || data.checkOutAt >= data.checkInAt, {
    message: 'Check-out time must be on or after check-in time.',
    path: ['checkOutAt'],
  })

export type CreateAttendanceRecordFormValues = z.infer<typeof createAttendanceRecordSchema>

/** Matches UpdateAttendanceRecordCommand — employeeId/attendanceDate are immutable after creation. */
export const updateAttendanceRecordSchema = z
  .object({
    shiftId: z.string().optional().or(z.literal('')),
    checkInAt: z.string().optional().or(z.literal('')),
    checkOutAt: z.string().optional().or(z.literal('')),
    status: attendanceStatusEnum,
    notes: z.string().max(500, 'Notes must be 500 characters or fewer.').optional().or(z.literal('')),
  })
  .refine((data) => !data.checkInAt || !data.checkOutAt || data.checkOutAt >= data.checkInAt, {
    message: 'Check-out time must be on or after check-in time.',
    path: ['checkOutAt'],
  })

export type UpdateAttendanceRecordFormValues = z.infer<typeof updateAttendanceRecordSchema>

/** Matches CreateAttendanceCorrectionCommand. */
export const correctionRequestSchema = z
  .object({
    requestedCheckInAt: z.string().optional().or(z.literal('')),
    requestedCheckOutAt: z.string().optional().or(z.literal('')),
    reason: z
      .string()
      .min(1, 'Reason is required.')
      .max(500, 'Reason must be 500 characters or fewer.'),
  })
  .refine((data) => Boolean(data.requestedCheckInAt) || Boolean(data.requestedCheckOutAt), {
    message: 'Provide a corrected check-in time, check-out time, or both.',
    path: ['requestedCheckInAt'],
  })

export type CorrectionRequestFormValues = z.infer<typeof correctionRequestSchema>

/** Matches DecisionRequest (approve/reject body). */
export const decisionSchema = z.object({
  comments: z.string().max(1000, 'Comments must be 1000 characters or fewer.').optional().or(z.literal('')),
})

export type DecisionFormValues = z.infer<typeof decisionSchema>
