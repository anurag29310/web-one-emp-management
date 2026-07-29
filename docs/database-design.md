# Employee Management System Database Design

## 1. Overview

The Employee Management System will use PostgreSQL for local and hosted database workloads, run via Docker (`postgres:15`) in every environment per `docker-compose.yml` / `docker-compose.prod.yml`. Entity Framework Core migrations (Npgsql provider) will manage schema changes, and the data model will follow the Clean Architecture boundaries defined in `docs/architecture.md`.

Design decisions:

- Use relational tables for core HR, attendance, leave, department, and identity data.
- Store files in Azure Blob Storage and keep only file metadata in PostgreSQL.
- Use audit fields and soft delete on business tables as required by `AI_CONTRACT.md`.
- Use proper foreign keys and indexes to support 10,000+ employees and response times under 2 seconds.
- Keep authentication tables separate from employee profile tables so user accounts can be managed independently from HR records.

## 2. Naming And Data Type Conventions

Recommended conventions:

- Primary keys: `Id` as `uuid`.
- Foreign keys: `{EntityName}Id` as `uuid`.
- Dates and timestamps: UTC, using `timestamptz`.
- Status fields: `varchar(50)` or small enum-backed integers. Use one approach consistently in implementation.
- Text fields: use bounded `varchar` lengths instead of `text` unless the field is intentionally long.
- Money values in future payroll modules: `decimal(18,2)`.
- Row version: PostgreSQL has no automatic rowversion column type. Use a `uint` property marked `.IsRowVersion()` in EF Core, which the Npgsql provider maps to the system `xmin` column instead of a stored column.

## 3. Shared Audit Fields

All business tables should include the following columns unless noted otherwise:

| Column | Type | Required | Purpose |
| --- | --- | --- | --- |
| `Id` | `uuid` | Yes | Primary key |
| `CreatedAtUtc` | `timestamptz` | Yes | Record creation timestamp |
| `CreatedBy` | `uuid` | No | User who created the record |
| `UpdatedAtUtc` | `timestamptz` | No | Last update timestamp |
| `UpdatedBy` | `uuid` | No | User who last updated the record |
| `DeletedAtUtc` | `timestamptz` | No | Soft delete timestamp |
| `DeletedBy` | `uuid` | No | User who soft deleted the record |
| `IsDeleted` | `boolean` | Yes | Soft delete flag |

> **Optimistic concurrency:** PostgreSQL has no automatic rowversion type. A `uint RowVersion` property configured with `.IsRowVersion()` is auto-mapped by the Npgsql EF Core provider to the database's native `xmin` system column — no `RowVersion` column is created in the table; reads/writes go through the existing `xmin` column PostgreSQL already maintains on every row.

Identity and security tables may use a smaller audit set where appropriate, but refresh token activity must still be traceable.

## 4. Core Tables

> **Implementation note (Users/Roles):** the tables below describe the target design —
> a many-to-many `Users`↔`Roles` relationship via `UserRoles`, with the full audit set
> (`CreatedBy`, `UpdatedBy`, `DeletedBy`, `DeletedAtUtc`, `RowVersion`) on every entity.
> The Users/Roles admin API currently implemented (see
> [api-specification.md §4](api-specification.md#4-user-and-role-administration-apis))
> instead uses the pre-existing single-role-per-user model (`User.RoleId`, a nullable FK,
> no `UserRoles` join table), with a reduced audit set (`IsDeleted`, `CreatedAtUtc`,
> `UpdatedAtUtc` only — no `CreatedBy`/`UpdatedBy`/`DeletedBy`/`RowVersion`) and no
> `LastLoginAtUtc` column. This was a deliberate scope decision to ship a
> minimal admin API without rewriting the login/JWT/current-user code paths, which assume a
> single role today. Migrating to the full design below — the many-to-many `UserRoles` table,
> full audit columns, last-login tracking — remains open follow-up work and would require
> updating `AuthRepository`, `JwtTokenService`, and `GetCurrentUserQueryHandler` alongside the
> schema change.
>
> **Implementation note (MFA):** `IsMfaEnabled` below is implemented as-is. Two columns beyond
> what's listed also exist on the real `Users` table: `MfaSecretProtected` (`text`,
> nullable) — the TOTP secret, encrypted at rest via ASP.NET Core Data Protection, never stored
> or transmitted in plaintext after enrollment — and `MfaEnabledAtUtc` (`timestamptz`, nullable).
> They're omitted from the table below because they're an implementation detail of how
> `IsMfaEnabled` is realized, not part of the target ERD shape.

### 4.1 Users

Stores application login accounts.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `EmployeeId` | `uuid` | Nullable FK to `Employees` |
| `UserName` | `varchar(100)` | Unique |
| `Email` | `varchar(256)` | Unique |
| `PasswordHash` | `text` | Required |
| `IsActive` | `boolean` | Required |
| `IsMfaEnabled` | `boolean` | Required |
| `LastLoginAtUtc` | `timestamptz` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields |

### 4.2 Roles

Stores role definitions.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `Name` | `varchar(50)` | Unique: `Admin`, `HR`, `Manager`, `Employee` |
| `Description` | `varchar(250)` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields |

### 4.3 UserRoles

Maps users to roles.

| Column | Type | Notes |
| --- | --- | --- |
| `UserId` | `uuid` | FK to `Users` |
| `RoleId` | `uuid` | FK to `Roles` |
| `AssignedAtUtc` | `timestamptz` | Required |
| `AssignedBy` | `uuid` | Nullable FK to `Users` |

Primary key: composite key on `UserId`, `RoleId`.

### 4.4 RefreshTokens

Stores hashed refresh tokens for JWT session renewal.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `UserId` | `uuid` | FK to `Users` |
| `TokenHash` | `varchar(512)` | Required |
| `TokenFamilyId` | `uuid` | Groups rotated tokens |
| `IssuedAtUtc` | `timestamptz` | Required |
| `ExpiresAtUtc` | `timestamptz` | Required |
| `RevokedAtUtc` | `timestamptz` | Nullable |
| `ReplacedByTokenId` | `uuid` | Nullable FK to `RefreshTokens` |
| `IpAddress` | `varchar(64)` | Nullable |
| `UserAgent` | `varchar(500)` | Nullable |
| `IsRevoked` | `boolean` | Required |

Refresh tokens should be hard deleted only after expiry and retention policy allow it.

### 4.5 PasswordResetTokens

Stores password reset requests.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `UserId` | `uuid` | FK to `Users` |
| `TokenHash` | `varchar(512)` | Required |
| `ExpiresAtUtc` | `timestamptz` | Required |
| `UsedAtUtc` | `timestamptz` | Nullable |
| `CreatedAtUtc` | `timestamptz` | Required |
| `IpAddress` | `varchar(64)` | Nullable |

### 4.6 MfaChallenges

Short-lived server-side record backing the `mfaChallengeId` a client receives from `POST
/auth/login` when `requiresMfa: true`. Exists so the pending-second-factor state survives past a
single request/process (unlike an in-memory cache) and works correctly behind a load balancer.
Rows are cheap to accumulate since each is single-use and short-lived; a periodic cleanup of
expired rows is a reasonable operational follow-up but not required for correctness.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key; this is the `mfaChallengeId` |
| `UserId` | `uuid` | FK to `Users` |
| `CreatedAtUtc` | `timestamptz` | Required |
| `ExpiresAtUtc` | `timestamptz` | Required — 5 minutes from creation |
| `IsConsumed` | `boolean` | Required — set once a code has been successfully verified against this challenge |

### 4.7 MfaRecoveryCodes

One-time backup codes issued when a user enables MFA (10 per enrollment), so losing the
authenticator device doesn't permanently lock the account out. Each code is shown to the user
exactly once at generation time and stored only as a hash — same treatment as `PasswordHash` on
`Users`, not reversible. Regenerating (`POST /auth/mfa/recovery-codes/regenerate`) invalidates
every prior code for the user, used or not, and issues 10 fresh ones.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `UserId` | `uuid` | FK to `Users` |
| `CodeHash` | `text` | Required — same PBKDF2 hashing as `Users.PasswordHash` |
| `CreatedAtUtc` | `timestamptz` | Required |
| `UsedAtUtc` | `timestamptz` | Nullable — set on first (and only) successful use |

## 5. Employee And Organization Tables

### 5.1 Employees

Stores employee profile and employment information.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `EmployeeCode` | `varchar(50)` | Unique |
| `FirstName` | `varchar(100)` | Required |
| `MiddleName` | `varchar(100)` | Nullable |
| `LastName` | `varchar(100)` | Required |
| `Email` | `varchar(256)` | Unique for active employees |
| `PhoneNumber` | `varchar(30)` | Nullable |
| `DateOfBirth` | `date` | Nullable |
| `Gender` | `varchar(50)` | Nullable |
| `AddressLine1` | `varchar(250)` | Nullable |
| `AddressLine2` | `varchar(250)` | Nullable |
| `City` | `varchar(100)` | Nullable |
| `State` | `varchar(100)` | Nullable |
| `PostalCode` | `varchar(20)` | Nullable |
| `Country` | `varchar(100)` | Nullable |
| `EmergencyContactName` | `varchar(150)` | Nullable |
| `EmergencyContactPhone` | `varchar(30)` | Nullable |
| `EmergencyContactRelation` | `varchar(100)` | Nullable |
| `DepartmentId` | `uuid` | FK to `Departments` |
| `TeamId` | `uuid` | Nullable FK to `Teams` |
| `DesignationId` | `uuid` | FK to `Designations` |
| `ManagerId` | `uuid` | Nullable self FK to `Employees` |
| `OfficeLocationId` | `uuid` | FK to `OfficeLocations` |
| `JoinDate` | `date` | Required |
| `ExitDate` | `date` | Nullable |
| `Status` | `varchar(50)` | Active, Inactive, OnLeave, Terminated |
| `ProfilePhotoDocumentId` | `uuid` | Nullable FK to `EmployeeDocuments` |
| Audit fields | Shared | Include audit and soft delete fields |

### 5.2 Departments

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `Name` | `varchar(150)` | Unique |
| `Code` | `varchar(50)` | Unique |
| `Description` | `varchar(500)` | Nullable |
| `HeadEmployeeId` | `uuid` | Nullable FK to `Employees` |
| Audit fields | Shared | Include audit and soft delete fields |

### 5.3 Teams

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `DepartmentId` | `uuid` | FK to `Departments` |
| `Name` | `varchar(150)` | Required |
| `Code` | `varchar(50)` | Required |
| `LeadEmployeeId` | `uuid` | Nullable FK to `Employees` |
| Audit fields | Shared | Include audit and soft delete fields |

Unique constraint: `DepartmentId`, `Code`.

### 5.4 Designations

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `Name` | `varchar(150)` | Unique |
| `Code` | `varchar(50)` | Unique |
| `Level` | `int` | Optional hierarchy level |
| Audit fields | Shared | Include audit and soft delete fields |

### 5.5 OfficeLocations

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `Name` | `varchar(150)` | Required |
| `Code` | `varchar(50)` | Unique |
| `AddressLine1` | `varchar(250)` | Nullable |
| `AddressLine2` | `varchar(250)` | Nullable |
| `City` | `varchar(100)` | Required |
| `State` | `varchar(100)` | Nullable |
| `Country` | `varchar(100)` | Required |
| `TimeZoneId` | `varchar(100)` | Required |
| Audit fields | Shared | Include audit and soft delete fields |

### 5.6 EmployeeDocuments

Stores metadata for files stored in Azure Blob Storage.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `EmployeeId` | `uuid` | FK to `Employees` |
| `DocumentType` | `varchar(100)` | ProfilePhoto, Identity, OfferLetter, NDA, Appraisal, Other |
| `OriginalFileName` | `varchar(255)` | Required |
| `ContentType` | `varchar(100)` | Required |
| `FileSizeBytes` | `bigint` | Required |
| `BlobContainer` | `varchar(100)` | Required |
| `BlobPath` | `varchar(500)` | Required |
| `UploadedAtUtc` | `timestamptz` | Required |
| `UploadedBy` | `uuid` | FK to `Users` |
| Audit fields | Shared | Include audit and soft delete fields |

## 6. Attendance Tables

### 6.1 Shifts

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `Name` | `varchar(150)` | Required |
| `StartTime` | `time` | Required |
| `EndTime` | `time` | Required |
| `GraceMinutes` | `int` | Required |
| `IsNightShift` | `boolean` | Required |
| Audit fields | Shared | Include audit and soft delete fields |

### 6.2 EmployeeShifts

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `EmployeeId` | `uuid` | FK to `Employees` |
| `ShiftId` | `uuid` | FK to `Shifts` |
| `EffectiveFrom` | `date` | Required |
| `EffectiveTo` | `date` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields |

### 6.3 AttendanceRecords

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `EmployeeId` | `uuid` | FK to `Employees` |
| `ShiftId` | `uuid` | Nullable FK to `Shifts` |
| `AttendanceDate` | `date` | Required |
| `CheckInAtUtc` | `timestamptz` | Nullable |
| `CheckOutAtUtc` | `timestamptz` | Nullable |
| `Status` | `varchar(50)` | Present, Absent, Late, HalfDay, OnLeave, Holiday |
| `IsLateArrival` | `boolean` | Required |
| `IsEarlyLeave` | `boolean` | Required |
| `TotalWorkMinutes` | `int` | Nullable |
| `Notes` | `varchar(500)` | Nullable |
| `CheckInLatitude` | `decimal(9,6)` | Nullable. GPS captured by the client at check-in |
| `CheckInLongitude` | `decimal(9,6)` | Nullable |
| `CheckInAddress` | `varchar(500)` | Nullable. Reverse-geocoded from `CheckInLatitude`/`CheckInLongitude` (see implementation note below); best-effort — left `null` if the geocoding provider is unavailable |
| `CheckInDeviceInfo` | `varchar(255)` | Nullable. Server-derived from the `User-Agent` request header, never client-supplied |
| `CheckInIpAddress` | `varchar(64)` | Nullable. Server-derived from the request's remote IP, never client-supplied |
| `CheckOutLatitude` | `decimal(9,6)` | Nullable. GPS captured by the client at check-out. Punch Out may legitimately be off-premises (client visit, field work) — always recorded regardless of location |
| `CheckOutLongitude` | `decimal(9,6)` | Nullable |
| `CheckOutAddress` | `varchar(500)` | Nullable. Reverse-geocoded, same best-effort semantics as `CheckInAddress` |
| `CheckOutDeviceInfo` | `varchar(255)` | Nullable, server-derived |
| `CheckOutIpAddress` | `varchar(64)` | Nullable, server-derived |
| Audit fields | Shared | Include audit and soft delete fields |

Unique constraint: `EmployeeId`, `AttendanceDate`.

> **Implementation note (GPS & Location Tracking):** see [requirements.md](requirements.md#gps--location-tracking-planned-enhancement) for the source requirement. Check-in and check-out locations are stored as two independent column groups rather than one shared set, since an employee's punch-out location (e.g. a client site) is frequently different from their punch-in location (the office) and both must remain independently visible to Admin. Reverse geocoding uses Nominatim/OpenStreetMap (`IGeocodingService` / `NominatimGeocodingService`, configured under `Geocoding:BaseUrl`/`Geocoding:UserAgent`) — free, no API key, but a geocoding failure or timeout never blocks a punch; `Latitude`/`Longitude` are always saved and `Address` is simply left `null`. Office geofencing (rejecting a Punch In outside a configurable radius) is explicitly out of scope here — requirements.md lists it as a future "Nice To Have", not part of this pass.

### 6.4 AttendanceCorrections

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `AttendanceRecordId` | `uuid` | FK to `AttendanceRecords` |
| `RequestedByEmployeeId` | `uuid` | FK to `Employees` |
| `ApprovedByEmployeeId` | `uuid` | Nullable FK to `Employees` |
| `RequestedCheckInAtUtc` | `timestamptz` | Nullable |
| `RequestedCheckOutAtUtc` | `timestamptz` | Nullable |
| `Reason` | `varchar(500)` | Required |
| `Status` | `varchar(50)` | Pending, Approved, Rejected |
| `DecisionAtUtc` | `timestamptz` | Nullable |
| `DecisionComments` | `varchar(500)` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields |

## 7. Leave Tables

### 7.1 LeaveTypes

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `Name` | `varchar(100)` | Casual Leave, Sick Leave, Earned Leave, Unpaid Leave, Work From Home |
| `Code` | `varchar(50)` | Unique |
| `IsPaid` | `boolean` | Required |
| `RequiresApproval` | `boolean` | Required |
| `AnnualEntitlementDays` | `decimal(5,2)` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields |

### 7.2 LeaveBalances

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `EmployeeId` | `uuid` | FK to `Employees` |
| `LeaveTypeId` | `uuid` | FK to `LeaveTypes` |
| `Year` | `int` | Required |
| `OpeningBalance` | `decimal(5,2)` | Required |
| `Accrued` | `decimal(5,2)` | Required |
| `Used` | `decimal(5,2)` | Required |
| `Adjusted` | `decimal(5,2)` | Required |
| `Available` | `decimal(5,2)` | Required |
| Audit fields | Shared | Include audit and soft delete fields |

Unique constraint: `EmployeeId`, `LeaveTypeId`, `Year`.

### 7.3 LeaveRequests

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `EmployeeId` | `uuid` | FK to `Employees` |
| `LeaveTypeId` | `uuid` | FK to `LeaveTypes` |
| `ApproverEmployeeId` | `uuid` | Nullable FK to `Employees` |
| `StartDate` | `date` | Required |
| `EndDate` | `date` | Required |
| `TotalDays` | `decimal(5,2)` | Required |
| `Reason` | `varchar(500)` | Nullable |
| `Status` | `varchar(50)` | Pending, Approved, Rejected, Cancelled |
| `DecisionAtUtc` | `timestamptz` | Nullable |
| `DecisionComments` | `varchar(500)` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields |

### 7.4 Holidays

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `OfficeLocationId` | `uuid` | Nullable FK to `OfficeLocations` |
| `Name` | `varchar(150)` | Required |
| `HolidayDate` | `date` | Required |
| `IsOptional` | `boolean` | Required |
| Audit fields | Shared | Include audit and soft delete fields |

## 8. Audit And Reporting Tables

### 8.1 AuditLogs

Stores immutable audit events for security-sensitive and HR-sensitive operations.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `UserId` | `uuid` | Nullable FK to `Users` |
| `EntityName` | `varchar(150)` | Required |
| `EntityId` | `uuid` | Nullable |
| `Action` | `varchar(100)` | Created, Updated, Deleted, Approved, Rejected, LoginFailed |
| `OldValuesJson` | `text` | Nullable |
| `NewValuesJson` | `text` | Nullable |
| `IpAddress` | `varchar(64)` | Nullable |
| `UserAgent` | `varchar(500)` | Nullable |
| `CreatedAtUtc` | `timestamptz` | Required |

Audit logs should be append-only. They should not use normal soft delete.

## 9. Notifications And Announcement Tables

### 9.1 Notifications

Stores personal, per-user in-app/email notifications (e.g. leave decisions, attendance alerts). Already implemented in code; documented here to close a gap between the shipped schema and this design doc.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `UserId` | `uuid` | Nullable FK to `Users`; recipient |
| `Title` | `varchar(250)` | Required |
| `Message` | `varchar(2000)` | Required |
| `Channel` | `varchar(50)` | `InApp` or `Email` |
| `IsRead` | `boolean` | Required |
| `CreatedAtUtc` | `timestamptz` | Required |
| `ReadAtUtc` | `timestamptz` | Nullable |
| `ExpiresAtUtc` | `timestamptz` | Nullable |
| `IsDeleted` | `boolean` | Required |
| `DeletedAtUtc` | `timestamptz` | Nullable |

> **Implementation note:** this table predates this section and uses a reduced audit set (`IsDeleted`/`CreatedAtUtc`/`DeletedAtUtc` only — no `CreatedBy`/`UpdatedAtUtc`/`UpdatedBy`/`RowVersion`), consistent with §3's allowance for a smaller audit set where appropriate. It does not follow the full Shared Audit Fields table.

### 9.2 Announcements

Stores company-wide broadcast announcements created by Admin/HR, distinct from personal `Notifications`. Default audience is the whole company; an announcement can optionally be scoped to one department or one role.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `Title` | `varchar(250)` | Required |
| `Message` | `varchar(2000)` | Required |
| `Priority` | `varchar(50)` | `Normal`, `Important`, `Critical` |
| `AudienceType` | `varchar(50)` | `All`, `Department`, `Role` |
| `DepartmentId` | `uuid` | Nullable FK to `Departments`; set when `AudienceType = Department` |
| `TargetRole` | `varchar(50)` | Nullable; set when `AudienceType = Role` (matches `Roles.Name`) |
| `CreatedByUserId` | `uuid` | FK to `Users`; author |
| `CreatedAtUtc` | `timestamptz` | Required |
| `ExpiresAtUtc` | `timestamptz` | Nullable |
| `IsDeleted` | `boolean` | Required; retracting an announcement soft-deletes it |
| `DeletedAtUtc` | `timestamptz` | Nullable |

> **Implementation note:** follows the same reduced audit set as `Notifications` (`IsDeleted`/`CreatedAtUtc`/`DeletedAtUtc` only), for consistency with the sibling table it was built alongside, rather than the full Shared Audit Fields table.

### 9.3 AnnouncementReads

Per-user read receipts for `Announcements`, since a single announcement row is shared across every recipient (unlike `Notifications`, where `IsRead` lives directly on the per-user row).

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `AnnouncementId` | `uuid` | FK to `Announcements` |
| `UserId` | `uuid` | FK to `Users` |
| `ReadAtUtc` | `timestamptz` | Required |

Unique constraint: `AnnouncementId`, `UserId`.

## 10. Relationships

### 10.1 Identity Relationships

- `Users` one-to-one optional `Employees`.
- `Users` many-to-many `Roles` through `UserRoles`.
- `Users` one-to-many `RefreshTokens`.
- `Users` one-to-many `PasswordResetTokens`.

### 10.2 Organization Relationships

- `Departments` one-to-many `Teams`.
- `Departments` one-to-many `Employees`.
- `Teams` one-to-many `Employees`.
- `Designations` one-to-many `Employees`.
- `OfficeLocations` one-to-many `Employees`.
- `Employees` self-referencing one-to-many through `ManagerId`.
- `Employees` one-to-many `EmployeeDocuments`.
- `Employees` optional one-to-one profile photo through `ProfilePhotoDocumentId`.

### 10.3 Attendance Relationships

- `Employees` one-to-many `AttendanceRecords`.
- `Shifts` one-to-many `AttendanceRecords`.
- `Employees` many-to-many effective shift assignments through `EmployeeShifts`.
- `AttendanceRecords` one-to-many `AttendanceCorrections`.
- `Employees` one-to-many requested attendance corrections.
- `Employees` one-to-many approved attendance corrections.

### 10.4 Leave Relationships

- `Employees` one-to-many `LeaveRequests`.
- `LeaveTypes` one-to-many `LeaveRequests`.
- `Employees` one-to-many leave approvals through `ApproverEmployeeId`.
- `Employees` one-to-many `LeaveBalances`.
- `LeaveTypes` one-to-many `LeaveBalances`.
- `OfficeLocations` one-to-many `Holidays`.

### 10.5 Notification And Announcement Relationships

- `Users` one-to-many `Notifications`.
- `Departments` optional one-to-many `Announcements` (via `DepartmentId`, when `AudienceType = Department`).
- `Users` one-to-many `Announcements` authored, through `CreatedByUserId`.
- `Announcements` one-to-many `AnnouncementReads`.
- `Users` one-to-many `AnnouncementReads`.

## 11. Index Strategy

### 11.1 Identity Indexes

| Table | Index | Type | Purpose |
| --- | --- | --- | --- |
| `Users` | `IX_Users_UserName` | Unique, filtered by `IsDeleted = 0` | Login lookup |
| `Users` | `IX_Users_Email` | Unique, filtered by `IsDeleted = 0` | Login and password reset lookup |
| `UserRoles` | `IX_UserRoles_RoleId` | Non-unique | Role membership lookup |
| `RefreshTokens` | `IX_RefreshTokens_UserId_IsRevoked_ExpiresAtUtc` | Non-unique | Active token lookup |
| `RefreshTokens` | `IX_RefreshTokens_TokenHash` | Unique | Token validation |
| `PasswordResetTokens` | `IX_PasswordResetTokens_UserId_ExpiresAtUtc` | Non-unique | Reset token cleanup |

### 11.2 Employee And Organization Indexes

| Table | Index | Type | Purpose |
| --- | --- | --- | --- |
| `Employees` | `IX_Employees_EmployeeCode` | Unique, filtered by `IsDeleted = 0` | Employee lookup |
| `Employees` | `IX_Employees_Email` | Unique, filtered by `IsDeleted = 0` | Contact and user linking |
| `Employees` | `IX_Employees_DepartmentId_Status` | Non-unique | Department dashboard |
| `Employees` | `IX_Employees_ManagerId` | Non-unique | Reporting hierarchy |
| `Employees` | `IX_Employees_DesignationId` | Non-unique | Employee filters |
| `Employees` | `IX_Employees_OfficeLocationId` | Non-unique | Location filters |
| `Departments` | `IX_Departments_Code` | Unique, filtered by `IsDeleted = 0` | Department lookup |
| `Teams` | `IX_Teams_DepartmentId_Code` | Unique, filtered by `IsDeleted = 0` | Team lookup |
| `Designations` | `IX_Designations_Code` | Unique, filtered by `IsDeleted = 0` | Designation lookup |
| `OfficeLocations` | `IX_OfficeLocations_Code` | Unique, filtered by `IsDeleted = 0` | Location lookup |
| `EmployeeDocuments` | `IX_EmployeeDocuments_EmployeeId_DocumentType` | Non-unique | Document list screens |
| `Clients` | `IX_Clients_ClientName` | Unique, not filtered (see §16.1) | Client lookup and duplicate-name prevention |
| `Clients` | `IX_Clients_IsActive` | Non-unique | Active-status filter on the client list |
| `Tasks` | `IX_Tasks_TaskNumber` | Unique | Task lookup |
| `Tasks` | `IX_Tasks_AssignedEmployeeId` | Non-unique | "My tasks" list and self-scoping checks |
| `Tasks` | `IX_Tasks_ClientId` | Non-unique | Tasks-by-client filter |
| `Tasks` | `IX_Tasks_Status` | Non-unique | Status filter on the task list |
| `TaskComments` | `IX_TaskComments_TaskId_CreatedAtUtc` | Non-unique | Chronological comment feed per task |
| `TaskAttachments` | `IX_TaskAttachments_TaskId` | Non-unique | Attachment list per task |
| `Reimbursements` | `IX_Reimbursements_ReimbursementNumber` | Unique | Reimbursement lookup |
| `Reimbursements` | `IX_Reimbursements_EmployeeId` | Non-unique | "My reimbursements" list and self-scoping checks |
| `Reimbursements` | `IX_Reimbursements_Status` | Non-unique | Status filter on the admin review queue |
| `Reimbursements` | `IX_Reimbursements_EmployeeId_Status_PayrollProcessed` | Non-unique | The exact predicate Payroll queries: approved, unprocessed, per employee |
| `ReimbursementAttachments` | `IX_ReimbursementAttachments_ReimbursementId` | Non-unique | Attachment list per reimbursement |

### 11.3 Attendance Indexes

| Table | Index | Type | Purpose |
| --- | --- | --- | --- |
| `AttendanceRecords` | `IX_AttendanceRecords_EmployeeId_AttendanceDate` | Unique, filtered by `IsDeleted = 0` | Daily attendance uniqueness |
| `AttendanceRecords` | `IX_AttendanceRecords_AttendanceDate_Status` | Non-unique | Daily dashboard |
| `AttendanceRecords` | `IX_AttendanceRecords_EmployeeId_AttendanceDate_Status` | Non-unique | Employee attendance history |
| `AttendanceCorrections` | `IX_AttendanceCorrections_Status` | Non-unique | Pending approvals |
| `EmployeeShifts` | `IX_EmployeeShifts_EmployeeId_EffectiveFrom` | Non-unique | Shift lookup by date |

### 11.4 Leave Indexes

| Table | Index | Type | Purpose |
| --- | --- | --- | --- |
| `LeaveTypes` | `IX_LeaveTypes_Code` | Unique, filtered by `IsDeleted = 0` | Leave type lookup |
| `LeaveBalances` | `IX_LeaveBalances_EmployeeId_LeaveTypeId_Year` | Unique, filtered by `IsDeleted = 0` | Balance lookup |
| `LeaveRequests` | `IX_LeaveRequests_EmployeeId_StartDate_EndDate` | Non-unique | Leave history |
| `LeaveRequests` | `IX_LeaveRequests_ApproverEmployeeId_Status` | Non-unique | Approval queue |
| `LeaveRequests` | `IX_LeaveRequests_Status_StartDate` | Non-unique | Leave dashboard |
| `Holidays` | `IX_Holidays_OfficeLocationId_HolidayDate` | Non-unique | Holiday calendar |

### 11.5 Audit Indexes

| Table | Index | Type | Purpose |
| --- | --- | --- | --- |
| `AuditLogs` | `IX_AuditLogs_EntityName_EntityId_CreatedAtUtc` | Non-unique | Entity audit history |
| `AuditLogs` | `IX_AuditLogs_UserId_CreatedAtUtc` | Non-unique | User activity lookup |
| `AuditLogs` | `IX_AuditLogs_Action_CreatedAtUtc` | Non-unique | Security reporting |

### 11.6 Notification And Announcement Indexes

| Table | Index | Type | Purpose |
| --- | --- | --- | --- |
| `Notifications` | `IX_Notifications_UserId` | Non-unique | Personal notification list |
| `Notifications` | `IX_Notifications_CreatedAtUtc` | Non-unique | Recency ordering |
| `Announcements` | `IX_Announcements_AudienceType_DepartmentId` | Non-unique | Department-scoped visibility filter |
| `Announcements` | `IX_Announcements_AudienceType_TargetRole` | Non-unique | Role-scoped visibility filter |
| `Announcements` | `IX_Announcements_CreatedAtUtc` | Non-unique | Recency ordering |
| `AnnouncementReads` | `IX_AnnouncementReads_AnnouncementId_UserId` | Unique | Read-receipt lookup and idempotency |

### 11.7 Payroll Indexes

| Table | Index | Type | Purpose |
| --- | --- | --- | --- |
| `SalaryStructures` | `IX_SalaryStructures_EmployeeId` | Non-unique | Current/effective salary structure lookup per employee |
| `SalaryStructures` | `IX_SalaryStructures_IsDeleted` | Non-unique | Excluding soft-deleted rows from every read path |
| `Allowances` | `IX_Allowances_SalaryStructureId` | Non-unique | Allowances per salary structure |
| `Deductions` | `IX_Deductions_SalaryStructureId` | Non-unique | Deductions per salary structure |
| `Payslips` | `IX_Payslips_EmployeeId` | Non-unique | Payslip history per employee |
| `Payslips` | `IX_Payslips_PayrollRunId` | Non-unique | Payslips within a run |

`PayrollRuns` has no indexes beyond its primary key.

## 12. Soft Delete Strategy

Soft delete should be implemented for business data where historical traceability matters.

Soft-deleted tables:

- `Users`
- `Roles`
- `Employees`
- `EmployeeDocuments`
- `Departments`
- `Teams`
- `Designations`
- `OfficeLocations`
- `Shifts`
- `EmployeeShifts`
- `AttendanceRecords`
- `AttendanceCorrections`
- `LeaveTypes`
- `LeaveBalances`
- `LeaveRequests`
- `Holidays`
- `Notifications`
- `Announcements`
- `Clients`
- `Reimbursements`
- `SalaryStructures`

Not normally soft-deleted:

- `UserRoles`: remove assignment rows or add effective dating later if role history is required.
- `RefreshTokens`: revoke first, then purge after retention.
- `PasswordResetTokens`: purge after expiry and retention.
- `AuditLogs`: append-only, no soft delete.
- `AnnouncementReads`: append-only read receipts — a row is inserted once per `(AnnouncementId, UserId)` and never updated or soft-deleted.
- `Tasks`: no soft delete — deliberately. There is no "Delete Task" action; `Cancel Task` is a status transition (`Status = Cancelled`), not a deletion, so a task is never removed from the table at all. See §16.1.
- `TaskComments`, `TaskAttachments`: append-only child records of `Tasks` — never updated or deleted, matching `AnnouncementReads`.
- `ReimbursementAttachments`: append-only child records of `Reimbursements` — never updated or deleted.
- `Allowances`, `Deductions`: no independent update/delete lifecycle — recreated wholesale by `UpdateSalaryStructureCommandHandler`, matching `TaskComments`/`ReimbursementAttachments`. `SalaryStructures` itself *is* soft-deleted (added to the list above) since it has real Create/Update/Delete/Restore actions. See the implementation note in §15.
- `PayrollRuns`, `Payslips`: no soft delete — deliberately. Neither has a Delete action; both are immutable records of payroll actually processed/paid. See the implementation note in §15.

Implementation rules:

- EF Core global query filters should apply `IsDeleted = 0` automatically.
- Delete operations should set `IsDeleted`, `DeletedAtUtc`, and `DeletedBy`.
- Unique indexes on soft-deleted business tables should be filtered by `IsDeleted = 0`.
- Administrative restore operations should be restricted to authorized roles.
- Hard delete should be allowed only for expired temporary security records or controlled data retention jobs.

## 13. Delete Behavior

Recommended foreign key delete behavior:

- Use `Restrict` or `NoAction` for most business relationships.
- Do not cascade delete employees into attendance, leave, documents, or audit history.
- When an employee leaves, update `Status` and `ExitDate` instead of deleting the record.
- When a department is retired, soft delete or mark inactive only after employees are reassigned.
- Use explicit application workflows for deletion so audit logs are created consistently.

## 14. Future Module Extension Points

Phase 2 and Phase 3 modules should be added in separate bounded table groups:

- Payroll: `SalaryStructures`, `Allowances`, `Deductions`, `PayrollRuns`, `Payslips` are implemented — see §15, including Bonus/Overtime (`Payslips.TotalBonus`/`TotalOvertime`/`OvertimeHours` — no separate tables needed) and the FK/relationship fix and audit/soft-delete backfill documented in §15's implementation note.
- Announcements: `Announcements` and `Notifications` are implemented — see §9. `EmailLogs` remains a future extension point.
- Client Master: `Clients` is implemented — see §16.
- Tasks: `Tasks`, `TaskComments`, `TaskAttachments` are implemented — see §17. (No separate `TaskAssignments` table: a task has exactly one assignee at a time, tracked directly on `Tasks.AssignedEmployeeId`; reassignment overwrites it and is itself audited via `AuditLogs`, so a full assignment-history table wasn't needed.)
- Expenses: `Reimbursements`, `ReimbursementAttachments` are implemented — see §18. (No separate `ExpenseClaims`/`ExpenseClaimItems` tables: requirements.md describes one flat reimbursement request per expense, not a multi-line claim, so one table covers it.)
- Recruitment: `Candidates`, `Interviews`, `Offers`, `OnboardingChecklists`.
- Assets: `Assets`, `AssetAssignments`, `AssetReturns`.
- Performance: `Goals`, `Kpis`, `PerformanceReviews`, `Promotions`.
- Messaging: `Conversations`, `Messages`, `MessageParticipants`.

Each future module should follow the same audit, soft delete, indexing, and ownership rules unless there is a clear compliance reason to do otherwise.

## 15. Payroll Tables

Implemented in Phase 1, before this document had Client/Task/Reimbursement-style per-module sections — backfilled here for the first time. See §18.3 for the `TotalReimbursements` column added to `Payslips` by Reimbursement Management.

### 15.1 SalaryStructures

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `EmployeeId` | `uuid` | Required FK to `Employees`, `Restrict` on delete. Indexed |
| `BasicSalary` | `numeric` | Required |
| `EffectiveFrom` | `timestamptz` | Required |
| `EffectiveTo` | `timestamptz` | Nullable |
| `IsDeleted` | `boolean` | Required, default `false`. Indexed |
| `CreatedAtUtc` | `timestamptz` | Required |
| `CreatedBy` | `uuid` | Nullable |
| `UpdatedAtUtc` | `timestamptz` | Nullable |
| `UpdatedBy` | `uuid` | Nullable |
| `DeletedAtUtc` | `timestamptz` | Nullable |
| `DeletedBy` | `uuid` | Nullable |
| `RowVersion` (`xmin`) | — | Optimistic concurrency, same `.IsRowVersion()` mapping as §3 |

The only Payroll table with a full CRUD lifecycle (Create/Update/Delete/Restore), so it's the only one of the five that gets the complete Shared Audit Fields (§3) set — see the implementation note below for why the other four don't.

### 15.2 Allowances

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `SalaryStructureId` | `uuid` | Required FK to `SalaryStructures`, `Cascade` on delete |
| `Name` | `varchar(150)` | Required |
| `Amount` | `numeric` | Required |
| `CreatedAtUtc` | `timestamptz` | Required. See implementation note below — no other audit/soft-delete columns |

### 15.3 Deductions

Same shape as `Allowances` (§15.2):

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `SalaryStructureId` | `uuid` | Required FK to `SalaryStructures`, `Cascade` on delete |
| `Name` | `varchar(150)` | Required |
| `Amount` | `numeric` | Required |
| `CreatedAtUtc` | `timestamptz` | Required |

### 15.4 PayrollRuns

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `PeriodStart` | `timestamptz` | Required |
| `PeriodEnd` | `timestamptz` | Required |
| `ProcessedAtUtc` | `timestamptz` | Required |
| `ProcessedBy` | `uuid` | Required. Not FK-enforced (loose reference to `Users`, matching `Tasks.AssignedByUserId`'s style) |
| `Status` | `text` | Nullable |
| `UpdatedAtUtc` | `timestamptz` | Nullable. Stamped on Approve — the run's only other lifecycle event besides creation |
| `UpdatedBy` | `uuid` | Nullable. The approver |

### 15.5 Payslips

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `PayrollRunId` | `uuid` | Required FK to `PayrollRuns`, `Cascade` on delete |
| `EmployeeId` | `uuid` | Required FK to `Employees`, `Restrict` on delete. Indexed |
| `Basic` | `numeric` | Required |
| `TotalAllowances` | `numeric` | Required |
| `TotalDeductions` | `numeric` | Required |
| `TotalReimbursements` | `numeric` | Required, default `0`. Added by Reimbursement Management — see §18.3 |
| `TotalBonus` | `numeric` | Required, default `0`. Discretionary, manual-entry only (`ProcessPayrollCommand.Adjustments[].BonusAmount`) — there's no basis to auto-calculate a discretionary amount. Included in `GrossPay` (taxable earnings, unlike `TotalReimbursements`) |
| `TotalOvertime` | `numeric` | Required, default `0`. Auto-calculated from `AttendanceRecords.TotalWorkMinutes` vs. the employee's assigned `Shift` for each day in the period by default; a per-employee `Adjustments[].OvertimeAmount` override always wins. Included in `GrossPay` |
| `OvertimeHours` | `numeric` | Required, default `0`. The overtime hours behind `TotalOvertime` when auto-calculated; `0` when `TotalOvertime` came from a manual override (an amount, not a derived hour count) |
| `GrossPay` | `numeric` | Required. `Basic + TotalAllowances + TotalBonus + TotalOvertime` |
| `NetPay` | `numeric` | Required. `GrossPay - TotalDeductions + TotalReimbursements` |
| `GeneratedAtUtc` | `timestamptz` | Required |
| `BlobPath` | `text` | Nullable |
| `BlobContainer` | `text` | Nullable |
| Audit fields | None | No audit or soft-delete columns exist on this table |

> **Implementation note — relationship/FK fix, and audit/soft-delete backfill (migrations `FixPayrollRelationshipsAndForeignKeys`, `AddPayrollAuditAndSoftDelete`):**
> - **Relationship/FK fix**: `SalaryStructures.EmployeeId` and `Payslips.EmployeeId` now have real FK constraints to `Employees` (`Restrict`), with `SalaryStructures.EmployeeId` also indexed for the first time. Separately, `SalaryStructureConfiguration`/`PayrollRunConfiguration` originally declared the parent side of the Allowances/Deductions/Payslips relationships as `HasMany<Allowance>().WithOne()` (etc.) without pointing at the entity's own navigation collection — EF Core read that as *two* independent relationships (the explicit `HasForeignKey` one, plus a second by-convention one inferred from the orphaned navigation) and materialized the second as an extra, always-`NULL` shadow FK column (`SalaryStructureId1`/`PayrollRunId1`) that the application never populated. Against the real (relational) provider, `.Include(s => s.Allowances)` joined on that dead shadow column, so **`GetEffectiveSalaryStructureAsync` silently returned zero allowances/deductions for every employee** — Payroll would compute correct `Basic` but always `$0` `TotalAllowances`/`TotalDeductions`. It also meant `UpdateSalaryStructureCommandHandler`'s "replace children" only cleared the shadow FK (an optional relationship, so EF nulls rather than deletes) instead of the real one, permanently orphaning every prior Allowance/Deduction row on each edit. Fixed by binding the relationships to the real navigation properties (`HasMany(s => s.Allowances)`); the migration drops the three dead shadow columns. See `SalaryStructureConfiguration.cs`/`PayrollRunConfiguration.cs` and `EMS.Tests/PayrollTests.cs`'s `SalaryStructure_AllowancesAndDeductions_RoundTripThroughRealNavigation` regression test.
> - **Audit/soft-delete backfill**: applied unevenly across the five tables on purpose, matching each table's actual lifecycle rather than blanket-copying the full Shared Audit Fields (§3) set everywhere:
>   - `SalaryStructures` gets the complete set (§15.1) — it's the only table here with independent Create/Update/Delete/Restore actions (`POST`/`PUT`/`DELETE`/`POST .../restore` under `/payroll/salary-structures`), so it needed real soft delete the same way `Clients`/`Employees` do. `DeleteSalaryStructureCommandHandler` now soft-deletes (stamping `DeletedAtUtc`/`DeletedBy`) instead of hard-deleting; `GetSalaryStructureByIdAsync`/`GetSalaryStructuresAsync`/`GetEffectiveSalaryStructureAsync` all exclude `IsDeleted` rows, so a deleted structure can never be picked up by a payroll run.
>   - `Allowances`/`Deductions` get `CreatedAtUtc` only — no update/delete lifecycle of their own since `UpdateSalaryStructureCommandHandler` always recreates them wholesale (clears and re-inserts) rather than editing a row in place, matching the same "append-only child, no independent lifecycle" reasoning already applied to `TaskComments`/`ReimbursementAttachments` elsewhere in this document.
>   - `PayrollRuns` gets `UpdatedAtUtc`/`UpdatedBy` only (stamped by Approve) — `CreatedAtUtc`/`CreatedBy` were deliberately **not** added since `ProcessedAtUtc`/`ProcessedBy` already record that exact same creation event under a more specific name; adding both would just be two columns holding identical data. No soft delete either — there is no Delete action on a payroll run (it's an immutable record of what was paid), matching the same "no unused field" reasoning `Tasks` uses for skipping soft delete.
>   - `Payslips` gets nothing further — `GeneratedAtUtc` already serves as its creation timestamp, and a payslip is never updated or deleted after generation (it's the legal/financial record of what an employee was actually paid).

> **Implementation note (Bonus & Overtime, migration `AddPayrollBonusAndOvertime`):** requirements.md lists "Bonus" and "Overtime" under Payroll Management with no further detail — no formula, no input mechanism. Bonus is treated as manual-entry-only (a discretionary amount has no basis for auto-calculation); Overtime auto-calculates by default but always accepts a manual override, since attendance-derived overtime can be wrong (missed punch, uncorrected attendance record, etc.) and there's no attendance-independent way to detect that. The overtime formula, entirely invented here since requirements.md specifies none:
> 1. For each `AttendanceRecord` in the pay period with a `TotalWorkMinutes` value, look up the employee's assigned `Shift` for that day (via `EmployeeShift`/`AttendanceRecord.ShiftId`) and take `(Shift.EndTime - Shift.StartTime)` in minutes as the standard for that day (wrapping across midnight for night shifts); with no shift assigned, use a configurable default (`Payroll:DefaultDailyShiftMinutes`, default 480 = 8 hours).
> 2. Sum `max(0, TotalWorkMinutes - standard)` across the period → total overtime minutes → hours.
> 3. Hourly rate = `SalaryStructure.BasicSalary / Payroll:StandardMonthlyHours` (default 208).
> 4. `TotalOvertime = OvertimeHours × HourlyRate × Payroll:OvertimeMultiplier` (default 1.5).
>
> All three constants are configuration, not hardcoded (`appsettings.json` → `Payroll` section). See `EMS.Application/Features/Payroll/OvertimeCalculator.cs` for the pure calculation logic and `ProcessPayrollCommandHandler.CalculateOvertimeAsync`/`DryRunPayrollQueryHandler.CalculateOvertimeAsync` for how it's wired into a run.

## 16. Client Tables

### 16.1 Clients

Client Master (see [requirements.md](requirements.md#client-master-new-module--supports-task-management)). Read access is open to any authenticated user; all mutations are Admin-only — this is intentionally not delegated to HR.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `ClientName` | `varchar(150)` | Required, unique |
| `CompanyName` | `varchar(150)` | Required |
| `ContactPerson` | `varchar(150)` | Required |
| `MobileNumber` | `varchar(20)` | Required |
| `AlternateMobile` | `varchar(20)` | Nullable |
| `Email` | `varchar(255)` | Required |
| `GstNumber` | `varchar(20)` | Nullable |
| `AddressLine1` | `varchar(250)` | Required |
| `AddressLine2` | `varchar(250)` | Nullable |
| `City` | `varchar(100)` | Required |
| `State` | `varchar(100)` | Nullable |
| `Country` | `varchar(100)` | Required |
| `PostalCode` | `varchar(20)` | Required |
| `Latitude` | `decimal(9,6)` | Nullable |
| `Longitude` | `decimal(9,6)` | Nullable |
| `Notes` | `varchar(1000)` | Nullable |
| `IsActive` | `boolean` | Required, default `true`. Inactive clients cannot receive new tasks (enforced when Task Management ships) |
| `IsArchived` | `boolean` | Required, default `false`. Retired from active workflows but retained for history — distinct from soft delete |
| Audit fields | Shared | Include audit and soft delete fields |

Unique index on `ClientName`. As implemented, the index itself is not filtered by `IsDeleted`; a soft-deleted client's name stays reserved unless it is restored or the row is purged. Rejecting a duplicate name against active *and* soft-deleted rows is enforced in the application layer (`IClientRepository.NameExistsAsync`, checked from `CreateClientCommandValidator`/`UpdateClientCommandValidator`) — the same pattern already used for `Designations`/`Teams`/`OfficeLocations` code uniqueness, despite §11.2 describing those as filtered indexes.

## 17. Task Tables

See [requirements.md](requirements.md#task-management) for the source requirement. A task's C# entity class is named `TaskItem`, not `Task` — `EMS.Domain.Entities.Task` would collide with `System.Threading.Tasks.Task`, which every async handler in this codebase uses. The database table itself is still named `Tasks`.

### 17.1 Tasks

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `TaskNumber` | `varchar(20)` | Required, unique. Derived from `Id` (e.g. `TSK-3F2A9B10`), not user-editable |
| `Title` | `varchar(200)` | Required |
| `Description` | `varchar(2000)` | Nullable |
| `ClientId` | `uuid` | Nullable FK to `Clients`, `Restrict` on delete. Not every task is a client visit — some are internal/office work |
| `AssignedEmployeeId` | `uuid` | Required FK to `Employees`, `Restrict` on delete |
| `AssignedByUserId` | `uuid` | Required. The Admin who created/assigned the task — not a FK-enforced relationship (matches the `AuditLogs`-style loose reference to `Users`) |
| `AssignedDate` | `timestamptz` | Required |
| `DueDate` | `timestamptz` | Nullable |
| `Priority` | `varchar(20)` | Required. `Low`, `Medium`, `High`, `Critical` |
| `Status` | `varchar(20)` | Required, default `Assigned`. `Assigned`, `Accepted`, `Rejected`, `InProgress`, `OnHold`, `Completed`, `Cancelled` — `Rejected` is one more than the six requirements.md lists, added because the "Reject task" employee action needs somewhere to land that isn't `Cancelled` (an Admin-only action) or a silent no-op back to `Assigned` |
| `Notes` | `varchar(1000)` | Nullable |
| `CompletedAtUtc` | `timestamptz` | Nullable. Set when `Status` becomes `Completed` |
| Audit fields | Partial | `CreatedAtUtc`/`CreatedBy`/`UpdatedAtUtc`/`UpdatedBy` only — **no soft delete**. requirements.md lists no "Delete Task" action; `Cancel Task` (a status, not a deletion) is the only removal path, so `IsDeleted`/`DeletedAtUtc`/`DeletedBy` were left off rather than added unused |

### 17.2 TaskComments

Append-only progress/notes log — never updated or deleted, matching the `AnnouncementReads` convention (§12).

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `TaskId` | `uuid` | Required FK to `Tasks`, `Cascade` on delete |
| `AuthorUserId` | `uuid` | Required |
| `Comment` | `varchar(2000)` | Required |
| `CreatedAtUtc` | `timestamptz` | Required |

### 17.3 TaskAttachments

Uploaded photo/document evidence for a task ("Upload photos"). Mirrors `EmployeeDocuments` (§5.6), minus the fields that don't apply here (`DocumentType`, `ExpiresAtUtc`, soft delete). Files are stored via the same `IFileStorageService` used for employee documents, under a separate `task-attachments` container.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `TaskId` | `uuid` | Required FK to `Tasks`, `Cascade` on delete |
| `OriginalFileName` | `varchar(255)` | Required |
| `ContentType` | `varchar(100)` | Required. Restricted to `application/pdf`, `image/jpeg`, `image/png` — magic-byte verified, same as `EmployeeDocuments` uploads |
| `FileSizeBytes` | `bigint` | Required. 10 MB max |
| `BlobContainer` | `varchar(100)` | Required |
| `BlobPath` | `varchar(500)` | Required |
| `UploadedAtUtc` | `timestamptz` | Required |
| `UploadedBy` | `uuid` | Nullable |

## 18. Reimbursement Tables

See [requirements.md](requirements.md#expense-management-employee-reimbursement-management) for the source requirement.

### 18.1 Reimbursements

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `ReimbursementNumber` | `varchar(20)` | Required, unique. Derived from `Id` (e.g. `REI-3F2A9B10`), not user-editable |
| `EmployeeId` | `uuid` | Required FK to `Employees`, `Restrict` on delete. Always the caller's own employee record — never client-suppliable |
| `ExpenseTitle` | `varchar(200)` | Required |
| `ExpenseCategory` | `varchar(100)` | Required. Free text — requirements.md doesn't enumerate a fixed category list (unlike Leave Types), so this isn't an enum |
| `ExpenseDate` | `timestamptz` | Required. Cannot be in the future |
| `Amount` | `decimal(18,2)` | Required, must be `> 0` |
| `Currency` | `varchar(10)` | Required, default `USD`. Free text, not validated against an ISO 4217 list |
| `Description` | `varchar(2000)` | Nullable |
| `Notes` | `varchar(1000)` | Nullable |
| `Status` | `varchar(20)` | Required, default `Draft`. `Draft`, `Submitted`, `UnderReview`, `Approved`, `Rejected`, `ChangesRequested`, `Paid` — matches the workflow diagram in requirements.md exactly, including the `UnderReview` step between `Submitted` and the approve/reject/changes-requested branch |
| `SubmittedAtUtc` | `timestamptz` | Nullable. Set when `Status` becomes `Submitted` |
| `ApprovedAtUtc` | `timestamptz` | Nullable. Set when `Status` becomes `Approved` |
| `ApprovedBy` | `uuid` | Nullable. The reviewing Admin's user id |
| `ReviewRemarks` | `varchar(1000)` | Nullable. Set on Reject or Request Changes — "View approval remarks" for the employee |
| `PayrollProcessed` | `boolean` | Required, default `false`. Set once folded into a payroll run — prevents double payment |
| `PayrollRunId` | `uuid` | Nullable. Not FK-enforced (loose reference to `PayrollRuns`, matching `Tasks.AssignedByUserId`'s style) |
| `PayrollDate` | `timestamptz` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields — "Delete Draft reimbursement" is a real action here (unlike Tasks), so soft delete applies |

Business rules enforced in the application layer, not the schema: an employee cannot approve their own reimbursement (checked against the reviewer's own `EmployeeId`, regardless of role); edits are only accepted while `Draft` or `ChangesRequested`; delete is only accepted while `Draft`; review actions (`start-review`/`approve`/`reject`/`request-changes`) all require `UnderReview` except `start-review` itself, which requires `Submitted`.

### 18.2 ReimbursementAttachments

Uploaded receipt/supporting document for a reimbursement ("Upload one or more supporting documents"). Mirrors `TaskAttachments` (§17.3) exactly, under a separate `reimbursement-attachments` container.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uuid` | Primary key |
| `ReimbursementId` | `uuid` | Required FK to `Reimbursements`, `Cascade` on delete |
| `OriginalFileName` | `varchar(255)` | Required |
| `ContentType` | `varchar(100)` | Required. Restricted to `application/pdf`, `image/jpeg`, `image/png` — magic-byte verified |
| `FileSizeBytes` | `bigint` | Required. 10 MB max |
| `BlobContainer` | `varchar(100)` | Required |
| `BlobPath` | `varchar(500)` | Required |
| `UploadedAtUtc` | `timestamptz` | Required |
| `UploadedBy` | `uuid` | Nullable |

### 18.3 Payslips.TotalReimbursements (Payroll Integration)

One column added to the `Payslips` table (§15.5): `TotalReimbursements` (`decimal`, required, default `0`). Sum of an employee's `Approved`, not-yet-`PayrollProcessed` reimbursements as of the run. Added to `NetPay`, not `GrossPay` — reimbursements are expense repayments, not taxable earnings. When a payroll run processes a reimbursement, that `Reimbursement` row is updated in the same unit of work: `Status` → `Paid`, `PayrollProcessed` → `true`, `PayrollRunId`/`PayrollDate` stamped — so a later run's query for "approved and unprocessed" can never select it again.

