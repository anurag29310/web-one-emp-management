import { z } from 'zod'

export const assetFormSchema = z.object({
  category: z.string().min(1, 'Category is required.').max(100, 'Category must be 100 characters or fewer.'),
  brand: z.string().min(1, 'Brand is required.').max(100, 'Brand must be 100 characters or fewer.'),
  model: z.string().min(1, 'Model is required.').max(100, 'Model must be 100 characters or fewer.'),
  serialNumber: z
    .string()
    .min(1, 'Serial number is required.')
    .max(100, 'Serial number must be 100 characters or fewer.'),
  purchaseDate: z.string().min(1, 'Purchase date is required.'),
  purchaseCost: z.coerce.number().min(0, 'Purchase cost cannot be negative.'),
  notes: z.string().max(1000, 'Notes must be 1000 characters or fewer.').optional().or(z.literal('')),
})

// purchaseCost uses z.coerce.number(), so the raw form input type (what
// register binds to) differs from the parsed output type (what onSubmit
// receives) — useForm needs both generics, see SalaryStructuresPage for the
// established pattern.
export type AssetFormInput = z.input<typeof assetFormSchema>
export type AssetFormValues = z.infer<typeof assetFormSchema>

export const assignAssetFormSchema = z.object({
  employeeId: z.string().min(1, 'Employee is required.'),
  expectedReturnDate: z.string().optional().or(z.literal('')),
  conditionAtAssignment: z
    .string()
    .max(500, 'Condition notes must be 500 characters or fewer.')
    .optional()
    .or(z.literal('')),
  notes: z.string().max(500, 'Notes must be 500 characters or fewer.').optional().or(z.literal('')),
})

export type AssignAssetFormValues = z.infer<typeof assignAssetFormSchema>

const RETURN_STATUSES = ['Available', 'UnderRepair', 'Retired', 'Lost'] as const

export const returnAssetFormSchema = z.object({
  conditionAtReturn: z
    .string()
    .max(500, 'Condition notes must be 500 characters or fewer.')
    .optional()
    .or(z.literal('')),
  resultingAssetStatus: z.enum(RETURN_STATUSES),
  notes: z.string().max(500, 'Notes must be 500 characters or fewer.').optional().or(z.literal('')),
})

export type ReturnAssetFormValues = z.infer<typeof returnAssetFormSchema>

export const assetStatusChangeFormSchema = z.object({
  status: z.enum(RETURN_STATUSES),
  notes: z.string().max(500, 'Notes must be 500 characters or fewer.').optional().or(z.literal('')),
})

export type AssetStatusChangeFormValues = z.infer<typeof assetStatusChangeFormSchema>
