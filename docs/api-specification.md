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
| `429 Too Many Requests` | Rate limit exceeded (see [3.1](#31-login), [3.10](#310-register), and [§20](#20-client-master-apis)/[§21](#21-task-management-apis)/[§22](#22-reimbursement-management-apis)); response includes a `Retry-After` header |
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

Rate limited per client IP (default: 5 requests / 60 seconds, configurable via `RateLimiting:Login` in
app configuration). Exceeding the limit returns `429 Too Many Requests` with a `Retry-After` header and
body `{ "status": 429, "code": "RATE_LIMIT_EXCEEDED", "message": "Too many requests. Please try again later." }`.
This budget is independent of the [Register](#310-register) endpoint's.

### 3.2 Verify MFA

```text
POST /auth/mfa/verify
```

Access: Public

Completes a login that returned `requiresMfa: true`. `code` accepts either a 6-digit TOTP code
from the authenticator app, or one of the account's recovery codes (`XXXXX-XXXXX` format) as a
fallback — either satisfies the request. A recovery code is consumed (single-use) the moment it's
accepted.

Request:

```json
{
  "mfaChallengeId": "challenge-id",
  "code": "123456"
}
```

Response `200 OK`:

```json
{
  "data": {
    "accessToken": "jwt",
    "refreshToken": "refresh-token",
    "expiresInSeconds": 900
  }
}
```

The challenge expires 5 minutes after `POST /auth/login` issued it, and is single-use — a second
`POST /auth/mfa/verify` against the same `mfaChallengeId` (successful or not) returns `401`. An
unknown, expired, already-consumed challenge ID, or a wrong code all return the same generic `401`
(`"Invalid or expired verification code."`) — the response never reveals which of those it was, the
same way `POST /auth/login` never reveals whether the username or the password was wrong.

Rate limited per client IP (default: 10 requests / 60 seconds, configurable via
`RateLimiting:MfaVerify`), independently of the [Login](#31-login)/[Register](#310-register)
budgets — a 6-digit TOTP code is only 1,000,000 possibilities, so this endpoint needs a tighter
budget than password-based endpoints to resist brute-forcing a single challenge within its
validity window.

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

Returns the current authenticated user's account, employee link, roles, and permissions,
including `isMfaEnabled`.

### 3.10 Register

```text
POST /auth/register
```

Access: Public

Request:

```json
{
  "userName": "jsmith",
  "email": "jsmith@example.com",
  "password": "Password@123"
}
```

Response: same token response as login.

There is no `roleId` field on this request, and none is accepted even if supplied — a
self-registered account can never choose its own role. Every account is created with no role
(the lowest-privilege `Employee` claim is issued by default) **except** the very first account
ever created on a fresh deployment, which is automatically granted `Admin`. This bootstrap exists
because there is otherwise no way to reach the `Admin`-only [Users API](#4-user-and-role-administration-apis)
at all on a brand-new system. Every registration after the first gets no role, regardless of
input — an existing Admin must assign one afterward via `PUT /users/{id}`.

Rate limited per client IP (default: 5 requests / 60 seconds, configurable via `RateLimiting:Register`
in app configuration), independently of the [Login](#31-login) endpoint's budget. Exceeding the limit
returns `429 Too Many Requests` with a `Retry-After` header, same error body shape as login.

### 3.11 MFA Setup

```text
POST /auth/mfa/setup
```

Access: Authenticated

Starts enrollment: generates a new TOTP secret for the caller and returns it as a manual-entry
key plus an `otpauth://` provisioning URI. The frontend renders that URI as a QR code
client-side (e.g. with a JS QR library) for scanning into an authenticator app — no QR image is
generated server-side. MFA is **not** enabled by this call; it only takes effect once a code is
confirmed via [3.12](#312-enable-mfa). Calling this again before confirming replaces the pending
secret. Returns `409 Conflict` if MFA is already enabled.

Response `200 OK`:

```json
{
  "data": {
    "manualEntryKey": "JBSWY3DPEHPK3PXP",
    "otpAuthUri": "otpauth://totp/EMS:hr@example.com?secret=JBSWY3DPEHPK3PXP&issuer=EMS&algorithm=SHA1&digits=6&period=30"
  }
}
```

### 3.12 Enable MFA

```text
POST /auth/mfa/enable
```

Access: Authenticated

Confirms enrollment with a code from the authenticator app. On success, turns MFA on and returns
10 one-time recovery codes — **shown only in this response, never retrievable again**. Returns
`401` for a wrong code, `409` if MFA is already enabled or [3.11](#311-mfa-setup) hasn't been
called yet.

Request:

```json
{
  "code": "123456"
}
```

Response `200 OK`:

```json
{
  "data": {
    "recoveryCodes": ["K7M9X-4RT2W", "..."]
  },
  "message": "MFA enabled. Store these recovery codes securely — they will not be shown again."
}
```

### 3.13 Disable MFA

```text
POST /auth/mfa/disable
```

Access: Authenticated

Turns MFA off and permanently invalidates all recovery codes. Requires the account password as
confirmation — an authenticated session alone (e.g. a stolen or left-open access token) is not
enough to disable MFA. Returns `401` for an incorrect password.

Request:

```json
{
  "password": "Password@123"
}
```

Response: `204 No Content`

### 3.14 Regenerate MFA Recovery Codes

```text
POST /auth/mfa/recovery-codes/regenerate
```

Access: Authenticated

Invalidates every existing recovery code (used or not) and issues 10 new ones. Requires the
account password as confirmation, same as [3.13](#313-disable-mfa). Returns `401` for an
incorrect password, `409` if MFA is not enabled.

Request:

```json
{
  "password": "Password@123"
}
```

Response `200 OK`:

```json
{
  "data": {
    "recoveryCodes": ["Q4XN8-7WKPT", "..."]
  },
  "message": "New recovery codes issued. Store them securely — they will not be shown again."
}
```

## 4. User And Role Administration APIs

These APIs support RBAC management and are required for a complete administration experience.

> **Implementation note:** the current implementation uses a single-role-per-user model
> (`User.RoleId`), not the many-to-many `UserRoles` join table described in
> [database-design.md §4.1–4.3](database-design.md). This is a deliberate, minimal-scope
> decision — see the note at the top of database-design.md §4 for the full rationale and
> what would be required to close the gap. Because of this, request/response bodies below
> use a singular `roleId` rather than `roleIds`, and there is no `PUT /users/{id}/roles`
> endpoint — role assignment happens via `POST /users` and `PUT /users/{id}`. Audit/soft-delete
> fields are also a reduced set (`isDeleted`, `createdAtUtc`, `updatedAtUtc`) rather than the
> full audit set (`createdBy`, `updatedBy`, `deletedBy`, `deletedAtUtc`, `rowVersion`) used
> elsewhere in the schema.

### 4.1 Users

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/users` | Admin | List users. Query params: `includeDeleted` (bool, default `false`), `roleId` (guid), `isActive` (bool) |
| `GET` | `/users/{id}` | Admin | Get user details |
| `POST` | `/users` | Admin | Create user account |
| `PUT` | `/users/{id}` | Admin | Update user account (username, email, roleId, employeeId) |
| `PATCH` | `/users/{id}/status` | Admin | Activate or deactivate user. Deactivating revokes all of the user's active refresh tokens |
| `DELETE` | `/users/{id}` | Admin | Soft delete user. Revokes all of the user's active refresh tokens |
| `POST` | `/users/{id}/restore` | Admin | Restore a soft-deleted user account |

Create user request:

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000101",
  "userName": "jsmith",
  "email": "jsmith@example.com",
  "temporaryPassword": "Password@123",
  "roleId": "00000000-0000-0000-0000-000000000201",
  "isActive": true
}
```

User response:

```json
{
  "id": "00000000-0000-0000-0000-000000000301",
  "userName": "jsmith",
  "email": "jsmith@example.com",
  "isActive": true,
  "roleId": "00000000-0000-0000-0000-000000000201",
  "roleName": "HR",
  "employeeId": "00000000-0000-0000-0000-000000000101",
  "isDeleted": false,
  "createdAtUtc": "2026-07-22T10:00:00Z",
  "updatedAtUtc": null
}
```

`temporaryPassword` must be at least 8 characters and include an uppercase letter, a
lowercase letter, a digit, and a special character (same rule as `POST /auth/change-password`).
The password is never returned in any response.

### 4.2 Roles

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/roles` | Admin, HR | List roles. Query param: `includeDeleted` (bool, default `false`) |
| `GET` | `/roles/{id}` | Admin | Get role details |
| `POST` | `/roles` | Admin | Create role |
| `PUT` | `/roles/{id}` | Admin | Update role |
| `DELETE` | `/roles/{id}` | Admin | Soft delete role. Fails with `409 Conflict` if any active (non-deleted) user is still assigned to the role |
| `POST` | `/roles/{id}/restore` | Admin | Restore a soft-deleted role |

Create/update role request:

```json
{
  "name": "Auditor",
  "description": "Read-only compliance access"
}
```

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

Note: unlike most controllers in this API, `EmployeeDocumentsController` returns raw JSON bodies
(a bare array from List, a bare id from Upload) rather than the `{ data, message, correlationId }`
envelope from [§2.3](#23-standard-success-response). Frontend clients must not unwrap these
responses. Employee-self access is enforced by comparing the caller's user id directly to the
`employeeId` route parameter (i.e. an employee's id and their linked user account's id must match)
— the Manager-for-team and role-scoped download restrictions described below are not currently
enforced in code and are tracked as a gap, not a contract to build against.

### 6.1 List Employee Documents

```text
GET /employees/{employeeId}/documents
```

Access: any authenticated user (see note above on Manager/self enforcement not yet being in code)

Query parameters: `documentType`, `page`, `pageSize`, `search`

### 6.2 Upload Employee Document

```text
POST /employees/{employeeId}/documents
```

Access: Admin, HR, or the employee themselves (self-upload is authorized by matching the caller's
user id to `employeeId`)

Content type: `multipart/form-data`

Form fields:

| Field | Type | Notes |
| --- | --- | --- |
| `file` | file | Required. Allowed types: `application/pdf`, `image/jpeg`, `image/png`, `application/msword`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`. Max size 10 MB (see `UploadDocumentCommandHandler`) |
| `documentType` | string | Required. Free-form string; conventional values are `ID Proof`, `OfferLetter`, `NDA`, `Appraisal`, `Payslip`, `Other` |
| `expiresAtUtc` | datetime | Optional |

Response: `201 Created` with a bare document id in the body.

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
  "notes": "Office check-in",
  "latitude": 12.9716,
  "longitude": 77.5946
}
```

Employees can check in only for themselves. Admin and HR can record on behalf of an employee.

GPS & Location Tracking (see [requirements.md](requirements.md#gps--location-tracking-planned-enhancement) and [database-design.md §6.3](database-design.md#63-attendancerecords)): `latitude`/`longitude` are required — captured by the client's Geolocation API and validated to `[-90, 90]`/`[-180, 180]`. `deviceInfo` and `ipAddress` are **not** part of the request body; the server derives them from the `User-Agent` header and the connection's remote IP respectively, and reverse-geocodes the coordinates into a human-readable `checkInAddress` (best-effort — a geocoding provider outage never blocks the check-in; the address is simply omitted). Office geofencing (rejecting a check-in outside a configurable radius) is a future enhancement, not enforced here.

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
  "notes": "Office check-out",
  "latitude": 13.0827,
  "longitude": 80.2707
}
```

Same GPS semantics as Check In (§8.1). Check-out location is stored independently of check-in location — a check-out may legitimately happen off-premises (client visit, field work) and is always recorded regardless of where it occurs.

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

An approver cannot approve/reject their own leave request, checked against the approver's own linked `employeeId` regardless of role. Every approve/reject decision is written to `AuditLogs` (entity `LeaveRequest`).

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

Reporting requirements include Excel and PDF export. Export endpoints return the file directly
(no `data`/`correlationId` envelope) via `Content-Disposition: attachment`; errors still use the
standard error response (`api-specification.md §2.4`).

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/exports/employees` | Admin, HR | Export employees to Excel |
| `GET` | `/exports/attendance` | Admin, HR, Manager | Export attendance to Excel |
| `GET` | `/exports/leave-requests` | Admin, HR, Manager | Export leave requests to Excel |
| `GET` | `/exports/dashboard-summary` | Admin, HR, Manager | Export dashboard summary to PDF |
| `GET` | `/exports/reimbursements` | Authenticated (self-scoped) | Export reimbursements to Excel. Non-Admin callers only ever see their own, regardless of filters supplied |
| `GET` | `/exports/assets` | Admin, HR | Export assets to Excel, including each asset's current assignee |
| `GET` | `/exports/candidates` | Admin, HR | Export candidates to Excel |

Export endpoints accept the same filters as their list or dashboard endpoints (§5.1, §8.3, §9.2,
§10.1), scoped to the filters currently implemented by those endpoints.

### 13.1 Export Employees

Query parameters: `search`, `sortBy`, `sortDir`, `departmentId`, `status` (same meaning as §5.1).

Response: `200 OK`, `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`,
file name `employees_{yyyyMMddHHmmss}.xlsx`.

### 13.2 Export Attendance

Query parameters: `employeeId`, `departmentId`, `managerId`, `dateFrom`, `dateTo`, `status`,
`isLateArrival`, `isEarlyLeave` (same meaning as §8.3). Manager-role callers are scoped to their
own team regardless of the `employeeId`/`managerId` filters supplied, matching the scoping rules
of the list endpoint.

Response: `200 OK`, `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`,
file name `attendance_{yyyyMMddHHmmss}.xlsx`.

### 13.3 Export Leave Requests

Query parameters: `employeeId`, `leaveTypeId`, `year`, `status` (same meaning as §9.2). Only
Admin, HR, and Manager can call this endpoint, so no self-service scoping is applied.

Response: `200 OK`, `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`,
file name `leave-requests_{yyyyMMddHHmmss}.xlsx`.

### 13.4 Export Dashboard Summary

Query parameters: `departmentId`, `officeLocationId`, `date` (same meaning as §10.1).

Response: `200 OK`, `Content-Type: application/pdf`, file name `dashboard-summary_{yyyyMMdd}.pdf`.

### 13.5 Export Reimbursements

Query parameters: `employeeId`, `status` (same meaning as [§22](#22-reimbursement-management-apis)'s `GET /reimbursements` list endpoint). A non-Admin caller is always scoped to their own reimbursements, regardless of any `employeeId` filter supplied — matching the list endpoint's rule exactly.

Response: `200 OK`, `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, file name `reimbursements_{yyyyMMddHHmmss}.xlsx`.

### 13.6 Export Assets

Query parameters: `status`, `category`, `search` (same meaning as §24.1's list endpoint). Each row includes a "Currently Assigned To" column resolved from the asset's active (not yet returned) assignment, if any.

Response: `200 OK`, `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, file name `assets_{yyyyMMddHHmmss}.xlsx`.

### 13.7 Export Candidates

Query parameters: `status`, `designationId`, `search` (same meaning as §23.1's list endpoint).

Response: `200 OK`, `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, file name `candidates_{yyyyMMddHHmmss}.xlsx`.

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
| `CanManageUsers` | Create, update, activate/deactivate, delete, restore users; create, update, delete, restore roles; get role details |
| `CanViewRoles` | List roles (read-only) |
| `CanViewAuditLogs` | Audit log APIs |
| `CanManagePayroll` | Process payroll, manage salary structures, view payroll runs |
| `CanApprovePayroll` | Approve a completed payroll run |
| `CanViewReports` | Employee, department, leave, and turnover reports |
| `CanManageAnnouncements` (`Admin,HR` roles) | Create and retract company-wide announcements |
| `CanManageClients` (`Admin` role only) | Create, update, delete, activate, deactivate, archive, restore clients — deliberately not delegated to HR |
| `CanManageTasks` (`Admin` role only) | Create, edit, reassign, cancel tasks — "Only Admin can assign tasks" per requirements.md. Accept/reject/start/progress/complete/comment/attach are open to any authenticated caller but scoped to the task's assignee at the handler level (see §21) |
| `CanManageReimbursements` (`Admin` role only) | Start review, approve, reject, request changes on reimbursements. Self-approval is additionally blocked at the handler level even for Admin callers (see §22) |
| `CanManageRecruitment` (`Admin,HR` roles) | Candidates, interview scheduling/cancel/reschedule, offers, onboarding checklist, convert-to-employee. Interview feedback submission is open to any authenticated caller but scoped to the interview's assigned interviewer at the handler level (see §23) |
| `CanManageAssets` (`Admin,HR` roles) | Assets, allocation, return tracking (see §24) — no self-service angle, unlike Task Management or Reimbursements |
| `CanManagePerformance` (`Admin,HR,Manager` roles) | Create/edit Goals, add KPIs, start Reviews, propose Promotions (see §25) — the first policy to include `Manager`, gated further at the handler level to a caller's own direct reports unless Admin/HR |
| `CanApprovePromotions` (`Admin,HR` roles) | Approve/Reject/Delete/Restore Promotions (see §25.3) — stricter than `CanManagePerformance` so a Manager cannot approve their own proposal |
| `CanManageMessaging` (`Admin,HR` roles) | Delete/Restore a Conversation (see §26) — Admin/HR-only moderation; everyday messaging (create, send, list, read, add participants, leave) is open to any authenticated caller and scoped at the handler level to the caller being an active participant |

## 16. Missing But Recommended APIs

The requirements explicitly mention the main MVP modules, but these supporting APIs are recommended because the architecture and database design require them:

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
  "periodEnd": "2026-06-30",
  "adjustments": [
    { "employeeId": "00000000-0000-0000-0000-000000000101", "bonusAmount": 200.00, "overtimeAmount": null }
  ]
}
```

`processedBy` is derived from the authenticated caller and is not accepted from the client. `periodEnd` must not be in the future — payroll cannot be processed for a period that has not yet ended.

`adjustments` is optional and per-employee. `bonusAmount` is the *only* way Bonus gets onto a payslip — it's discretionary, so there's nothing to auto-calculate; employees not listed (or with `bonusAmount` omitted) get `totalBonus: 0`. `overtimeAmount`, when supplied, **overrides** the auto-calculated overtime for that employee (see §17.3 for the auto-calculation); when omitted, Overtime is auto-calculated from Attendance vs. the employee's assigned shift. Both amounts must be `>= 0` when supplied.

Payroll run response:

```json
{
  "id": "00000000-0000-0000-0000-000000000901",
  "periodStart": "2026-06-01",
  "periodEnd": "2026-06-30",
  "processedAtUtc": "2026-07-01T02:00:00Z",
  "processedBy": "00000000-0000-0000-0000-000000000010",
  "status": "Completed",
  "updatedAtUtc": null,
  "updatedBy": null,
  "payslipCount": 42,
  "totalNetPay": 168400.00,
  "payslips": []
}
```

`updatedAtUtc`/`updatedBy` are `null` until the run is approved, then hold the approval timestamp and the approver's user ID (redundant with nothing else on this record — `processedBy` is who ran payroll, `updatedBy` is who approved it, and they're often different people).

`status` is one of `Processing`, `Completed`, `Approved`.

Processing a payroll run and approving it are both written to `AuditLogs` (entity `PayrollRun`, actions `Processed`/`Approved`) — money movement is treated as a sensitive operation on par with Leave approvals and employee lifecycle changes.

`POST /payroll/dry-run` accepts the same `adjustments` field, previewing exactly what `/payroll/process` would produce for that input.

### 17.2 Salary Structures

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/payroll/salary-structures` | `CanManagePayroll` | List salary structures |
| `GET` | `/payroll/salary-structures/{id}` | `CanManagePayroll` | Get a salary structure |
| `POST` | `/payroll/salary-structures` | `CanManagePayroll` | Create a salary structure for an employee |
| `PUT` | `/payroll/salary-structures/{id}` | `CanManagePayroll` | Update a salary structure |
| `DELETE` | `/payroll/salary-structures/{id}` | `CanManagePayroll` | Delete (soft) a salary structure (404 if it does not exist) |
| `POST` | `/payroll/salary-structures/{id}/restore` | `CanManagePayroll` | Restore a soft-deleted salary structure (404 if it does not exist) |

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

Salary structure response adds `isDeleted`, `createdAtUtc`, `updatedAtUtc` (see [database-design.md §15.1](database-design.md#151-salarystructures)). `DELETE` soft-deletes rather than removing the row — a deleted structure is excluded from `GET`/list, and from the effective-salary-structure lookup Payroll processing uses, until restored. This is the only Payroll table with a full audit + soft-delete lifecycle; `Allowances`/`Deductions`/`PayrollRuns`/`Payslips` don't have one, for reasons documented in database-design.md §15's implementation note.

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
  "totalReimbursements": 75.00,
  "totalBonus": 200.00,
  "totalOvertime": 45.00,
  "overtimeHours": 3.0,
  "grossPay": 5745.00,
  "netPay": 5570.00,
  "generatedAtUtc": "2026-07-01T02:00:01Z",
  "hasDocument": true
}
```

`grossPay = basic + totalAllowances + totalBonus + totalOvertime` (Bonus and Overtime are taxable earnings). `netPay = grossPay - totalDeductions + totalReimbursements` (Reimbursements are expense repayments, not earnings — see §22.1). `overtimeHours` is the hour count behind an auto-calculated `totalOvertime`; it's `0` when `totalOvertime` came from a manual `adjustments[].overtimeAmount` override (an amount, not a derived hour count) — see §17.1 for how Bonus/Overtime get onto a run, and [database-design.md §15.5](database-design.md#155-payslips) for the auto-calculation formula.

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

## 19. Notification And Announcement APIs (Phase 2)

Two distinct broadcast mechanisms exist: personal `Notifications` (per-user, e.g. leave-decision or attendance alerts) and company-wide `Announcements` (broadcast to everyone, or optionally scoped to one department or one role). See [database-design.md §9](database-design.md#9-notifications-and-announcement-tables) for the underlying schema.

Note: like the Employee Document APIs ([§6](#6-employee-document-apis)), `NotificationsController` and
`AnnouncementsController` return raw JSON bodies (bare arrays/ids) rather than the
`{ data, message, correlationId }` envelope from [§2.3](#23-standard-success-response). Frontend
clients must not unwrap these responses.

### 19.1 Notifications

Base path: `/notifications`. All endpoints require authentication.

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `POST` | `/notifications` | `Admin,HR` roles | Create a personal notification for a specific user |
| `GET` | `/notifications/user/{userId}` | Authenticated (self); `Admin` for any user | List notifications for a user, paginated. Returns 403 if a non-Admin caller requests another user's notifications |
| `POST` | `/notifications/{id}/mark-read` | Authenticated | Mark a notification as read |

Query parameters on the list endpoint: `page` (default 1), `pageSize` (default 20), `onlyUnread` (default `false`).

Create notification request:

```json
{
  "userId": "00000000-0000-0000-0000-000000000101",
  "title": "Leave Approved",
  "message": "Your leave request for 2026-08-01 to 2026-08-03 was approved.",
  "channel": "InApp",
  "expiresAtUtc": null
}
```

`channel` is `InApp` or `Email`. When `channel` is `Email`, delivery failures are logged but do not fail the request — the in-app row is always created.

Notification response:

```json
{
  "id": "00000000-0000-0000-0000-000000001001",
  "userId": "00000000-0000-0000-0000-000000000101",
  "title": "Leave Approved",
  "message": "Your leave request for 2026-08-01 to 2026-08-03 was approved.",
  "channel": "InApp",
  "isRead": false,
  "createdAtUtc": "2026-07-20T09:00:00Z",
  "expiresAtUtc": null
}
```

### 19.2 Announcements

Base path: `/announcements`. All endpoints require authentication; create and retract additionally require the `CanManageAnnouncements` (`Admin,HR`) roles.

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `POST` | `/announcements` | `Admin,HR` roles | Broadcast a new company-wide announcement |
| `GET` | `/announcements` | Authenticated | List announcements visible to the caller, paginated, most recent first |
| `GET` | `/announcements/{id}` | Authenticated | Get a single announcement. Returns 404 if it does not exist, is expired, is retracted, or is not visible to the caller's department/role |
| `POST` | `/announcements/{id}/mark-read` | Authenticated | Mark an announcement as read by the caller |
| `DELETE` | `/announcements/{id}` | `Admin,HR` roles | Retract (soft-delete) an announcement |

Query parameters on the list endpoint: `page` (default 1), `pageSize` (default 20), `onlyUnread` (default `false`).

The caller's visible list is filtered server-side from the authenticated user's id and role claim — there is no client-suppliable `userId`/`departmentId` filter. An announcement is visible to a caller when it is not retracted, not expired, and either `audienceType` is `All`, or `audienceType` is `Department` and the caller's employee record belongs to the announcement's `departmentId`, or `audienceType` is `Role` and the caller's role matches `targetRole`.

Create announcement request:

```json
{
  "title": "Office Closed for Maintenance",
  "message": "The Bengaluru office will be closed on 2026-08-15 for electrical maintenance.",
  "priority": "Important",
  "audienceType": "All",
  "departmentId": null,
  "targetRole": null,
  "expiresAtUtc": "2026-08-16T00:00:00Z"
}
```

`priority` is one of `Normal`, `Important`, `Critical` (default `Normal`). `audienceType` is one of `All`, `Department`, `Role` (default `All`). `departmentId` is required when `audienceType` is `Department`; `targetRole` is required when `audienceType` is `Role`. `expiresAtUtc`, if provided, must be in the future. `createdByUserId` is derived from the authenticated caller and is not accepted from the client.

Announcement response:

```json
{
  "id": "00000000-0000-0000-0000-000000001101",
  "title": "Office Closed for Maintenance",
  "message": "The Bengaluru office will be closed on 2026-08-15 for electrical maintenance.",
  "priority": "Important",
  "audienceType": "All",
  "departmentId": null,
  "targetRole": null,
  "createdByUserId": "00000000-0000-0000-0000-000000000010",
  "createdAtUtc": "2026-07-23T10:00:00Z",
  "expiresAtUtc": "2026-08-16T00:00:00Z",
  "isReadByMe": false
}
```

Delivery is poll-based, not real-time: the frontend fetches `GET /announcements` on load and on an interval (see [architecture.md §8](architecture.md#8-cross-cutting-concerns)). There is no SignalR or push infrastructure in this system today.

## 20. Client Master APIs

Base path: `/clients`. See [database-design.md §16](database-design.md#16-client-tables) for the underlying schema and [requirements.md](requirements.md#client-master-new-module--supports-task-management) for the source requirement. `GET` endpoints require only authentication — Employees will be scoped to clients linked to their assigned tasks once Task Management ships, but that scoping isn't implemented yet, so every authenticated caller currently sees every client. All mutating endpoints require the `CanManageClients` policy (`Admin` role only — intentionally not delegated to HR, unlike every other master-data module in this API).

Every endpoint in this controller (all of §20, §21, and §22) shares one `WriteActionPolicy` rate-limit budget per client IP (default: 100 requests / 60 seconds, configurable via `RateLimiting:WriteAction`) — deliberately combined across all three of these newer modules rather than one budget each, so a caller can't dodge the limit by round-robining across them. Exceeding it returns `429 Too Many Requests`, same response shape as [Login](#31-login). File-upload endpoints (`POST /tasks/{id}/attachments`, `POST /reimbursements/{id}/attachments`) instead use a separate, tighter `AttachmentUploadPolicy` budget (default: 20 requests / 60 seconds, configurable via `RateLimiting:AttachmentUpload`), independent of the shared write budget.

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/clients` | Authenticated | List clients — search, active-status filter, pagination |
| `GET` | `/clients/{id}` | Authenticated | Get a single client |
| `POST` | `/clients` | `CanManageClients` | Create a client |
| `PUT` | `/clients/{id}` | `CanManageClients` | Update a client |
| `DELETE` | `/clients/{id}` | `CanManageClients` | Soft delete a client |
| `POST` | `/clients/{id}/activate` | `CanManageClients` | Activate a client (eligible for new tasks) |
| `POST` | `/clients/{id}/deactivate` | `CanManageClients` | Deactivate a client (blocks new tasks; history retained) |
| `POST` | `/clients/{id}/archive` | `CanManageClients` | Archive a client — retires it from active workflows (also deactivates it) while keeping it distinct from a soft delete |
| `POST` | `/clients/{id}/restore` | `CanManageClients` | Restore a client — reverses whichever terminal state applies: un-deletes a soft-deleted client, or un-archives an archived one |

Query parameters on the list endpoint: `page` (default 1), `pageSize` (default 20, max 100), `search` (matches `clientName`, `companyName`, `contactPerson`, or `email`), `isActive` (optional boolean filter).

Create/update request body:

```json
{
  "clientName": "Acme Retail",
  "companyName": "Acme Corp",
  "contactPerson": "Jane Doe",
  "mobileNumber": "+1-555-0100",
  "alternateMobile": null,
  "email": "jane@acme.example",
  "gstNumber": null,
  "addressLine1": "1 Market Street",
  "addressLine2": null,
  "city": "San Francisco",
  "state": "CA",
  "country": "USA",
  "postalCode": "94105",
  "latitude": 37.7936,
  "longitude": -122.3965,
  "notes": null
}
```

`clientName` must be unique among non-deleted clients (`409`-equivalent `VALIDATION_ERROR` on conflict, enforced by `CreateClientCommandValidator`/`UpdateClientCommandValidator`, not a database constraint violation). `email` must be a valid email address. `latitude`/`longitude` are optional and independently validated to `[-90, 90]` / `[-180, 180]` when supplied.

Client response:

```json
{
  "id": "00000000-0000-0000-0000-000000001201",
  "clientName": "Acme Retail",
  "companyName": "Acme Corp",
  "contactPerson": "Jane Doe",
  "mobileNumber": "+1-555-0100",
  "alternateMobile": null,
  "email": "jane@acme.example",
  "gstNumber": null,
  "addressLine1": "1 Market Street",
  "addressLine2": null,
  "city": "San Francisco",
  "state": "CA",
  "country": "USA",
  "postalCode": "94105",
  "latitude": 37.7936,
  "longitude": -122.3965,
  "notes": null,
  "isActive": true,
  "isArchived": false,
  "isDeleted": false,
  "createdAtUtc": "2026-07-26T09:00:00Z",
  "updatedAtUtc": null
}
```

The `activate`/`deactivate`/`archive`/`restore`/`delete` endpoints return `204 No Content` on success, matching the Employee status-management endpoints ([§5.6](#56-update-employee-status)).

Business rules enforced today: client names must be unique (see above); soft-deleted clients are excluded from `GET /clients` and `GET /clients/{id}` (`404`) until restored. The "inactive clients cannot receive new tasks" rule is now enforced — see [§21](#21-task-management-apis), `CreateTaskCommandValidator`.

## 21. Task Management APIs

Base path: `/tasks`. See [database-design.md §17](database-design.md#17-task-tables) for the underlying schema and [requirements.md](requirements.md#task-management) for the source requirement. Create/Edit/Reassign/Cancel require the `CanManageTasks` policy (`Admin` role only — "Only Admin can assign tasks"). Everything else is open to any authenticated caller but scoped at the handler level: non-Admin callers may only act on tasks assigned to their own linked Employee record, mirroring the Attendance check-in/out privileged-override pattern (`RequestingUserId`/`IsPrivileged`, resolved server-side from the JWT, never client-supplied).

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/tasks` | Authenticated | List tasks — paginated, filterable by `assignedEmployeeId`, `clientId`, `status`, `priority`. Non-Admin callers are always scoped to their own tasks regardless of `assignedEmployeeId` |
| `GET` | `/tasks/{id}` | Authenticated | Get a single task. Returns `404` (not `403`) for a non-Admin, non-assignee caller — existence is not disclosed |
| `POST` | `/tasks` | `CanManageTasks` | Create (and assign) a task |
| `PUT` | `/tasks/{id}` | `CanManageTasks` | Edit task details. Rejected once the task is `Completed` or `Cancelled` |
| `POST` | `/tasks/{id}/reassign` | `CanManageTasks` | Reassign to a different employee; resets `Status` to `Assigned` regardless of where the previous assignee had gotten to |
| `POST` | `/tasks/{id}/cancel` | `CanManageTasks` | Cancel a task. Rejected if already `Completed` |
| `POST` | `/tasks/{id}/accept` | Authenticated (assignee or Admin) | `Assigned` → `Accepted` |
| `POST` | `/tasks/{id}/reject` | Authenticated (assignee or Admin) | `Assigned` → `Rejected`. Optional `{ "reason": "..." }` body, appended to `Notes` |
| `POST` | `/tasks/{id}/start` | Authenticated (assignee or Admin) | `Accepted` → `InProgress` |
| `POST` | `/tasks/{id}/progress` | Authenticated (assignee or Admin) | The "Update progress" action — toggles between `InProgress` and `OnHold`. Body: `{ "status": "OnHold" }` |
| `POST` | `/tasks/{id}/complete` | Authenticated (assignee or Admin) | `InProgress`/`OnHold` → `Completed`, sets `completedAtUtc`. The task becomes read-only afterward — every mutating endpoint above, plus comments and attachments, reject a `Completed` or `Cancelled` task |
| `GET` | `/tasks/{id}/comments` | Authenticated (assignee or Admin) | List the progress/notes log, chronological |
| `POST` | `/tasks/{id}/comments` | Authenticated (assignee or Admin) | The "Add notes" action. Body: `{ "comment": "..." }` |
| `GET` | `/tasks/{id}/attachments` | Authenticated (assignee or Admin) | List attachments |
| `POST` | `/tasks/{id}/attachments` | Authenticated (assignee or Admin) | The "Upload photos" action. Multipart form upload (`file`); PDF/JPEG/PNG only, magic-byte verified, 10 MB max — same constraints as [Employee Document upload](#62-upload-employee-document) |
| `GET` | `/tasks/attachments/{attachmentId}/download` | Authenticated (assignee or Admin) | Download an attachment |

Create/update request body:

```json
{
  "title": "Deliver quarterly report",
  "description": "Present Q3 numbers to the client's finance team.",
  "clientId": "00000000-0000-0000-0000-000000001201",
  "assignedEmployeeId": "00000000-0000-0000-0000-000000000101",
  "dueDate": "2026-08-15T00:00:00Z",
  "priority": "High",
  "notes": null
}
```

`assignedEmployeeId` must reference an existing employee (`create` only — `update` doesn't change the assignee, use `/reassign`). `clientId` is optional (not every task is a client visit) but when supplied must reference an existing, **active** client — this is the "inactive clients cannot receive new tasks" rule from requirements.md. `dueDate`, if supplied, cannot be in the past.

Task response:

```json
{
  "id": "00000000-0000-0000-0000-000000001301",
  "taskNumber": "TSK-3F2A9B10",
  "title": "Deliver quarterly report",
  "description": "Present Q3 numbers to the client's finance team.",
  "clientId": "00000000-0000-0000-0000-000000001201",
  "clientName": "Acme Retail",
  "clientAddress": "1 Market Street, San Francisco, CA, USA",
  "clientLatitude": 37.7936,
  "clientLongitude": -122.3965,
  "assignedEmployeeId": "00000000-0000-0000-0000-000000000101",
  "assignedEmployeeName": "Jane Doe",
  "assignedByUserId": "00000000-0000-0000-0000-000000000010",
  "assignedDate": "2026-07-26T09:00:00Z",
  "dueDate": "2026-08-15T00:00:00Z",
  "priority": "High",
  "status": "Assigned",
  "notes": null,
  "completedAtUtc": null,
  "createdAtUtc": "2026-07-26T09:00:00Z",
  "updatedAtUtc": null
}
```

`clientName`/`clientAddress`/`clientLatitude`/`clientLongitude` are denormalized onto the response so the mobile client can open the location in Maps ("Open client location in Maps") without a second round trip; they're `null` when the task has no client.

Status values: `Assigned`, `Accepted`, `Rejected`, `InProgress`, `OnHold`, `Completed`, `Cancelled`. `Rejected` is one more than the six requirements.md names explicitly — see [database-design.md §17.1](database-design.md#171-tasks) for why. Priority values: `Low`, `Medium`, `High`, `Critical`.

Business rules enforced: only Admin can create/edit/reassign/cancel a task; a non-Admin caller can only accept/reject/start/update-progress/complete/comment-on/attach-to a task assigned to their own employee record; a `Completed` or `Cancelled` task is read-only to every mutating endpoint in this module; `accept`/`reject` only from `Assigned`, `start` only from `Accepted`, `progress`/`complete` only from `InProgress`/`OnHold`; every status change is written to `AuditLogs` (entity `Task`).

## 22. Reimbursement Management APIs

Base path: `/reimbursements`. See [database-design.md §18](database-design.md#18-reimbursement-tables) for the underlying schema and [requirements.md](requirements.md#expense-management-employee-reimbursement-management) for the source requirement. Create/Edit/Submit/Delete/attach are **owner-only with no Admin override** — unlike Task Management, requirements.md never grants Admin the ability to edit an employee's claim on their behalf. Review actions (start-review/approve/reject/request-changes) require the `CanManageReimbursements` policy (`Admin` role only) and additionally block self-approval at the handler level: an Admin who is also the claimant on a reimbursement cannot approve/reject/request-changes on their own claim, checked against the caller's own linked `EmployeeId` regardless of role.

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/reimbursements` | Authenticated | List reimbursements — paginated, filterable by `employeeId`, `status`. Non-Admin callers are always scoped to their own reimbursements regardless of `employeeId` |
| `GET` | `/reimbursements/{id}` | Authenticated | Get a single reimbursement. Returns `404` (not `403`) for a non-Admin, non-owner caller |
| `POST` | `/reimbursements` | Authenticated | Create a Draft reimbursement for the caller. `employeeId` is derived from the caller's identity, never client-suppliable |
| `PUT` | `/reimbursements/{id}` | Authenticated (owner only) | Edit a reimbursement. Only while `Draft` or `ChangesRequested` |
| `DELETE` | `/reimbursements/{id}` | Authenticated (owner only) | Delete a reimbursement. Only while `Draft` |
| `POST` | `/reimbursements/{id}/submit` | Authenticated (owner only) | `Draft`/`ChangesRequested` → `Submitted` |
| `POST` | `/reimbursements/{id}/start-review` | `CanManageReimbursements` | `Submitted` → `UnderReview` |
| `POST` | `/reimbursements/{id}/approve` | `CanManageReimbursements` | `UnderReview` → `Approved`. `409`-equivalent error if the caller is the claimant |
| `POST` | `/reimbursements/{id}/reject` | `CanManageReimbursements` | `UnderReview` → `Rejected`. Body: `{ "remarks": "..." }` (required) |
| `POST` | `/reimbursements/{id}/request-changes` | `CanManageReimbursements` | `UnderReview` → `ChangesRequested`, sending it back to the employee for editing. Body: `{ "remarks": "..." }` (required) |
| `GET` | `/reimbursements/{id}/attachments` | Authenticated (owner or Admin) | List supporting documents |
| `POST` | `/reimbursements/{id}/attachments` | Authenticated (owner only) | Upload a supporting document. Multipart form upload (`file`); PDF/JPEG/PNG only, magic-byte verified, 10 MB max — same constraints as [Employee Document upload](#62-upload-employee-document) and [Task attachment upload](#21-task-management-apis). Rejected once the reimbursement is `Approved`, `Rejected`, or `Paid` |
| `GET` | `/reimbursements/attachments/{attachmentId}/download` | Authenticated (owner or Admin) | Download an attachment |

Create/update request body:

```json
{
  "expenseTitle": "Client dinner",
  "expenseCategory": "Meals",
  "expenseDate": "2026-07-24T00:00:00Z",
  "amount": 120.50,
  "currency": "USD",
  "description": "Dinner with Acme Retail's finance team.",
  "notes": null
}
```

`expenseDate` cannot be in the future. `amount` must be greater than `0`. `currency` is free text (not validated against ISO 4217), defaulting to `USD`. `expenseCategory` is free text — requirements.md doesn't enumerate a fixed category list.

Reimbursement response:

```json
{
  "id": "00000000-0000-0000-0000-000000001401",
  "reimbursementNumber": "REI-3F2A9B10",
  "employeeId": "00000000-0000-0000-0000-000000000101",
  "employeeName": "Jane Doe",
  "expenseTitle": "Client dinner",
  "expenseCategory": "Meals",
  "expenseDate": "2026-07-24T00:00:00Z",
  "amount": 120.50,
  "currency": "USD",
  "description": "Dinner with Acme Retail's finance team.",
  "notes": null,
  "status": "Submitted",
  "submittedAtUtc": "2026-07-26T09:00:00Z",
  "approvedAtUtc": null,
  "approvedBy": null,
  "reviewRemarks": null,
  "payrollProcessed": false,
  "payrollRunId": null,
  "payrollDate": null,
  "createdAtUtc": "2026-07-25T14:00:00Z",
  "updatedAtUtc": "2026-07-26T09:00:00Z"
}
```

Status values: `Draft`, `Submitted`, `UnderReview`, `Approved`, `Rejected`, `ChangesRequested`, `Paid` — matches the workflow diagram in requirements.md, including the `UnderReview` step between `Submitted` and the approve/reject/changes-requested branch.

### 22.1 Payroll Integration

No separate endpoint — this happens automatically inside `POST /payroll/process` ([§17.1](#171-payroll-runs)) and is previewable via `POST /payroll/dry-run`. For each employee, every `Approved` reimbursement with `payrollProcessed: false` is summed into that run's payslip as `totalReimbursements` (added to `netPay`, not `grossPay`), and then stamped `status: "Paid"`, `payrollProcessed: true`, `payrollRunId`, `payrollDate` in the same unit of work — so a later run's query for "approved and unprocessed" can never select it again, satisfying "Payroll can process approved reimbursement only once." `Draft`, `Submitted`, `UnderReview`, `Rejected`, and `ChangesRequested` reimbursements are never included.

Business rules enforced: an employee cannot approve/reject/request-changes on their own reimbursement, checked against the reviewer's own `employeeId` regardless of role; edits/deletes/attachment-uploads are owner-only with no Admin override; a reimbursement is read-only to edits/deletes/new-attachments once `Approved`, `Rejected`, or `Paid`; every status change is written to `AuditLogs` (entity `Reimbursement`).

## 23. Recruitment & Onboarding APIs

See [database-design.md §19](database-design.md#19-recruitment-tables) for the underlying schema and [requirements.md](requirements.md#recruitment--onboarding) for the source requirement — four bullets (Candidate Management, Interview Scheduling, Offer Generation, Joining Checklist) with no endpoint list, field list, or status workflow specified. Everything below was designed to fit this codebase's existing conventions (Client Master's CRUD+soft-delete shape, Task Management's self-scoping pattern, Payroll's PDF-generation pattern), not specified upstream.

All endpoints require the `CanManageRecruitment` policy (`Admin`, `HR` roles) except interview feedback submission, which is open to any authenticated caller but scoped at the handler level to the interview's assigned interviewer (`RequestingUserId`/`IsPrivileged`, same pattern as Task Management, §21), with an Admin/HR override.

### 23.1 Candidates

Base path: `/candidates`.

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/candidates` | List candidates — paginated, filterable by `status`, `designationId`, `search` (matches first name, last name, or email) |
| `GET` | `/candidates/{id}` | Get a single candidate |
| `POST` | `/candidates` | Register a new candidate application. `status` always starts at `Applied` |
| `PUT` | `/candidates/{id}` | Update candidate details |
| `DELETE` | `/candidates/{id}` | Soft-delete a candidate |
| `POST` | `/candidates/{id}/restore` | Restore a soft-deleted candidate |
| `POST` | `/candidates/{id}/reject` | The company's decision. Terminal. Optional `{ "reason": "..." }` body, appended to `notes` |
| `POST` | `/candidates/{id}/withdraw` | The candidate's own decision. Terminal. Same optional body shape as Reject |
| `GET` | `/candidates/{id}/attachments` | List uploaded attachments (resume, etc.) |
| `POST` | `/candidates/{id}/attachments` | Upload an attachment. Multipart form upload (`file`); PDF/JPEG/PNG only, magic-byte verified, 10 MB max — same constraints as [Employee Document upload](#62-upload-employee-document) |
| `GET` | `/candidates/attachments/{attachmentId}/download` | Download an attachment |

Create/update request body:

```json
{
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@example.com",
  "phoneNumber": "+1-555-0101",
  "designationId": "00000000-0000-0000-0000-000000000401",
  "departmentId": "00000000-0000-0000-0000-000000000301",
  "source": "LinkedIn",
  "appliedDate": "2026-07-20",
  "notes": null
}
```

`designationId` must reference an existing designation (the position applied for); `departmentId`, when supplied, must reference an existing department. `email` is not required to be unique — the same person can apply more than once over time.

Candidate response:

```json
{
  "id": "00000000-0000-0000-0000-000000001401",
  "candidateNumber": "CAN-3F2A9B10",
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@example.com",
  "phoneNumber": "+1-555-0101",
  "designationId": "00000000-0000-0000-0000-000000000401",
  "designationName": "Software Engineer",
  "departmentId": "00000000-0000-0000-0000-000000000301",
  "departmentName": "Engineering",
  "source": "LinkedIn",
  "appliedDate": "2026-07-20T00:00:00Z",
  "status": "Applied",
  "notes": null,
  "convertedEmployeeId": null,
  "isDeleted": false,
  "createdAtUtc": "2026-07-20T09:00:00Z",
  "updatedAtUtc": null
}
```

Status values: `Applied`, `Screening`, `Interviewing`, `Offered`, `Hired`, `Rejected`, `Withdrawn`. `Interviewing` is set automatically the moment the first interview is scheduled (§23.2); `Offered` when an offer is sent (§23.3); `Hired` only by Convert-to-Employee (§23.5), not by the candidate accepting an offer. `Rejected`/`Withdrawn`/`Hired` are terminal — every mutating endpoint in this module rejects a candidate already in one of those states.

### 23.2 Interviews

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/candidates/{id}/interviews` | `CanManageRecruitment` | List a candidate's interviews, chronological |
| `POST` | `/candidates/{id}/interviews` | `CanManageRecruitment` | Schedule an interview round |
| `POST` | `/interviews/{id}/reschedule` | `CanManageRecruitment` | Updates `scheduledAtUtc`/`durationMinutes` in place; only from `Scheduled` |
| `POST` | `/interviews/{id}/cancel` | `CanManageRecruitment` | `Scheduled` → `Cancelled` |
| `POST` | `/interviews/{id}/no-show` | `CanManageRecruitment` | `Scheduled` → `NoShow` |
| `POST` | `/interviews/{id}/feedback` | Authenticated (assigned interviewer or Admin/HR) | `Scheduled` → `Completed`. Records `feedback`, `rating` (1–5), and `outcome` |

Schedule request:

```json
{
  "interviewerEmployeeId": "00000000-0000-0000-0000-000000000101",
  "round": "Technical Round 1",
  "mode": "VideoCall",
  "scheduledAtUtc": "2026-07-28T10:00:00Z",
  "durationMinutes": 45
}
```

Feedback request:

```json
{
  "feedback": "Strong on system design, needs more depth on databases.",
  "rating": 4,
  "outcome": "Passed"
}
```

`mode`: `Onsite`, `Phone`, `VideoCall`. `status`: `Scheduled`, `Completed`, `Cancelled`, `NoShow`. `outcome`: `Pending`, `Passed`, `Failed`, `OnHold` — `feedback`/`rating`/`outcome` submission is rejected with `409` if the interview isn't `Scheduled`, and with `403` if the caller is neither the assigned interviewer nor Admin/HR.

### 23.3 Offers

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/candidates/{id}/offers` | List a candidate's offers, most recent first. A candidate can have more than one over time (e.g. a renegotiated offer after a rejection) |
| `POST` | `/candidates/{id}/offers` | Create a `Draft` offer |
| `POST` | `/offers/{id}/send` | `Draft` → `Sent`. Generates the offer letter PDF and moves the candidate to `Offered` |
| `POST` | `/offers/{id}/accept` | `Sent` → `Accepted`. Recorded by Admin/HR on the candidate's behalf (there's no candidate-facing portal). Seeds the default onboarding checklist (§23.4) |
| `POST` | `/offers/{id}/reject` | `Sent` → `Rejected`. Optional `{ "reason": "..." }` body |
| `POST` | `/offers/{id}/withdraw` | `Draft`/`Sent` → `Withdrawn` — the company pulling the offer back |
| `GET` | `/offers/{id}/download` | Download the offer letter PDF (available once `Sent`) |

Create request:

```json
{
  "designationId": "00000000-0000-0000-0000-000000000401",
  "departmentId": "00000000-0000-0000-0000-000000000301",
  "offeredSalary": 90000.00,
  "joiningDate": "2026-09-01",
  "expiresAtUtc": "2026-08-15T00:00:00Z",
  "notes": null
}
```

`expiresAtUtc` is optional and must be in the future when supplied; an offer without one never auto-expires.

Offer response:

```json
{
  "id": "00000000-0000-0000-0000-000000001501",
  "offerNumber": "OFR-3F2A9B10",
  "candidateId": "00000000-0000-0000-0000-000000001401",
  "designationId": "00000000-0000-0000-0000-000000000401",
  "designationName": "Software Engineer",
  "departmentId": "00000000-0000-0000-0000-000000000301",
  "departmentName": "Engineering",
  "offeredSalary": 90000.00,
  "joiningDate": "2026-09-01T00:00:00Z",
  "status": "Sent",
  "issuedAtUtc": "2026-07-29T10:00:00Z",
  "respondedAtUtc": null,
  "expiresAtUtc": "2026-08-15T00:00:00Z",
  "notes": null,
  "hasDocument": true,
  "createdAtUtc": "2026-07-28T09:00:00Z"
}
```

Status values: `Draft`, `Sent`, `Accepted`, `Rejected`, `Withdrawn`, `Expired`. `Expired` is set automatically: a daily background sweep (see [database-design.md §23](database-design.md#23-background-jobs)) flips any `Sent` offer whose `expiresAtUtc` has passed.

### 23.4 Onboarding Checklist

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/candidates/{id}/checklist` | List a candidate's onboarding checklist items |
| `POST` | `/candidates/{id}/checklist` | Add a custom item on top of the default set |
| `POST` | `/checklist/{itemId}/complete` | Mark an item complete. Optional `{ "reason": "..." }` body, stored as the item's `notes` |

A default 5-item set — "Offer Letter Signed", "ID Proof Submitted", "Bank Details Collected", "Laptop/Asset Allocated", "Induction Completed" (an invented default; requirements.md names the feature but not its items) — is auto-created the instant an offer is accepted (§23.3).

### 23.5 Convert to Employee

```text
POST /candidates/{id}/convert-to-employee
```

Creates the real `Employees` row (see [database-design.md §5.1](database-design.md#51-employees)) for a candidate whose most recent offer is `Accepted`. Request body:

```json
{
  "employeeCode": "EMP-1042",
  "officeLocationId": "00000000-0000-0000-0000-000000000201",
  "teamId": null,
  "managerId": "00000000-0000-0000-0000-000000000102",
  "joinDate": null
}
```

`employeeCode` and `officeLocationId` are required — `Employees.OfficeLocationId`/`EmployeeCode` have no equivalent anywhere on `Candidates`/`Offers`, so they're supplied here, the same way they're supplied to [Create Employee](#53-create-employee) directly. Everything else (`firstName`/`lastName`/`email`/`phoneNumber` from the candidate; `designationId`/`departmentId`/join date from the accepted offer, unless `joinDate` is explicitly overridden here) is copied automatically. Rejected with `409` if there's no `Accepted` offer, or if the candidate was already converted. On success, `candidates.status` becomes `Hired` and `convertedEmployeeId` is set — both are then permanent.

Business rules enforced: `Reject`/`Withdraw`/`Hired` are terminal for a candidate — every mutating endpoint in this module rejects a candidate already in one of those states; interview feedback is rejected with `403` for anyone but the assigned interviewer or Admin/HR; every status change (candidate, interview, offer) and the Convert-to-Employee action are written to `AuditLogs`.

## 24. Asset Management APIs

See [database-design.md §20](database-design.md#20-asset-management-tables) for the underlying schema and [requirements.md](requirements.md#asset-management) for the source requirement — three bullets (Laptop Allocation, Mobile Allocation, Asset Return Tracking) with no endpoint list, field list, or status workflow specified. Modeled as one `Assets` master table plus one `AssetAssignments` history table, the same "master + assignment history" shape as `Employees`+`AttendanceRecords`.

All endpoints require the `CanManageAssets` policy (`Admin`, `HR` roles) — there is no self-service angle here, unlike Task Management (§21) or Reimbursements (§22): an employee doesn't manage their own asset assignments.

### 24.1 Assets

Base path: `/assets`.

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/assets` | List assets — paginated, filterable by `status`, `category`, `search` (matches `assetTag`, `brand`, `model`, `serialNumber`) |
| `GET` | `/assets/{id}` | Get a single asset |
| `POST` | `/assets` | Register a new asset. `status` always starts at `Available` |
| `PUT` | `/assets/{id}` | Update asset details. Does not change `status` — use `/assets/{id}/status` or the assign/return actions |
| `DELETE` | `/assets/{id}` | Soft-delete an asset. Rejected with `409` while `status` is `Assigned` |
| `POST` | `/assets/{id}/restore` | Restore a soft-deleted asset |
| `POST` | `/assets/{id}/status` | Change status outside the assign/return flow — e.g. mark `UnderRepair`, `Retired`, `Lost`, or back to `Available`. Rejected with `409` if the target status is `Assigned` (use Assign instead), or if the asset is currently `Assigned` (must be returned first) |

Create/update request body:

```json
{
  "category": "Laptop",
  "brand": "Dell",
  "model": "Latitude 5440",
  "serialNumber": "SN-88213X",
  "purchaseDate": "2026-01-15",
  "purchaseCost": 1200.00,
  "notes": null
}
```

Status change request:

```json
{
  "status": "UnderRepair",
  "notes": "Screen flickering, sent to vendor for repair."
}
```

Asset response:

```json
{
  "id": "00000000-0000-0000-0000-000000001601",
  "assetTag": "AST-3F2A9B10",
  "category": "Laptop",
  "brand": "Dell",
  "model": "Latitude 5440",
  "serialNumber": "SN-88213X",
  "purchaseDate": "2026-01-15T00:00:00Z",
  "purchaseCost": 1200.00,
  "status": "Available",
  "notes": null,
  "isDeleted": false,
  "createdAtUtc": "2026-01-15T09:00:00Z",
  "updatedAtUtc": null
}
```

`category` is free text, not a fixed enum — requirements.md names Laptop/Mobile as examples, not an exhaustive list. Status values: `Available`, `Assigned`, `UnderRepair`, `Retired`, `Lost`.

### 24.2 Assignments (Allocation / Return Tracking)

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/assets/{id}/assignments` | List an asset's full assignment history, most recent first |
| `POST` | `/assets/{id}/assign` | Allocate an `Available` asset to an employee. Rejected with `409` unless the asset's `status` is `Available` |
| `POST` | `/asset-assignments/{id}/return` | Close out an outstanding assignment. Sets the asset's resulting status (defaults to `Available`; the caller can instead record `UnderRepair`/`Retired`/`Lost`) |
| `GET` | `/employees/{employeeId}/assets` | An employee's full asset assignment history (current and past) |

Assign request:

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000103",
  "expectedReturnDate": "2027-01-15",
  "conditionAtAssignment": "New, unboxed.",
  "notes": null
}
```

Return request:

```json
{
  "conditionAtReturn": "Good, minor scuffs on the lid.",
  "resultingAssetStatus": "Available",
  "notes": null
}
```

Assignment response:

```json
{
  "id": "00000000-0000-0000-0000-000000001701",
  "assetId": "00000000-0000-0000-0000-000000001601",
  "assetTag": "AST-3F2A9B10",
  "employeeId": "00000000-0000-0000-0000-000000000103",
  "employeeName": "Jane Doe",
  "assignedByUserId": "00000000-0000-0000-0000-000000000001",
  "assignedDate": "2026-07-28T09:00:00Z",
  "expectedReturnDate": "2027-01-15T00:00:00Z",
  "conditionAtAssignment": "New, unboxed.",
  "returnedDate": null,
  "conditionAtReturn": null,
  "notes": null,
  "createdAtUtc": "2026-07-28T09:00:00Z",
  "updatedAtUtc": null
}
```

`returnedDate: null` means the asset is currently out with that employee. `assignedByUserId` is resolved server-side from the caller's JWT, not supplied by the client.

Business rules enforced: an asset can only be assigned to one employee at a time (`Assign` rejected unless `status = Available`); `status` can never be set to `Assigned` directly through `/assets/{id}/status` — only the Assign action does that; an asset can't be deleted, updated in a way that bypasses these rules, or have its status changed while `Assigned` — it must be returned first; every create/update/delete/restore/status-change/assign/return action is written to `AuditLogs`.

## 25. Performance Management APIs

See [database-design.md §21](database-design.md#21-performance-management-tables) for the underlying schema and [requirements.md](requirements.md#performance-management) for the source requirement — four bullets (Goals, KPI Tracking, Performance Reviews, Promotions) with no endpoint list, field list, status workflow, or authorization model specified. This is the first module built with a Manager tier: it extends the manager-scoping pattern already used by Attendance (`ManagerId`/direct-report checks, §12) instead of the Admin/HR-only pattern used by Recruitment/Assets, since Goals/Reviews/Promotions naturally involve a line manager, not just HR.

Every endpoint requires authentication. `GET` endpoints and self-service actions (progress updates, self-assessment) are open to any authenticated caller and scoped inside the handler — an employee sees/acts on their own records, a Manager additionally sees/acts on their own direct reports', Admin/HR see and act on everything. Creating or fully editing a record requires the `CanManagePerformance` policy (`Admin`, `HR`, `Manager`); a Manager who isn't privileged is further restricted to their own direct reports at the handler level. Promotion approve/reject/delete/restore require the stricter `CanApprovePromotions` policy (`Admin`, `HR` only — a Manager cannot approve their own proposal).

### 25.1 Goals

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/goals` | Authenticated (self-scoped) | List goals — paginated, filterable by `employeeId`/`status`/`category`. Employees see their own; Managers see their own and their reports'; Admin/HR see all |
| `GET` | `/goals/{id}` | Authenticated (self-scoped) | Get a single goal |
| `POST` | `/goals` | `CanManagePerformance` | Set a goal for an employee. A Manager may only set goals for their own direct reports |
| `PUT` | `/goals/{id}` | `CanManagePerformance` | Update a goal's `title`/`description`/`category`/`targetDate`/`weight`/`status`. Does not change `employeeId` |
| `POST` | `/goals/{id}/progress` | Authenticated (self-scoped) | Update `progressPercent` (0–100). The goal's own employee, their manager, or Admin/HR |
| `DELETE` | `/goals/{id}` | `CanManagePerformance` | Soft-delete a goal |
| `POST` | `/goals/{id}/restore` | `CanManagePerformance` | Restore a soft-deleted goal |
| `POST` | `/goals/{id}/kpis` | `CanManagePerformance` | Add a KPI to a goal ("KPI Tracking") |
| `POST` | `/kpis/{kpiId}/progress` | Authenticated (self-scoped) | Update a KPI's `currentValue`. The goal's own employee, their manager, or Admin/HR |

Create/update request body:

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000103",
  "title": "Ship the Q3 roadmap",
  "description": "Deliver the three committed features on schedule.",
  "category": "Engineering",
  "startDate": "2026-01-01",
  "targetDate": "2026-03-31",
  "weight": 30,
  "status": "InProgress"
}
```

`employeeId` is only accepted on create — a goal's owner can't be changed afterward. `status` is one of `NotStarted`, `InProgress`, `Completed`, `Cancelled`, set explicitly here (it is not auto-derived from `progressPercent`).

Goal response:

```json
{
  "id": "00000000-0000-0000-0000-000000001801",
  "goalNumber": "GOL-3F2A9B10",
  "employeeId": "00000000-0000-0000-0000-000000000103",
  "employeeName": "Jane Doe",
  "title": "Ship the Q3 roadmap",
  "description": "Deliver the three committed features on schedule.",
  "category": "Engineering",
  "startDate": "2026-01-01T00:00:00Z",
  "targetDate": "2026-03-31T00:00:00Z",
  "weight": 30,
  "status": "InProgress",
  "progressPercent": 40,
  "kpis": [
    {
      "id": "00000000-0000-0000-0000-000000001901",
      "goalId": "00000000-0000-0000-0000-000000001801",
      "name": "Features shipped",
      "targetValue": 3,
      "currentValue": 1,
      "unit": "features",
      "notes": null,
      "createdAtUtc": "2026-01-05T09:00:00Z",
      "updatedAtUtc": "2026-02-01T09:00:00Z"
    }
  ],
  "isDeleted": false,
  "createdAtUtc": "2026-01-05T09:00:00Z",
  "updatedAtUtc": "2026-02-01T09:00:00Z"
}
```

Add-KPI request: `{ "name": "Features shipped", "targetValue": 3, "unit": "features", "notes": null }`. Update-KPI-progress request: `{ "currentValue": 2, "notes": null }`.

### 25.2 Performance Reviews

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/reviews` | Authenticated (self-scoped) | List reviews — paginated, filterable by `employeeId`/`reviewerEmployeeId`/`status`. An employee sees reviews where they're the subject or reviewer; a Manager additionally sees their reports'; Admin/HR see all |
| `GET` | `/reviews/{id}` | Authenticated (self-scoped) | Get a single review |
| `POST` | `/reviews` | `CanManagePerformance` | Start a review cycle for an employee (`Draft`). A Manager may only start reviews for their own direct reports, and must set themselves as `reviewerEmployeeId` — only Admin/HR can assign an arbitrary reviewer |
| `POST` | `/reviews/{id}/self-assessment` | Authenticated (self-scoped) | `Draft` → `SelfAssessmentSubmitted`. Only the reviewed employee (or Admin/HR) |
| `POST` | `/reviews/{id}/manager-review` | Authenticated (self-scoped) | `Draft`/`SelfAssessmentSubmitted` → `Completed`. Records `managerAssessment`/`overallRating`. Only the assigned reviewer (or Admin/HR) |
| `POST` | `/reviews/{id}/cancel` | `CanManagePerformance` | Cancel a review that hasn't completed yet. Only the assigned reviewer (or Admin/HR) |
| `DELETE` | `/reviews/{id}` | `CanManagePerformance` | Soft-delete a review. Only the assigned reviewer (or Admin/HR) |
| `POST` | `/reviews/{id}/restore` | `CanManagePerformance` | Restore a soft-deleted review |

Create request:

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000103",
  "reviewerEmployeeId": "00000000-0000-0000-0000-000000000102",
  "reviewPeriodStart": "2026-01-01",
  "reviewPeriodEnd": "2026-06-30",
  "notes": null
}
```

Self-assessment request: `{ "selfAssessment": "I delivered the roadmap on time and mentored two juniors." }`. Manager-review request: `{ "managerAssessment": "Strong half, exceeded targets.", "overallRating": 4.5 }`.

Review response:

```json
{
  "id": "00000000-0000-0000-0000-000000002001",
  "reviewNumber": "REV-3F2A9B10",
  "employeeId": "00000000-0000-0000-0000-000000000103",
  "employeeName": "Jane Doe",
  "reviewerEmployeeId": "00000000-0000-0000-0000-000000000102",
  "reviewerName": "John Manager",
  "reviewPeriodStart": "2026-01-01T00:00:00Z",
  "reviewPeriodEnd": "2026-06-30T00:00:00Z",
  "status": "Completed",
  "selfAssessment": "I delivered the roadmap on time and mentored two juniors.",
  "managerAssessment": "Strong half, exceeded targets.",
  "overallRating": 4.5,
  "selfSubmittedAtUtc": "2026-06-25T09:00:00Z",
  "completedAtUtc": "2026-06-28T09:00:00Z",
  "notes": null,
  "isDeleted": false,
  "createdAtUtc": "2026-06-01T09:00:00Z",
  "updatedAtUtc": "2026-06-28T09:00:00Z"
}
```

Status values: `Draft`, `SelfAssessmentSubmitted`, `Completed`, `Cancelled`. There's no separate "manager review submitted" status between the manager's submission and `Completed` — submitting the manager review completes the cycle in one step. No `PUT` — a review's fields besides the workflow ones are fixed at creation, matching `Interviews`/`Offers`.

### 25.3 Promotions

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/promotions` | Authenticated (self-scoped) | List promotions — paginated, filterable by `employeeId`/`status`. Employees see their own; Managers see their own and their reports'; Admin/HR see all |
| `GET` | `/promotions/{id}` | Authenticated (self-scoped) | Get a single promotion |
| `POST` | `/promotions` | `CanManagePerformance` | Propose a promotion. A Manager may only propose for their own direct reports |
| `POST` | `/promotions/{id}/approve` | `CanApprovePromotions` | `Proposed` → `Approved`. Applies `toDesignationId`/`toDepartmentId` to the employee's record immediately if `effectiveDate` has already arrived; otherwise deferred until the daily sweep applies it (see [database-design.md §23](database-design.md#23-background-jobs)) |
| `POST` | `/promotions/{id}/reject` | `CanApprovePromotions` | `Proposed` → `Rejected` |
| `POST` | `/promotions/{id}/withdraw` | `CanManagePerformance` | `Proposed` → `Withdrawn` — the proposer pulling it back. Only the original proposer (or Admin/HR) |
| `DELETE` | `/promotions/{id}` | `CanApprovePromotions` | Soft-delete a promotion record |
| `POST` | `/promotions/{id}/restore` | `CanApprovePromotions` | Restore a soft-deleted promotion record |

Propose request:

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000103",
  "toDesignationId": "00000000-0000-0000-0000-000000000402",
  "toDepartmentId": null,
  "effectiveDate": "2026-04-01",
  "reason": "Consistently exceeding expectations across two review cycles."
}
```

`fromDesignationId`/`fromDepartmentId` are not accepted from the client — they're captured automatically from the employee's current designation/department at proposal time. Rejected with `409` if `toDesignationId`/`toDepartmentId` are unchanged from the employee's current ones.

Approve/reject request (optional): `{ "decisionNotes": "Approved effective next cycle." }`.

Promotion response:

```json
{
  "id": "00000000-0000-0000-0000-000000002101",
  "promotionNumber": "PRO-3F2A9B10",
  "employeeId": "00000000-0000-0000-0000-000000000103",
  "employeeName": "Jane Doe",
  "fromDesignationId": "00000000-0000-0000-0000-000000000401",
  "fromDesignationName": "Software Engineer",
  "toDesignationId": "00000000-0000-0000-0000-000000000402",
  "toDesignationName": "Senior Software Engineer",
  "fromDepartmentId": "00000000-0000-0000-0000-000000000301",
  "fromDepartmentName": "Engineering",
  "toDepartmentId": null,
  "toDepartmentName": null,
  "effectiveDate": "2026-04-01T00:00:00Z",
  "reason": "Consistently exceeding expectations across two review cycles.",
  "status": "Approved",
  "proposedByUserId": "00000000-0000-0000-0000-000000000002",
  "decidedByUserId": "00000000-0000-0000-0000-000000000001",
  "decidedAtUtc": "2026-03-20T09:00:00Z",
  "decisionNotes": "Approved effective next cycle.",
  "appliedAtUtc": "2026-04-01T00:03:00Z",
  "isDeleted": false,
  "createdAtUtc": "2026-03-15T09:00:00Z",
  "updatedAtUtc": "2026-04-01T00:03:00Z"
}
```

Status values: `Proposed`, `Approved`, `Rejected`, `Withdrawn` — all except `Proposed` are terminal. No `PUT` — a proposal's terms are fixed at creation, matching `Offers`. `appliedAtUtc` is separate from `status`: it's `null` on a freshly `Approved` promotion whose `effectiveDate` is still in the future, and gets stamped — by the Approve action itself or, if it had to wait, by the next daily sweep — the moment `toDesignationId`/`toDepartmentId` is actually written to the employee's record.

Business rules enforced: a Manager may only Create/Propose for their own direct reports and must set themselves as reviewer when creating a Review; only the assigned reviewer may submit a manager review or cancel/delete/restore a Review; only the original proposer (or Admin/HR) may Withdraw a Promotion; only Admin/HR may Approve/Reject a Promotion, even though a Manager can Propose one; every create/update/status-change/delete/restore action across Goals, Reviews, and Promotions is written to `AuditLogs`.

## 26. Messaging APIs

See [database-design.md §22](database-design.md#22-messaging-tables) for the underlying schema and [requirements.md](requirements.md#internal-messaging) for the source requirement — two bullets (Employee Messaging, Manager Messaging) with no endpoint list, field list, or access model specified. Unlike Performance (§25), this is **open messaging**: "Employee Messaging"/"Manager Messaging" are read as the two participant roles in a conversation, not a restriction — any authenticated caller can start a conversation with any other user. Every action other than Delete/Restore is open to any authenticated caller (`[Authorize]`, no policy) and scoped inside the handler to the caller being an **active participant** (a `MessageParticipants` row with `leftAtUtc` null) of the conversation. Delete/Restore require the `CanManageMessaging` policy (`Admin`, `HR` only) — an Admin/HR moderation action, not a participant-facing "delete for me".

### 26.1 Conversations

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/conversations` | Authenticated (self-scoped) | List the caller's conversations — paginated, filterable by `search` (matches conversation title or another participant's name), ordered by most recent activity |
| `GET` | `/conversations/unread-count` | Authenticated | Number of the caller's conversations with at least one unread message, for a nav badge |
| `GET` | `/conversations/{id}` | Authenticated (self-scoped) | Get a single conversation with its active participants. Only an active participant (or Admin/HR) |
| `POST` | `/conversations` | Authenticated | Start a conversation with one or more other users and send the first message |
| `POST` | `/conversations/{id}/participants` | Authenticated (self-scoped) | Add one or more users to a conversation. Only an active participant |
| `POST` | `/conversations/{id}/leave` | Authenticated (self-scoped) | Leave a group conversation. Not available on a direct (1:1) conversation — `409` |
| `DELETE` | `/conversations/{id}` | `CanManageMessaging` | Soft-delete a conversation |
| `POST` | `/conversations/{id}/restore` | `CanManageMessaging` | Restore a soft-deleted conversation |

Create request:

```json
{
  "participantUserIds": ["00000000-0000-0000-0000-000000000102"],
  "title": null,
  "initialMessageBody": "Hi — do you have a minute to review the Q3 roadmap doc?"
}
```

`participantUserIds` excludes the caller — it's added automatically. If it resolves to exactly one other user and `title` is omitted, this reuses an existing active 1:1 conversation between the caller and that user instead of creating a duplicate DM thread — the response `id` is the existing conversation's id and `initialMessageBody` is appended as a new message. Two or more `participantUserIds` (or a non-null `title`) always creates a new conversation with `isGroup: true` once there are 3+ participants.

Add-participants request: `{ "userIds": ["00000000-0000-0000-0000-000000000103"] }`. Adding anyone to a 1:1 conversation promotes it to `isGroup: true`.

Conversation response:

```json
{
  "id": "00000000-0000-0000-0000-000000002201",
  "title": null,
  "isGroup": false,
  "participants": [
    { "userId": "00000000-0000-0000-0000-000000000101", "name": "Jane Doe", "joinedAtUtc": "2026-07-20T09:00:00Z", "leftAtUtc": null },
    { "userId": "00000000-0000-0000-0000-000000000102", "name": "John Manager", "joinedAtUtc": "2026-07-20T09:00:00Z", "leftAtUtc": null }
  ],
  "lastMessageAtUtc": "2026-07-20T09:00:00Z",
  "lastMessagePreview": "Hi — do you have a minute to review the Q3 roadmap doc?",
  "unreadCount": 1,
  "isDeleted": false,
  "createdAtUtc": "2026-07-20T09:00:00Z",
  "updatedAtUtc": "2026-07-20T09:00:00Z"
}
```

`unreadCount` is computed per caller from their own `MessageParticipants.lastReadAtUtc` watermark, not a global count — it's always `0` from the sender's own point of view immediately after sending.

### 26.2 Messages

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/conversations/{id}/messages` | Authenticated (self-scoped) | List messages in a conversation — paginated, newest first. Only an active participant (or Admin/HR) |
| `POST` | `/conversations/{id}/messages` | Authenticated (self-scoped) | Send a message. Only an active participant |
| `POST` | `/conversations/{id}/read` | Authenticated (self-scoped) | Advance the caller's read watermark for this conversation to now |

Send request: `{ "body": "Sounds good, I'll take a look this afternoon." }`.

Message response:

```json
{
  "id": "00000000-0000-0000-0000-000000002301",
  "conversationId": "00000000-0000-0000-0000-000000002201",
  "senderUserId": "00000000-0000-0000-0000-000000000102",
  "senderName": "John Manager",
  "body": "Sounds good, I'll take a look this afternoon.",
  "sentAtUtc": "2026-07-20T09:05:00Z"
}
```

Business rules enforced: only an active participant may send a message, view a conversation or its messages, add participants, or mark it read — a caller who isn't a participant gets `403`, and a nonexistent conversation gets `404`; sending a message advances the sender's own read watermark so their own message never shows as unread to them; a participant newly added to an existing conversation has their watermark set to the join time (not unread from the beginning), so a long conversation's prior history doesn't flood in as unread; leaving is rejected with `409` on a direct (1:1) conversation; every conversation create, participants-added, participant-left, message-sent, delete, and restore action is written to `AuditLogs`.

