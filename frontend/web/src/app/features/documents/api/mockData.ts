import type { EmployeeDocument } from './documentRepository'

// Ids line up with frontend/web/src/app/features/employees/api/mockData.ts.
export const mockDocuments: EmployeeDocument[] = [
  {
    id: '30000000-0000-0000-0000-000000000001',
    employeeId: '10000000-0000-0000-0000-000000000001',
    documentType: 'OfferLetter',
    originalFileName: 'ava-patel-offer-letter.pdf',
    contentType: 'application/pdf',
    fileSizeBytes: 182_400,
    uploadedAtUtc: '2022-02-20T09:15:00Z',
    expiresAtUtc: null,
  },
  {
    id: '30000000-0000-0000-0000-000000000002',
    employeeId: '10000000-0000-0000-0000-000000000001',
    documentType: 'ID Proof',
    originalFileName: 'ava-patel-passport.jpg',
    contentType: 'image/jpeg',
    fileSizeBytes: 96_200,
    uploadedAtUtc: '2022-02-21T11:40:00Z',
    expiresAtUtc: '2031-05-01T00:00:00Z',
  },
  {
    id: '30000000-0000-0000-0000-000000000003',
    employeeId: '10000000-0000-0000-0000-000000000002',
    documentType: 'NDA',
    originalFileName: 'liam-chen-nda.docx',
    contentType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    fileSizeBytes: 45_800,
    uploadedAtUtc: '2021-07-15T14:05:00Z',
    expiresAtUtc: null,
  },
]
