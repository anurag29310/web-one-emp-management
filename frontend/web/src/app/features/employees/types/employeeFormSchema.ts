import { z } from 'zod'

const optionalString = (max: number, label: string) =>
  z
    .string()
    .max(max, `${label} must be ${max} characters or fewer.`)
    .optional()
    .or(z.literal(''))

/** Mirrors backend/EMS.Application/Features/Employees/Validators/EmployeeCommandValidator.cs. */
export const employeeFormSchema = z.object({
  employeeCode: z
    .string()
    .min(1, 'Employee code is required.')
    .max(50, 'Employee code must be 50 characters or fewer.'),
  firstName: z.string().min(1, 'First name is required.').max(100, 'First name must be 100 characters or fewer.'),
  middleName: optionalString(100, 'Middle name'),
  lastName: z.string().min(1, 'Last name is required.').max(100, 'Last name must be 100 characters or fewer.'),
  email: z
    .string()
    .max(256, 'Email must be 256 characters or fewer.')
    .email('Enter a valid email address.')
    .optional()
    .or(z.literal('')),
  phoneNumber: optionalString(30, 'Phone number'),
  dateOfBirth: z.string().optional().or(z.literal('')),
  gender: optionalString(20, 'Gender'),
  addressLine1: optionalString(200, 'Address line 1'),
  addressLine2: optionalString(200, 'Address line 2'),
  city: optionalString(100, 'City'),
  state: optionalString(100, 'State'),
  postalCode: optionalString(20, 'Postal code'),
  country: optionalString(100, 'Country'),
  emergencyContactName: optionalString(150, 'Emergency contact name'),
  emergencyContactPhone: optionalString(30, 'Emergency contact phone'),
  emergencyContactRelation: optionalString(50, 'Relation'),
  joinDate: z.string().min(1, 'Join date is required.'),
  departmentId: z.string().optional().or(z.literal('')),
  teamId: z.string().optional().or(z.literal('')),
  designationId: z.string().min(1, 'Designation is required.'),
  managerId: z.string().optional().or(z.literal('')),
  officeLocationId: z.string().min(1, 'Office location is required.'),
  employmentStatus: z.enum(['Active', 'Inactive', 'OnLeave', 'Terminated']),
})

export type EmployeeFormValues = z.infer<typeof employeeFormSchema>

export const employeeStatusFormSchema = z.object({
  status: z.enum(['Active', 'Inactive', 'OnLeave', 'Terminated']),
  exitDate: z.string().optional().or(z.literal('')),
  reason: optionalString(500, 'Reason'),
})

export type EmployeeStatusFormValues = z.infer<typeof employeeStatusFormSchema>
