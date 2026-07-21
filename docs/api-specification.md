# Employee Management System API Specification

## 1. Overview

The Employee Management System API will be built with ASP.NET Core 9 Web API and exposed to the React frontend. The API follows REST principles, Clean Architecture boundaries, JWT authentication, refresh token rotation, role-based authorization, FluentValidation, Serilog logging, and centralized exception handling.

Base URL:

```text
/api/v1
```

Primary modules:

- Authentication and authorization
- Employees and employee documents
- Departments, teams, designations, office locations, and reporting hierarchy
- Attendance, shifts, and attendance corrections
- Leave requests, leave balances, leave types, and holidays
- Dashboard metrics
- Supporting admin, lookup, audit, and export APIs

## 2. Common API Standards

### 2.1 Authentication Header

All protected endpoints require:

```text
Authorization: Bearer {accessToken}
```

Public endpoints:

- `POST /auth/login`
- `POST /auth/refresh`
- `POST /auth/forgot-password`
- `POST /auth/reset-password`
- `POST /auth/mfa/verify`

### 2.2 Common Headers

Recommended request headers:

```text
Content-Type: application/json
Accept: application/json
X-Correlation-Id: optional-client-generated-id
```

Recommended response headers:

```text
X-Correlation-Id: server-or-client-correlation-id
```

### 2.3 Standard Success Response

Single resource:

```json
{
  "data": {},
  "message": "Request completed successfully",
  "correlationId": "f9b2f37a1e7b4f66"
}
```

List resource:

```json
{
  "data": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 125,
  "totalPages": 7,
  "correlationId": "f9b2f37a1e7b4f66"
}
```

### 2.4 Standard Error Response

```json
{
  "status": 400,
  "code": "VALIDATION_ERROR",
  "message": "One or more validation errors occurred.",
  "errors": [
    {
      "field": "email",
      "message": "Email is required."
    }
  ],
  "correlationId": "f9b2f37a1e7b4f66"
}
```

### 2.5 HTTP Status Codes

| Status | Usage |
| --- | --- |
| `200 OK` | Successful read or update |
| `201 Created` | Resource created |
| `204 No Content` | Successful delete or action without response body |
| `400 Bad Request` | Validation or malformed request |
| `401 Unauthorized` | Missing or invalid authentication |
| `403 Forbidden` | Authenticated but not allowed |
| `404 Not Found` | Resource not found |
| `409 Conflict` | Duplicate or conflicting state |
| `422 Unprocessable Entity` | Business rule violation |
| `500 Internal Server Error` | Unexpected server error |

### 2.6 Pagination, Sorting, And Filtering

Common query parameters for list endpoints:

| Parameter | Type | Notes |
| --- | --- | --- |
| `page` | integer | Default `1` |
| `pageSize` | integer | Default `20`, maximum `100` |
| `search` | string | Optional keyword search |
| `sortBy` | string | Field name |
| `sortDirection` | string | `asc` or `desc` |
| `includeDeleted` | boolean | Admin-only, default `false` |

### 2.7 Role Names

Supported roles:

- `Admin`
- `HR`
- `Manager`
- `Employee`

## 3. Authentication And Authorization APIs

### 3.1 Login

```text
POST /auth/login
```

Access: Public

Request:

```json
{
  "userNameOrEmail": "hr@example.com",
  "password": "Password@123"
}
```

Response `200 OK`:

```json
{
  "data": {
    "accessToken": "jwt",
    "refreshToken": "refresh-token",
    "expiresAtUtc": "2026-06-12T10:30:00Z",
    "requiresMfa": false,
    "user": {
      "id": "00000000-0000-0000-0000-000000000001",
      "employeeId": "00000000-0000-0000-0000-000000000101",
      "displayName": "HR User",
      "email": "hr@example.com",
      "roles": ["HR"]
    }
  }
}
```

If MFA is required, return `200 OK` with `requiresMfa: true` and an `mfaChallengeId` instead of tokens.

### 3.2 Verify MFA

```text
POST /auth/mfa/verify
```

Access: Public

Request:

```json
{
  "mfaChallengeId": "challenge-id",
  "code": "123456"
}
```

Response: same token response as login.

### 3.3 Refresh Token

```text
POST /auth/refresh
```

Access: Public

Request:

```json
{
  "refreshToken": "refresh-token"
}
```

Response `200 OK`: new access token and rotated refresh token.

### 3.4 Logout

```text
POST /auth/logout
```

Access: Authenticated

Request:

```json
{
  "refreshToken": "refresh-token"
}
```

Response: `204 No Content`

### 3.5 Logout All Sessions

```text
POST /auth/logout-all
```

Access: Authenticated

Revokes all active refresh tokens for the current user.

Response: `204 No Content`

### 3.6 Forgot Password

```text
POST /auth/forgot-password
```

Access: Public

Request:

```json
{
  "email": "employee@example.com"
}
```

Response: `204 No Content`

### 3.7 Reset Password

```text
POST /auth/reset-password
```

Access: Public

Request:

```json
{
  "email": "employee@example.com",
  "resetToken": "reset-token",
  "newPassword": "NewPassword@123"
}
```

Response: `204 No Content`

### 3.8 Change Password

```text
POST /auth/change-password
```

Access: Authenticated

Request:

```json
{
  "currentPassword": "Password@123",
  "newPassword": "NewPassword@123"
}
```

Response: `204 No Content`

### 3.9 Current User Profile

```text
GET /auth/me
```

Access: Authenticated

Returns the current authenticated user's account, employee link, roles, and permissions.

## 4. User And Role Administration APIs

These APIs support RBAC management and are required for a complete administration experience.

### 4.1 Users

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/users` | Admin | List users |
| `GET` | `/users/{id}` | Admin | Get user details |
| `POST` | `/users` | Admin | Create user account |
| `PUT` | `/users/{id}` | Admin | Update user account |
| `PATCH` | `/users/{id}/status` | Admin | Activate or deactivate user |
| `DELETE` | `/users/{id}` | Admin | Soft delete user |

Create user request:

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000101",
  "userName": "jsmith",
  "email": "jsmith@example.com",
  "temporaryPassword": "Password@123",
  "roleIds": ["00000000-0000-0000-0000-000000000201"],
  "isActive": true,
  "isMfaEnabled": false
}
```

### 4.2 Roles

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/roles` | Admin, HR | List roles |
| `GET` | `/roles/{id}` | Admin | Get role details |
| `POST` | `/roles` | Admin | Create role |
| `PUT` | `/roles/{id}` | Admin | Update role |
| `DELETE` | `/roles/{id}` | Admin | Soft delete role |
| `PUT` | `/users/{id}/roles` | Admin | Replace roles for a user |

## 5. Employee APIs

### 5.1 List Employees

```text
GET /employees
```

Access: Admin, HR, Manager

Query parameters:

| Parameter | Type | Notes |
| --- | --- | --- |
| `departmentId` | guid | Optional |
| `teamId` | guid | Optional |
| `designationId` | guid | Optional |
| `managerId` | guid | Optional |
| `officeLocationId` | guid | Optional |
| `status` | string | Active, Inactive, OnLeave, Terminated |
| `joinDateFrom` | date | Optional |
| `joinDateTo` | date | Optional |

Response `200 OK`: paginated employee summaries.

### 5.2 Get Employee

```text
GET /employees/{id}
```

Access: Admin, HR, Manager, Employee self

Returns full employee details, including department, team, designation, manager, location, and document summary.

### 5.3 Create Employee

```text
POST /employees
```

Access: Admin, HR

Request:

```json
{
  "employeeCode": "EMP-1001",
  "firstName": "John",
  "middleName": null,
  "lastName": "Smith",
  "email": "john.smith@example.com",
  "phoneNumber": "+1-555-0100",
  "dateOfBirth": "1992-04-15",
  "gender": "Male",
  "address": {
    "addressLine1": "100 Main Street",
    "addressLine2": null,
    "city": "Seattle",
    "state": "WA",
    "postalCode": "98101",
    "country": "USA"
  },
  "emergencyContact": {
    "name": "Jane Smith",
    "phone": "+1-555-0101",
    "relation": "Spouse"
  },
  "departmentId": "00000000-0000-0000-0000-000000000301",
  "teamId": null,
  "designationId": "00000000-0000-0000-0000-000000000401",
  "managerId": null,
  "officeLocationId": "00000000-0000-0000-0000-000000000501",
  "joinDate": "2026-07-01",
  "status": "Active",
  "createUserAccount": true,
  "roleIds": ["00000000-0000-0000-0000-000000000204"]
}
```

Response: `201 Created`

### 5.4 Update Employee

```text
PUT /employees/{id}
```

Access: Admin, HR

Updates full employee profile and employment data.

Response: `200 OK`

### 5.5 Update Own Employee Profile

```text
PATCH /employees/{id}/profile
```

Access: Employee self, Admin, HR

Allows limited self-service updates such as phone number, address, and emergency contact.

### 5.6 Update Employee Status

```text
PATCH /employees/{id}/status
```

Access: Admin, HR

Request:

```json
{
  "status": "Inactive",
  "exitDate": "2026-12-31",
  "reason": "Resigned"
}
```

### 5.7 Delete Employee

```text
DELETE /employees/{id}
```

Access: Admin, HR

Soft deletes the employee. Prefer status changes for normal exits.

Response: `204 No Content`

### 5.8 Restore Employee

```text
POST /employees/{id}/restore
```

Access: Admin

Restores a soft-deleted employee.

### 5.9 Employee Reporting Hierarchy

```text
GET /employees/{id}/reporting-hierarchy
GET /employees/{id}/direct-reports
```

Access: Admin, HR, Manager, Employee self

Returns manager chain and direct report summaries.

## 6. Employee Document APIs

### 6.1 List Employee Documents

```text
GET /employees/{employeeId}/documents
```

Access: Admin, HR, Manager for team, Employee self

Query parameters: `documentType`, `page`, `pageSize`

### 6.2 Upload Employee Document

```text
POST /employees/{employeeId}/documents
```

Access: Admin, HR, Employee self for allowed document types

Content type: `multipart/form-data`

Form fields:

| Field | Type | Notes |
| --- | --- | --- |
| `file` | file | Required |
| `documentType` | string | Required |
| `description` | string | Optional |

Response: `201 Created`

### 6.3 Download Employee Document

```text
GET /employees/{employeeId}/documents/{documentId}/download
```

Access: Admin, HR, Manager for team, Employee self

Returns file stream or short-lived signed Blob URL.

### 6.4 Delete Employee Document

```text
DELETE /employees/{employeeId}/documents/{documentId}
```

Access: Admin, HR

Soft deletes document metadata and removes or archives the Blob according to retention policy.

### 6.5 Set Profile Photo

```text
POST /employees/{employeeId}/profile-photo
```

Access: Admin, HR, Employee self

Content type: `multipart/form-data`

## 7. Department And Organization APIs

### 7.1 Departments

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/departments` | Authenticated | List departments |
| `GET` | `/departments/{id}` | Authenticated | Get department |
| `POST` | `/departments` | Admin, HR | Create department |
| `PUT` | `/departments/{id}` | Admin, HR | Update department |
| `DELETE` | `/departments/{id}` | Admin, HR | Soft delete department |
| `POST` | `/departments/{id}/restore` | Admin | Restore department |
| `GET` | `/departments/{id}/employees` | Admin, HR, Manager | List employees in department |
| `GET` | `/departments/{id}/teams` | Authenticated | List teams in department |

Department request:

```json
{
  "name": "Human Resources",
  "code": "HR",
  "description": "People operations",
  "headEmployeeId": "00000000-0000-0000-0000-000000000101"
}
```

### 7.2 Teams

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/teams` | Authenticated | List teams |
| `GET` | `/teams/{id}` | Authenticated | Get team |
| `POST` | `/teams` | Admin, HR | Create team |
| `PUT` | `/teams/{id}` | Admin, HR | Update team |
| `DELETE` | `/teams/{id}` | Admin, HR | Soft delete team |
| `GET` | `/teams/{id}/employees` | Admin, HR, Manager | List employees in team |

### 7.3 Designations

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/designations` | Authenticated | List designations |
| `GET` | `/designations/{id}` | Authenticated | Get designation |
| `POST` | `/designations` | Admin, HR | Create designation |
| `PUT` | `/designations/{id}` | Admin, HR | Update designation |
| `DELETE` | `/designations/{id}` | Admin, HR | Soft delete designation |

### 7.4 Office Locations

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/office-locations` | Authenticated | List office locations |
| `GET` | `/office-locations/{id}` | Authenticated | Get office location |
| `POST` | `/office-locations` | Admin, HR | Create office location |
| `PUT` | `/office-locations/{id}` | Admin, HR | Update office location |
| `DELETE` | `/office-locations/{id}` | Admin, HR | Soft delete office location |

## 8. Attendance APIs

### 8.1 Check In

```text
POST /attendance/check-in
```

Access: Employee, Admin, HR

Request:

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000101",
  "checkInAtUtc": "2026-06-12T03:45:00Z",
  "notes": "Office check-in"
}
```

Employees can check in only for themselves. Admin and HR can record on behalf of an employee.

### 8.2 Check Out

```text
POST /attendance/check-out
```

Access: Employee, Admin, HR

Request:

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000101",
  "checkOutAtUtc": "2026-06-12T12:45:00Z",
  "notes": "Office check-out"
}
```

### 8.3 Get Attendance Records

```text
GET /attendance
```

Access: Admin, HR, Manager, Employee self

Query parameters:

| Parameter | Type | Notes |
| --- | --- | --- |
| `employeeId` | guid | Optional; employees can use only self |
| `departmentId` | guid | Optional |
| `managerId` | guid | Optional |
| `dateFrom` | date | Required for large exports |
| `dateTo` | date | Required for large exports |
| `status` | string | Optional |
| `isLateArrival` | boolean | Optional |
| `isEarlyLeave` | boolean | Optional |

### 8.4 Get Attendance Record

```text
GET /attendance/{id}
```

Access: Admin, HR, Manager for team, Employee self

### 8.5 Create Manual Attendance Record

```text
POST /attendance
```

Access: Admin, HR

Creates a manual attendance record.

### 8.6 Update Attendance Record

```text
PUT /attendance/{id}
```

Access: Admin, HR

Manual correction by HR or admin.

### 8.7 Delete Attendance Record

```text
DELETE /attendance/{id}
```

Access: Admin, HR

Soft deletes attendance record.

### 8.8 Attendance Corrections

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/attendance/corrections` | Admin, HR, Manager | List correction requests |
| `GET` | `/attendance/corrections/{id}` | Admin, HR, Manager, Employee self | Get correction request |
| `POST` | `/attendance/corrections` | Employee | Request correction |
| `POST` | `/attendance/corrections/{id}/approve` | Admin, HR, Manager | Approve correction |
| `POST` | `/attendance/corrections/{id}/reject` | Admin, HR, Manager | Reject correction |

Correction request:

```json
{
  "attendanceRecordId": "00000000-0000-0000-0000-000000000701",
  "requestedCheckInAtUtc": "2026-06-12T03:45:00Z",
  "requestedCheckOutAtUtc": "2026-06-12T12:45:00Z",
  "reason": "Forgot to check out"
}
```

Decision request:

```json
{
  "comments": "Approved after manager confirmation."
}
```

### 8.9 Shifts

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/shifts` | Authenticated | List shifts |
| `GET` | `/shifts/{id}` | Authenticated | Get shift |
| `POST` | `/shifts` | Admin, HR | Create shift |
| `PUT` | `/shifts/{id}` | Admin, HR | Update shift |
| `DELETE` | `/shifts/{id}` | Admin, HR | Soft delete shift |
| `GET` | `/employees/{employeeId}/shifts` | Admin, HR, Manager, Employee self | List employee shift assignments |
| `POST` | `/employees/{employeeId}/shifts` | Admin, HR | Assign shift |
| `PUT` | `/employees/{employeeId}/shifts/{assignmentId}` | Admin, HR | Update assignment |
| `DELETE` | `/employees/{employeeId}/shifts/{assignmentId}` | Admin, HR | End or delete assignment |

## 9. Leave APIs

### 9.1 Leave Types

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/leave-types` | Authenticated | List leave types |
| `GET` | `/leave-types/{id}` | Authenticated | Get leave type |
| `POST` | `/leave-types` | Admin, HR | Create leave type |
| `PUT` | `/leave-types/{id}` | Admin, HR | Update leave type |
| `DELETE` | `/leave-types/{id}` | Admin, HR | Soft delete leave type |
| `POST` | `/leave-types/{id}/restore` | Admin, HR | Restore a soft-deleted leave type |

### 9.2 Leave Requests

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/leave/requests` | Admin, HR, Manager, Employee self | List leave requests |
| `GET` | `/leave/requests/{id}` | Admin, HR, Manager, Employee self | Get leave request |
| `POST` | `/leave/requests` | Employee, Admin, HR | Apply leave |
| `PUT` | `/leave/requests/{id}` | Employee self while pending, Admin, HR | Update pending request |
| `POST` | `/leave/requests/{id}/approve` | Admin, HR, Manager | Approve request |
| `POST` | `/leave/requests/{id}/reject` | Admin, HR, Manager | Reject request |
| `POST` | `/leave/requests/{id}/cancel` | Employee self, Admin, HR | Cancel request |

Leave request:

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000101",
  "leaveTypeId": "00000000-0000-0000-0000-000000000801",
  "startDate": "2026-07-10",
  "endDate": "2026-07-12",
  "totalDays": 3,
  "reason": "Family event"
}
```

Decision request:

```json
{
  "comments": "Approved."
}
```

List query parameters:

| Parameter | Type | Notes |
| --- | --- | --- |
| `employeeId` | guid | Optional |
| `approverEmployeeId` | guid | Optional |
| `leaveTypeId` | guid | Optional |
| `status` | string | Pending, Approved, Rejected, Cancelled |
| `dateFrom` | date | Optional |
| `dateTo` | date | Optional |

### 9.3 Leave Balances

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/leave/balances` | Admin, HR, Manager, Employee self | List balances |
| `GET` | `/employees/{employeeId}/leave-balances` | Admin, HR, Manager, Employee self | Get employee balances |
| `PUT` | `/employees/{employeeId}/leave-balances/{leaveTypeId}` | Admin, HR | Adjust leave balance |

Balance adjustment request:

```json
{
  "year": 2026,
  "adjusted": 1.5,
  "reason": "Carry-forward correction"
}
```

### 9.4 Holidays

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/holidays` | Authenticated | List holidays |
| `GET` | `/holidays/{id}` | Authenticated | Get holiday |
| `POST` | `/holidays` | Admin, HR | Create holiday |
| `PUT` | `/holidays/{id}` | Admin, HR | Update holiday |
| `DELETE` | `/holidays/{id}` | Admin, HR | Soft delete holiday |

Query parameters: `officeLocationId`, `year`, `isOptional`

## 10. Dashboard APIs

### 10.1 Dashboard Summary

```text
GET /dashboard/summary
```

Access: Admin, HR, Manager

Query parameters:

| Parameter | Type | Notes |
| --- | --- | --- |
| `departmentId` | guid | Optional |
| `officeLocationId` | guid | Optional |
| `date` | date | Defaults to current date |

Response:

```json
{
  "data": {
    "totalEmployees": 1000,
    "activeEmployees": 950,
    "inactiveEmployees": 50,
    "attendance": {
      "present": 820,
      "absent": 70,
      "late": 45,
      "onLeave": 60
    },
    "leave": {
      "pending": 12,
      "approvedToday": 8,
      "rejectedToday": 1
    },
    "departments": [
      {
        "departmentId": "00000000-0000-0000-0000-000000000301",
        "departmentName": "Human Resources",
        "activeEmployees": 45
      }
    ]
  }
}
```

### 10.2 Employee Metrics

```text
GET /dashboard/employees
```

Access: Admin, HR, Manager

Returns employee counts by status, department, designation, location, and join month.

### 10.3 Attendance Metrics

```text
GET /dashboard/attendance
```

Access: Admin, HR, Manager

Query parameters: `dateFrom`, `dateTo`, `departmentId`, `managerId`, `officeLocationId`

### 10.4 Leave Metrics

```text
GET /dashboard/leave
```

Access: Admin, HR, Manager

Query parameters: `dateFrom`, `dateTo`, `departmentId`, `managerId`, `leaveTypeId`

### 10.5 My Dashboard

```text
GET /dashboard/me
```

Access: Employee

Returns current employee profile summary, today's attendance status, leave balances, pending leave requests, and upcoming holidays.

## 11. Lookup APIs

Lookup APIs help frontend forms avoid hardcoded values.

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/lookups/employee-statuses` | Authenticated | Employee status values |
| `GET` | `/lookups/attendance-statuses` | Authenticated | Attendance status values |
| `GET` | `/lookups/leave-statuses` | Authenticated | Leave request status values |
| `GET` | `/lookups/document-types` | Authenticated | Employee document types |
| `GET` | `/lookups/genders` | Authenticated | Gender values if configured |
| `GET` | `/lookups/time-zones` | Authenticated | Supported time zones |

## 12. Audit APIs

Audit APIs are useful for admin review and compliance.

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/audit-logs` | Admin | List audit logs |
| `GET` | `/audit-logs/{id}` | Admin | Get audit log entry |
| `GET` | `/audit-logs/entity/{entityName}/{entityId}` | Admin, HR | Get audit history for an entity |

Query parameters: `userId`, `entityName`, `entityId`, `action`, `dateFrom`, `dateTo`, `page`, `pageSize`

## 13. Export APIs

Reporting requirements include Excel and PDF export.

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/exports/employees` | Admin, HR | Export employees to Excel |
| `GET` | `/exports/attendance` | Admin, HR, Manager | Export attendance to Excel |
| `GET` | `/exports/leave-requests` | Admin, HR, Manager | Export leave requests to Excel |
| `GET` | `/exports/dashboard-summary` | Admin, HR, Manager | Export dashboard summary to PDF |

Export endpoints should accept the same filters as their list or dashboard endpoints.

## 14. Health And System APIs

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/health` | Public or internal | Basic API health |
| `GET` | `/health/ready` | Internal | Readiness check with database |
| `GET` | `/health/live` | Internal | Liveness check |

## 15. Authorization Policy Summary

| Policy | Used By |
| --- | --- |
| `CanManageEmployees` | Create, update, delete, restore employees |
| `CanViewEmployeeDetails` | Employee detail and employee documents |
| `CanManageDepartments` | Departments, teams, designations, locations |
| `CanRecordAttendance` | Check-in and check-out |
| `CanCorrectAttendance` | Manual attendance and correction decisions |
| `CanApplyLeave` | Create leave requests |
| `CanApproveLeave` | Approve or reject leave requests |
| `CanViewDashboard` | Dashboard metrics |
| `CanManageUsers` | Users and roles |
| `CanViewAuditLogs` | Audit log APIs |
| `CanManagePayroll` | Process payroll, manage salary structures, view payroll runs |
| `CanApprovePayroll` | Approve a completed payroll run |
| `CanViewReports` | Employee, department, leave, and turnover reports |

## 16. Missing But Recommended APIs

The requirements explicitly mention the main MVP modules, but these supporting APIs are recommended because the architecture and database design require them:

- `Users` and `Roles` for role-based access control.
- `Teams`, `Designations`, and `OfficeLocations` for department management.
- `EmployeeDocuments` and `ProfilePhoto` for employee files.
- `Shifts` and `EmployeeShifts` for shift attendance.
- `LeaveTypes`, `LeaveBalances`, and `Holidays` for leave management.
- `AuditLogs` for security and HR traceability.
- `Lookups` to keep frontend forms free from hardcoded values.
- `Exports` to satisfy Excel and PDF reporting requirements.
- `Health` endpoints for Azure deployment and monitoring.

## 17. Payroll APIs (Phase 2)

Base path: `/payroll`. All endpoints require authentication; per-endpoint access is noted below. Non-privileged (Employee-role) callers are always scoped to their own payslips regardless of any `employeeId` filter supplied.

### 17.1 Payroll Runs

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `POST` | `/payroll/process` | `CanManagePayroll` | Process payroll for a period: generates payslips and PDFs for all active employees |
| `POST` | `/payroll/dry-run` | `CanManagePayroll` | Preview payslip calculations for a period without persisting anything |
| `GET` | `/payroll/runs` | `CanManagePayroll` | List all payroll runs |
| `GET` | `/payroll/runs/{id}` | `CanManagePayroll` | Get a payroll run, including its payslips |
| `POST` | `/payroll/runs/{id}/approve` | `CanApprovePayroll` | Approve a completed payroll run. Only runs in `Completed` status can be approved; already-approved runs are rejected. The approver is always the authenticated caller — never a client-supplied value |

Process payroll request:

```json
{
  "periodStart": "2026-06-01",
  "periodEnd": "2026-06-30"
}
```

`processedBy` is derived from the authenticated caller and is not accepted from the client. `periodEnd` must not be in the future — payroll cannot be processed for a period that has not yet ended.

Payroll run response:

```json
{
  "id": "00000000-0000-0000-0000-000000000901",
  "periodStart": "2026-06-01",
  "periodEnd": "2026-06-30",
  "processedAtUtc": "2026-07-01T02:00:00Z",
  "processedBy": "00000000-0000-0000-0000-000000000010",
  "status": "Completed",
  "payslipCount": 42,
  "totalNetPay": 168400.00,
  "payslips": []
}
```

`status` is one of `Processing`, `Completed`, `Approved`.

### 17.2 Salary Structures

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/payroll/salary-structures` | `CanManagePayroll` | List salary structures |
| `GET` | `/payroll/salary-structures/{id}` | `CanManagePayroll` | Get a salary structure |
| `POST` | `/payroll/salary-structures` | `CanManagePayroll` | Create a salary structure for an employee |
| `PUT` | `/payroll/salary-structures/{id}` | `CanManagePayroll` | Update a salary structure |
| `DELETE` | `/payroll/salary-structures/{id}` | `CanManagePayroll` | Delete a salary structure (404 if it does not exist) |

Salary structure request:

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000101",
  "basicSalary": 5000.00,
  "allowances": [{ "name": "House", "amount": 500.00 }],
  "deductions": [{ "name": "Tax", "amount": 250.00 }],
  "effectiveFrom": "2026-01-01",
  "effectiveTo": null
}
```

### 17.3 Payslips

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/payroll/payslips?employeeId={id}` | Authenticated (self); `CanManagePayroll` for any employee | List payslips for an employee. `employeeId` is required for privileged callers and ignored (forced to self) for non-privileged callers |
| `GET` | `/payroll/payslips/{payslipId}/download` | Authenticated (self); `CanManagePayroll` for any employee | Download a payslip PDF. Returns 403 if a non-privileged caller requests another employee's payslip, 404 if the payslip or its document does not exist |

Payslip response:

```json
{
  "id": "00000000-0000-0000-0000-000000000701",
  "payrollRunId": "00000000-0000-0000-0000-000000000901",
  "employeeId": "00000000-0000-0000-0000-000000000101",
  "basic": 5000.00,
  "totalAllowances": 500.00,
  "totalDeductions": 250.00,
  "grossPay": 5500.00,
  "netPay": 5250.00,
  "generatedAtUtc": "2026-07-01T02:00:01Z",
  "hasDocument": true
}
```

## 18. Reports APIs

Base path: `/reports`. All endpoints require the `CanViewReports` policy (Admin, HR, Manager) — these expose aggregate, org-wide data and are never scoped to a single employee. This module is distinct from the `/exports` module described in section 13, which remains unbuilt.

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/reports/employees` | Total, active, and inactive employee counts |
| `GET` | `/reports/departments` | Employee headcount grouped by department |
| `GET` | `/reports/departments/export` | Department headcount report as a CSV file download |
| `GET` | `/reports/leave-summary?from={date}&to={date}` | Leave request counts by status (Pending/Approved/Rejected) within a date range |
| `GET` | `/reports/employee-turnover?from={date}&to={date}` | Employees who joined or exited within a date range |
| `GET` | `/reports/employee-turnover/export?from={date}&to={date}` | Employee turnover report as a CSV file download |

`from` and `to` are both required and `from` must be before or equal to `to` on every date-ranged endpoint; violations return `400 VALIDATION_ERROR`.

Department counts exclude soft-deleted departments. CSV exports neutralize formula/CSV injection (CWE-1236): any field value starting with `=`, `+`, `-`, `@`, tab, or CR is prefixed with `'` before being written, so a department or employee name cannot execute as a formula when the file is opened in Excel or Sheets.

