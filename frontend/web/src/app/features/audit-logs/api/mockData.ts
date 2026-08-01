import type { AuditLog } from './auditLogRepository'

// User/employee IDs match the ones used in employees mock data, and companyId matches Acme
// Corporation in features/platform/api/mockData.ts, so mock mode stays cross-consistent across
// modules.
const MOCK_COMPANY_ID = '00000000-0000-0000-0000-00000000c001'

export const mockAuditLogs: AuditLog[] = [
  {
    id: '90000000-0000-0000-0000-000000000001',
    companyId: MOCK_COMPANY_ID,
    userId: '10000000-0000-0000-0000-000000000001',
    entityName: 'Employee',
    entityId: '10000000-0000-0000-0000-000000000002',
    action: 'Update',
    oldValuesJson: '{"designation":"Software Engineer"}',
    newValuesJson: '{"designation":"Senior Software Engineer"}',
    ipAddress: '10.0.0.15',
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
    createdAtUtc: '2026-07-20T09:15:00Z',
  },
  {
    id: '90000000-0000-0000-0000-000000000002',
    companyId: MOCK_COMPANY_ID,
    userId: '10000000-0000-0000-0000-000000000001',
    entityName: 'LeaveRequest',
    entityId: '20000000-0000-0000-0000-000000000001',
    action: 'Approve',
    oldValuesJson: '{"status":"Pending"}',
    newValuesJson: '{"status":"Approved"}',
    ipAddress: '10.0.0.15',
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
    createdAtUtc: '2026-07-21T13:42:00Z',
  },
  {
    id: '90000000-0000-0000-0000-000000000003',
    companyId: MOCK_COMPANY_ID,
    userId: '10000000-0000-0000-0000-000000000002',
    entityName: 'Department',
    entityId: '00000000-0000-0000-0000-000000000303',
    action: 'Create',
    oldValuesJson: null,
    newValuesJson: '{"name":"Sales","code":"SALES"}',
    ipAddress: '10.0.0.22',
    userAgent: 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)',
    createdAtUtc: '2026-07-22T08:05:00Z',
  },
  {
    id: '90000000-0000-0000-0000-000000000004',
    companyId: null,
    userId: null,
    entityName: 'User',
    entityId: null,
    action: 'LoginFailed',
    oldValuesJson: null,
    newValuesJson: '{"email":"unknown@ems.local"}',
    ipAddress: '203.0.113.42',
    userAgent: 'Mozilla/5.0 (X11; Linux x86_64)',
    createdAtUtc: '2026-07-23T02:11:00Z',
  },
  {
    id: '90000000-0000-0000-0000-000000000005',
    companyId: MOCK_COMPANY_ID,
    userId: '10000000-0000-0000-0000-000000000001',
    entityName: 'PayrollRun',
    entityId: '30000000-0000-0000-0000-000000000001',
    action: 'Approve',
    oldValuesJson: '{"status":"Completed"}',
    newValuesJson: '{"status":"Approved"}',
    ipAddress: '10.0.0.15',
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
    createdAtUtc: '2026-07-23T16:30:00Z',
  },
]
