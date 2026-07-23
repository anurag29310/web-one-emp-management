import { z } from 'zod'

export const salaryComponentFormSchema = z.object({
  id: z.string().optional(),
  name: z.string().trim().min(1, 'Name is required.').max(100, 'Name must be 100 characters or fewer.'),
  amount: z.coerce.number().min(0, 'Amount must be zero or greater.'),
})

export const salaryStructureFormSchema = z
  .object({
    employeeId: z.string().min(1, 'Employee is required.'),
    basicSalary: z.coerce.number().min(0, 'Basic salary must be zero or greater.'),
    effectiveFrom: z.string().min(1, 'Effective from date is required.'),
    effectiveTo: z.string().optional().or(z.literal('')),
    allowances: z.array(salaryComponentFormSchema),
    deductions: z.array(salaryComponentFormSchema),
  })
  .refine((values) => !values.effectiveTo || values.effectiveTo >= values.effectiveFrom, {
    message: 'Effective to date must be on or after the effective from date.',
    path: ['effectiveTo'],
  })

export type SalaryComponentFormValues = z.infer<typeof salaryComponentFormSchema>

// `basicSalary`/`amount` use z.coerce.number(), so the raw form input type
// (what react-hook-form's `register` binds to before parsing) differs from
// the parsed output type (what the submit handler receives). useForm is
// given both generics below so field registration and submit values each
// get the right shape.
export type SalaryStructureFormInput = z.input<typeof salaryStructureFormSchema>
export type SalaryStructureFormValues = z.infer<typeof salaryStructureFormSchema>

const todayIso = () => new Date().toISOString().slice(0, 10)

export const processPayrollFormSchema = z
  .object({
    periodStart: z.string().min(1, 'Period start is required.'),
    periodEnd: z.string().min(1, 'Period end is required.'),
  })
  .refine((values) => values.periodStart <= values.periodEnd, {
    message: 'Period start must be before or equal to period end.',
    path: ['periodEnd'],
  })
  .refine((values) => values.periodEnd <= todayIso(), {
    message: 'Payroll cannot be processed for a period that has not yet ended.',
    path: ['periodEnd'],
  })

export type ProcessPayrollFormValues = z.infer<typeof processPayrollFormSchema>
