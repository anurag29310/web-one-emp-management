# Employee Management System Database Design

## 1. Overview

The Employee Management System will use SQL Server for local and hosted database workloads, with Azure SQL Database recommended for Azure deployment. Entity Framework Core migrations will manage schema changes, and the data model will follow the Clean Architecture boundaries defined in `docs/architecture.md`.

Design decisions:

- Use relational tables for core HR, attendance, leave, department, and identity data.
- Store files in Azure Blob Storage and keep only file metadata in SQL Server.
- Use audit fields and soft delete on business tables as required by `AI_CONTRACT.md`.
- Use proper foreign keys and indexes to support 10,000+ employees and response times under 2 seconds.
- Keep authentication tables separate from employee profile tables so user accounts can be managed independently from HR records.

## 2. Naming And Data Type Conventions

Recommended conventions:

- Primary keys: `Id` as `uniqueidentifier`.
- Foreign keys: `{EntityName}Id` as `uniqueidentifier`.
- Dates and timestamps: UTC, using `datetime2`.
- Status fields: `nvarchar(50)` or small enum-backed integers. Use one approach consistently in implementation.
- Text fields: use bounded `nvarchar` lengths instead of `nvarchar(max)` unless the field is intentionally long.
- Money values in future payroll modules: `decimal(18,2)`.
- Row version: `rowversion` for optimistic concurrency.

## 3. Shared Audit Fields

All business tables should include the following columns unless noted otherwise:

| Column | Type | Required | Purpose |
| --- | --- | --- | --- |
| `Id` | `uniqueidentifier` | Yes | Primary key |
| `CreatedAtUtc` | `datetime2` | Yes | Record creation timestamp |
| `CreatedBy` | `uniqueidentifier` | No | User who created the record |
| `UpdatedAtUtc` | `datetime2` | No | Last update timestamp |
| `UpdatedBy` | `uniqueidentifier` | No | User who last updated the record |
| `DeletedAtUtc` | `datetime2` | No | Soft delete timestamp |
| `DeletedBy` | `uniqueidentifier` | No | User who soft deleted the record |
| `IsDeleted` | `bit` | Yes | Soft delete flag |
| `RowVersion` | `rowversion` | Yes | Optimistic concurrency token |

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
> what's listed also exist on the real `Users` table: `MfaSecretProtected` (`nvarchar(max)`,
> nullable) — the TOTP secret, encrypted at rest via ASP.NET Core Data Protection, never stored
> or transmitted in plaintext after enrollment — and `MfaEnabledAtUtc` (`datetime2`, nullable).
> They're omitted from the table below because they're an implementation detail of how
> `IsMfaEnabled` is realized, not part of the target ERD shape.

### 4.1 Users

Stores application login accounts.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `EmployeeId` | `uniqueidentifier` | Nullable FK to `Employees` |
| `UserName` | `nvarchar(100)` | Unique |
| `Email` | `nvarchar(256)` | Unique |
| `PasswordHash` | `nvarchar(max)` | Required |
| `IsActive` | `bit` | Required |
| `IsMfaEnabled` | `bit` | Required |
| `LastLoginAtUtc` | `datetime2` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields |

### 4.2 Roles

Stores role definitions.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `Name` | `nvarchar(50)` | Unique: `Admin`, `HR`, `Manager`, `Employee` |
| `Description` | `nvarchar(250)` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields |

### 4.3 UserRoles

Maps users to roles.

| Column | Type | Notes |
| --- | --- | --- |
| `UserId` | `uniqueidentifier` | FK to `Users` |
| `RoleId` | `uniqueidentifier` | FK to `Roles` |
| `AssignedAtUtc` | `datetime2` | Required |
| `AssignedBy` | `uniqueidentifier` | Nullable FK to `Users` |

Primary key: composite key on `UserId`, `RoleId`.

### 4.4 RefreshTokens

Stores hashed refresh tokens for JWT session renewal.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `UserId` | `uniqueidentifier` | FK to `Users` |
| `TokenHash` | `nvarchar(512)` | Required |
| `TokenFamilyId` | `uniqueidentifier` | Groups rotated tokens |
| `IssuedAtUtc` | `datetime2` | Required |
| `ExpiresAtUtc` | `datetime2` | Required |
| `RevokedAtUtc` | `datetime2` | Nullable |
| `ReplacedByTokenId` | `uniqueidentifier` | Nullable FK to `RefreshTokens` |
| `IpAddress` | `nvarchar(64)` | Nullable |
| `UserAgent` | `nvarchar(500)` | Nullable |
| `IsRevoked` | `bit` | Required |

Refresh tokens should be hard deleted only after expiry and retention policy allow it.

### 4.5 PasswordResetTokens

Stores password reset requests.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `UserId` | `uniqueidentifier` | FK to `Users` |
| `TokenHash` | `nvarchar(512)` | Required |
| `ExpiresAtUtc` | `datetime2` | Required |
| `UsedAtUtc` | `datetime2` | Nullable |
| `CreatedAtUtc` | `datetime2` | Required |
| `IpAddress` | `nvarchar(64)` | Nullable |

### 4.6 MfaChallenges

Short-lived server-side record backing the `mfaChallengeId` a client receives from `POST
/auth/login` when `requiresMfa: true`. Exists so the pending-second-factor state survives past a
single request/process (unlike an in-memory cache) and works correctly behind a load balancer.
Rows are cheap to accumulate since each is single-use and short-lived; a periodic cleanup of
expired rows is a reasonable operational follow-up but not required for correctness.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key; this is the `mfaChallengeId` |
| `UserId` | `uniqueidentifier` | FK to `Users` |
| `CreatedAtUtc` | `datetime2` | Required |
| `ExpiresAtUtc` | `datetime2` | Required — 5 minutes from creation |
| `IsConsumed` | `bit` | Required — set once a code has been successfully verified against this challenge |

### 4.7 MfaRecoveryCodes

One-time backup codes issued when a user enables MFA (10 per enrollment), so losing the
authenticator device doesn't permanently lock the account out. Each code is shown to the user
exactly once at generation time and stored only as a hash — same treatment as `PasswordHash` on
`Users`, not reversible. Regenerating (`POST /auth/mfa/recovery-codes/regenerate`) invalidates
every prior code for the user, used or not, and issues 10 fresh ones.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `UserId` | `uniqueidentifier` | FK to `Users` |
| `CodeHash` | `nvarchar(max)` | Required — same PBKDF2 hashing as `Users.PasswordHash` |
| `CreatedAtUtc` | `datetime2` | Required |
| `UsedAtUtc` | `datetime2` | Nullable — set on first (and only) successful use |

## 5. Employee And Organization Tables

### 5.1 Employees

Stores employee profile and employment information.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `EmployeeCode` | `nvarchar(50)` | Unique |
| `FirstName` | `nvarchar(100)` | Required |
| `MiddleName` | `nvarchar(100)` | Nullable |
| `LastName` | `nvarchar(100)` | Required |
| `Email` | `nvarchar(256)` | Unique for active employees |
| `PhoneNumber` | `nvarchar(30)` | Nullable |
| `DateOfBirth` | `date` | Nullable |
| `Gender` | `nvarchar(50)` | Nullable |
| `AddressLine1` | `nvarchar(250)` | Nullable |
| `AddressLine2` | `nvarchar(250)` | Nullable |
| `City` | `nvarchar(100)` | Nullable |
| `State` | `nvarchar(100)` | Nullable |
| `PostalCode` | `nvarchar(20)` | Nullable |
| `Country` | `nvarchar(100)` | Nullable |
| `EmergencyContactName` | `nvarchar(150)` | Nullable |
| `EmergencyContactPhone` | `nvarchar(30)` | Nullable |
| `EmergencyContactRelation` | `nvarchar(100)` | Nullable |
| `DepartmentId` | `uniqueidentifier` | FK to `Departments` |
| `TeamId` | `uniqueidentifier` | Nullable FK to `Teams` |
| `DesignationId` | `uniqueidentifier` | FK to `Designations` |
| `ManagerId` | `uniqueidentifier` | Nullable self FK to `Employees` |
| `OfficeLocationId` | `uniqueidentifier` | FK to `OfficeLocations` |
| `JoinDate` | `date` | Required |
| `ExitDate` | `date` | Nullable |
| `Status` | `nvarchar(50)` | Active, Inactive, OnLeave, Terminated |
| `ProfilePhotoDocumentId` | `uniqueidentifier` | Nullable FK to `EmployeeDocuments` |
| Audit fields | Shared | Include audit and soft delete fields |

### 5.2 Departments

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `Name` | `nvarchar(150)` | Unique |
| `Code` | `nvarchar(50)` | Unique |
| `Description` | `nvarchar(500)` | Nullable |
| `HeadEmployeeId` | `uniqueidentifier` | Nullable FK to `Employees` |
| Audit fields | Shared | Include audit and soft delete fields |

### 5.3 Teams

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `DepartmentId` | `uniqueidentifier` | FK to `Departments` |
| `Name` | `nvarchar(150)` | Required |
| `Code` | `nvarchar(50)` | Required |
| `LeadEmployeeId` | `uniqueidentifier` | Nullable FK to `Employees` |
| Audit fields | Shared | Include audit and soft delete fields |

Unique constraint: `DepartmentId`, `Code`.

### 5.4 Designations

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `Name` | `nvarchar(150)` | Unique |
| `Code` | `nvarchar(50)` | Unique |
| `Level` | `int` | Optional hierarchy level |
| Audit fields | Shared | Include audit and soft delete fields |

### 5.5 OfficeLocations

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `Name` | `nvarchar(150)` | Required |
| `Code` | `nvarchar(50)` | Unique |
| `AddressLine1` | `nvarchar(250)` | Nullable |
| `AddressLine2` | `nvarchar(250)` | Nullable |
| `City` | `nvarchar(100)` | Required |
| `State` | `nvarchar(100)` | Nullable |
| `Country` | `nvarchar(100)` | Required |
| `TimeZoneId` | `nvarchar(100)` | Required |
| Audit fields | Shared | Include audit and soft delete fields |

### 5.6 EmployeeDocuments

Stores metadata for files stored in Azure Blob Storage.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `EmployeeId` | `uniqueidentifier` | FK to `Employees` |
| `DocumentType` | `nvarchar(100)` | ProfilePhoto, Identity, OfferLetter, NDA, Appraisal, Other |
| `OriginalFileName` | `nvarchar(255)` | Required |
| `ContentType` | `nvarchar(100)` | Required |
| `FileSizeBytes` | `bigint` | Required |
| `BlobContainer` | `nvarchar(100)` | Required |
| `BlobPath` | `nvarchar(500)` | Required |
| `UploadedAtUtc` | `datetime2` | Required |
| `UploadedBy` | `uniqueidentifier` | FK to `Users` |
| Audit fields | Shared | Include audit and soft delete fields |

## 6. Attendance Tables

### 6.1 Shifts

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `Name` | `nvarchar(150)` | Required |
| `StartTime` | `time` | Required |
| `EndTime` | `time` | Required |
| `GraceMinutes` | `int` | Required |
| `IsNightShift` | `bit` | Required |
| Audit fields | Shared | Include audit and soft delete fields |

### 6.2 EmployeeShifts

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `EmployeeId` | `uniqueidentifier` | FK to `Employees` |
| `ShiftId` | `uniqueidentifier` | FK to `Shifts` |
| `EffectiveFrom` | `date` | Required |
| `EffectiveTo` | `date` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields |

### 6.3 AttendanceRecords

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `EmployeeId` | `uniqueidentifier` | FK to `Employees` |
| `ShiftId` | `uniqueidentifier` | Nullable FK to `Shifts` |
| `AttendanceDate` | `date` | Required |
| `CheckInAtUtc` | `datetime2` | Nullable |
| `CheckOutAtUtc` | `datetime2` | Nullable |
| `Status` | `nvarchar(50)` | Present, Absent, Late, HalfDay, OnLeave, Holiday |
| `IsLateArrival` | `bit` | Required |
| `IsEarlyLeave` | `bit` | Required |
| `TotalWorkMinutes` | `int` | Nullable |
| `Notes` | `nvarchar(500)` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields |

Unique constraint: `EmployeeId`, `AttendanceDate`.

### 6.4 AttendanceCorrections

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `AttendanceRecordId` | `uniqueidentifier` | FK to `AttendanceRecords` |
| `RequestedByEmployeeId` | `uniqueidentifier` | FK to `Employees` |
| `ApprovedByEmployeeId` | `uniqueidentifier` | Nullable FK to `Employees` |
| `RequestedCheckInAtUtc` | `datetime2` | Nullable |
| `RequestedCheckOutAtUtc` | `datetime2` | Nullable |
| `Reason` | `nvarchar(500)` | Required |
| `Status` | `nvarchar(50)` | Pending, Approved, Rejected |
| `DecisionAtUtc` | `datetime2` | Nullable |
| `DecisionComments` | `nvarchar(500)` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields |

## 7. Leave Tables

### 7.1 LeaveTypes

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `Name` | `nvarchar(100)` | Casual Leave, Sick Leave, Earned Leave, Unpaid Leave, Work From Home |
| `Code` | `nvarchar(50)` | Unique |
| `IsPaid` | `bit` | Required |
| `RequiresApproval` | `bit` | Required |
| `AnnualEntitlementDays` | `decimal(5,2)` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields |

### 7.2 LeaveBalances

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `EmployeeId` | `uniqueidentifier` | FK to `Employees` |
| `LeaveTypeId` | `uniqueidentifier` | FK to `LeaveTypes` |
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
| `Id` | `uniqueidentifier` | Primary key |
| `EmployeeId` | `uniqueidentifier` | FK to `Employees` |
| `LeaveTypeId` | `uniqueidentifier` | FK to `LeaveTypes` |
| `ApproverEmployeeId` | `uniqueidentifier` | Nullable FK to `Employees` |
| `StartDate` | `date` | Required |
| `EndDate` | `date` | Required |
| `TotalDays` | `decimal(5,2)` | Required |
| `Reason` | `nvarchar(500)` | Nullable |
| `Status` | `nvarchar(50)` | Pending, Approved, Rejected, Cancelled |
| `DecisionAtUtc` | `datetime2` | Nullable |
| `DecisionComments` | `nvarchar(500)` | Nullable |
| Audit fields | Shared | Include audit and soft delete fields |

### 7.4 Holidays

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `OfficeLocationId` | `uniqueidentifier` | Nullable FK to `OfficeLocations` |
| `Name` | `nvarchar(150)` | Required |
| `HolidayDate` | `date` | Required |
| `IsOptional` | `bit` | Required |
| Audit fields | Shared | Include audit and soft delete fields |

## 8. Audit And Reporting Tables

### 8.1 AuditLogs

Stores immutable audit events for security-sensitive and HR-sensitive operations.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `UserId` | `uniqueidentifier` | Nullable FK to `Users` |
| `EntityName` | `nvarchar(150)` | Required |
| `EntityId` | `uniqueidentifier` | Nullable |
| `Action` | `nvarchar(100)` | Created, Updated, Deleted, Approved, Rejected, LoginFailed |
| `OldValuesJson` | `nvarchar(max)` | Nullable |
| `NewValuesJson` | `nvarchar(max)` | Nullable |
| `IpAddress` | `nvarchar(64)` | Nullable |
| `UserAgent` | `nvarchar(500)` | Nullable |
| `CreatedAtUtc` | `datetime2` | Required |

Audit logs should be append-only. They should not use normal soft delete.

## 9. Notifications And Announcement Tables

### 9.1 Notifications

Stores personal, per-user in-app/email notifications (e.g. leave decisions, attendance alerts). Already implemented in code; documented here to close a gap between the shipped schema and this design doc.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `UserId` | `uniqueidentifier` | Nullable FK to `Users`; recipient |
| `Title` | `nvarchar(250)` | Required |
| `Message` | `nvarchar(2000)` | Required |
| `Channel` | `nvarchar(50)` | `InApp` or `Email` |
| `IsRead` | `bit` | Required |
| `CreatedAtUtc` | `datetime2` | Required |
| `ReadAtUtc` | `datetime2` | Nullable |
| `ExpiresAtUtc` | `datetime2` | Nullable |
| `IsDeleted` | `bit` | Required |
| `DeletedAtUtc` | `datetime2` | Nullable |

> **Implementation note:** this table predates this section and uses a reduced audit set (`IsDeleted`/`CreatedAtUtc`/`DeletedAtUtc` only — no `CreatedBy`/`UpdatedAtUtc`/`UpdatedBy`/`RowVersion`), consistent with §3's allowance for a smaller audit set where appropriate. It does not follow the full Shared Audit Fields table.

### 9.2 Announcements

Stores company-wide broadcast announcements created by Admin/HR, distinct from personal `Notifications`. Default audience is the whole company; an announcement can optionally be scoped to one department or one role.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `Title` | `nvarchar(250)` | Required |
| `Message` | `nvarchar(2000)` | Required |
| `Priority` | `nvarchar(50)` | `Normal`, `Important`, `Critical` |
| `AudienceType` | `nvarchar(50)` | `All`, `Department`, `Role` |
| `DepartmentId` | `uniqueidentifier` | Nullable FK to `Departments`; set when `AudienceType = Department` |
| `TargetRole` | `nvarchar(50)` | Nullable; set when `AudienceType = Role` (matches `Roles.Name`) |
| `CreatedByUserId` | `uniqueidentifier` | FK to `Users`; author |
| `CreatedAtUtc` | `datetime2` | Required |
| `ExpiresAtUtc` | `datetime2` | Nullable |
| `IsDeleted` | `bit` | Required; retracting an announcement soft-deletes it |
| `DeletedAtUtc` | `datetime2` | Nullable |

> **Implementation note:** follows the same reduced audit set as `Notifications` (`IsDeleted`/`CreatedAtUtc`/`DeletedAtUtc` only), for consistency with the sibling table it was built alongside, rather than the full Shared Audit Fields table.

### 9.3 AnnouncementReads

Per-user read receipts for `Announcements`, since a single announcement row is shared across every recipient (unlike `Notifications`, where `IsRead` lives directly on the per-user row).

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `AnnouncementId` | `uniqueidentifier` | FK to `Announcements` |
| `UserId` | `uniqueidentifier` | FK to `Users` |
| `ReadAtUtc` | `datetime2` | Required |

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

Not normally soft-deleted:

- `UserRoles`: remove assignment rows or add effective dating later if role history is required.
- `RefreshTokens`: revoke first, then purge after retention.
- `PasswordResetTokens`: purge after expiry and retention.
- `AuditLogs`: append-only, no soft delete.
- `AnnouncementReads`: append-only read receipts — a row is inserted once per `(AnnouncementId, UserId)` and never updated or soft-deleted.

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

- Payroll: `SalaryStructures`, `Allowances`, `Deductions`, `Payslips`, `Bonuses`, `OvertimeRecords`.
- Tasks: `Tasks`, `TaskAssignments`, `TaskComments`.
- Announcements: `Announcements` and `Notifications` are implemented — see §9. `EmailLogs` remains a future extension point.
- Recruitment: `Candidates`, `Interviews`, `Offers`, `OnboardingChecklists`.
- Assets: `Assets`, `AssetAssignments`, `AssetReturns`.
- Performance: `Goals`, `Kpis`, `PerformanceReviews`, `Promotions`.
- Expenses: `ExpenseClaims`, `ExpenseClaimItems`, `Reimbursements`.
- Messaging: `Conversations`, `Messages`, `MessageParticipants`.

Each future module should follow the same audit, soft delete, indexing, and ownership rules unless there is a clear compliance reason to do otherwise.

