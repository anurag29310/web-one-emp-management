import { z } from 'zod'

/**
 * Mirrors the allow-list and size cap enforced server-side in
 * backend/EMS.Application/Features/Documents/Handlers/UploadDocumentCommandHandler.cs.
 * Keep these in sync if the backend's limits ever change — the server remains the
 * source of truth and re-validates independently of this client-side check.
 */
export const ALLOWED_DOCUMENT_CONTENT_TYPES = [
  'application/pdf',
  'image/jpeg',
  'image/png',
  'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
] as const

export const ALLOWED_DOCUMENT_EXTENSIONS_LABEL = 'PDF, JPEG, PNG, DOC, or DOCX'

export const MAX_DOCUMENT_SIZE_BYTES = 10 * 1024 * 1024 // 10 MB
export const MAX_DOCUMENT_SIZE_LABEL = '10 MB'

// backend/EMS.Domain/Entities/EmployeeDocument.cs documents this as the conventional set for
// DocumentType — the column itself is a free-form string (no server-side enum/validator), so
// this list is a client-side convenience, not an enforced contract.
export const DOCUMENT_TYPES = ['ID Proof', 'OfferLetter', 'NDA', 'Appraisal', 'Payslip', 'Other'] as const

export const documentUploadSchema = z.object({
  documentType: z.string().min(1, 'Document type is required.'),
  expiresAtUtc: z.string().optional().or(z.literal('')),
  file: z
    .instanceof(FileList)
    .refine((files) => files.length === 1, 'Select a file to upload.')
    .refine((files) => files.length !== 1 || files[0].size > 0, 'File cannot be empty.')
    .refine(
      (files) => files.length !== 1 || files[0].size <= MAX_DOCUMENT_SIZE_BYTES,
      `File must be ${MAX_DOCUMENT_SIZE_LABEL} or smaller.`,
    )
    .refine(
      (files) =>
        files.length !== 1 ||
        (ALLOWED_DOCUMENT_CONTENT_TYPES as readonly string[]).includes(files[0].type),
      `Unsupported file type. Allowed: ${ALLOWED_DOCUMENT_EXTENSIONS_LABEL}.`,
    ),
})

export type DocumentUploadFormValues = z.infer<typeof documentUploadSchema>
