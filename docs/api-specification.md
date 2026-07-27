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
| `429 Too Many Requests` | Rate limit exceeded (see [3.1](#31-login) and [3.10](#310-register)); response includes a `Retry-After` header |
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

Processing a payroll run and approving it are both written to `AuditLogs` (entity `PayrollRun`, actions `Processed`/`Approved`) — money movement is treated as a sensitive operation on par with Leave approvals and employee lifecycle changes.

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

