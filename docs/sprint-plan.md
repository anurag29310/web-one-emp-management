# Employee Management System Sprint Plan

## 1. Sprint Overview

Sprint length: 2 weeks

Sprint goal:

Build the MVP foundation for the Employee Management System and deliver the first usable vertical slice: authenticated users can log in, access role-protected routes, manage core organization lookup data, and create, view, update, and soft delete employees.

Primary priority:

1. Technical foundation
2. Authentication and role-based access
3. Organization master data
4. Employee management
5. Basic attendance, leave, and dashboard scaffolding

Design decision:

The first sprint should not try to complete every MVP module end to end. Authentication, database foundation, audit fields, soft delete, and employee management are prerequisites for attendance, leave, dashboards, reporting, and future modules.

## 2. Sprint Assumptions

- Team size: 1 backend developer, 1 frontend developer, 1 QA/devops engineer, and product/architecture support.
- Workdays: 10 business days.
- Backend: .NET 9 Web API using Clean Architecture.
- Frontend: Angular 20 standalone components with Reactive Forms.
- Database: SQL Server with EF Core migrations.
- Authentication: JWT access tokens and refresh tokens.
- Deployment target: local Docker-ready setup first, Azure deployment preparation only.
- MVP quality rules from `AI_CONTRACT.md` apply from the start.

## 3. Sprint Scope

### In Scope

- Repository and solution structure.
- Backend Clean Architecture projects.
- Angular application shell.
- SQL Server database foundation.
- Shared audit fields and soft delete support.
- Authentication APIs for login, refresh, logout, forgot password, reset password, and current user.
- Role model for Admin, HR, Manager, and Employee.
- Route guards and HTTP interceptor.
- Department, team, designation, and office location APIs.
- Employee CRUD APIs and Angular screens.
- Employee status management.
- Basic profile photo/document metadata design hooks.
- Attendance, leave, and dashboard scaffolding endpoints or placeholder service structure.
- Unit tests for critical authentication, validation, and employee use cases.

### Out Of Scope For This Sprint

- Full attendance correction workflow.
- Full leave approval workflow.
- Full dashboard analytics.
- MFA implementation beyond API-ready design.
- Production Azure deployment.
- Excel/PDF export implementation.
- Payroll, recruitment, assets, performance, expenses, messaging, and announcements.
- Biometric, QR attendance, Slack, Teams, or ERP integrations.

## 4. Prioritized Sprint Backlog

| Priority | Story | Owner | Estimate | Outcome |
| --- | --- | --- | --- | --- |
| P0 | Create backend Clean Architecture solution structure | Backend | 1 day | API, Application, Domain, Infrastructure, Tests projects created |
| P0 | Create Angular 20 application shell | Frontend | 1 day | App routing, layout, core/shared/features folders created |
| P0 | Configure database foundation | Backend | 1 day | EF Core DbContext, migrations, audit fields, soft delete filters |
| P0 | Implement authentication and refresh tokens | Backend | 2 days | Login, refresh, logout, forgot/reset password APIs |
| P0 | Implement frontend auth flow | Frontend | 2 days | Login page, auth state, route guards, HTTP interceptor |
| P0 | Seed roles and first admin user | Backend | 0.5 day | Admin, HR, Manager, Employee roles available |
| P1 | Implement organization master APIs | Backend | 1.5 days | Departments, teams, designations, office locations CRUD |
| P1 | Implement organization UI screens | Frontend | 1.5 days | List/create/edit screens for master data |
| P1 | Implement employee domain, APIs, and validation | Backend | 2 days | Employee list/detail/create/update/delete/status APIs |
| P1 | Implement employee UI screens | Frontend | 2 days | Employee list, detail, create/edit forms |
| P1 | Add authorization policies | Backend | 1 day | Role/policy checks for auth, org, and employee APIs |
| P1 | Add global exception handling and logging | Backend | 0.5 day | Standard error response and Serilog setup |
| P2 | Add basic dashboard summary API | Backend | 0.5 day | Counts for total, active, inactive employees |
| P2 | Add basic dashboard UI | Frontend | 0.5 day | MVP summary cards |
| P2 | Scaffold attendance module | Backend/Frontend | 1 day | Check-in/check-out service/API shape started |
| P2 | Scaffold leave module | Backend/Frontend | 1 day | Leave type/balance/request service/API shape started |
| P2 | Add smoke tests and API tests | QA/Devops | 1 day | Authentication and employee happy-path coverage |
| P2 | Add Docker/local run documentation | QA/Devops | 0.5 day | Local setup documented |

## 5. Day-By-Day Plan

### Day 1: Project Foundation

Backend:

- Create solution under `src/server`.
- Add `EmployeeManagement.Api`, `Application`, `Domain`, `Infrastructure`, and `Tests`.
- Configure project references according to Clean Architecture.
- Add baseline packages for EF Core, SQL Server, FluentValidation, Serilog, JWT authentication, and testing.

Frontend:

- Create Angular 20 app under `src/client/employee-management-web`.
- Add core, shared, and feature folder structure.
- Configure app routes, layout shell, environment files, and base API service.

QA/Devops:

- Define local run checklist.
- Confirm expected environment variables.

### Day 2: Database And Cross-Cutting Backend

Backend:

- Add shared entities: `BaseEntity`, `AuditableEntity`, `SoftDeletableEntity`.
- Add `ApplicationDbContext`.
- Add audit field population in `SaveChangesAsync`.
- Add EF Core global query filters for soft delete.
- Add initial migration for identity, role, organization, and employee tables.
- Add global exception middleware and standard error response.

Frontend:

- Add HTTP interceptor shell.
- Add shared API response and paged result models.
- Add base form and validation helpers.

QA/Devops:

- Verify database can be created locally.
- Add first migration verification notes.

### Day 3: Authentication Backend

Backend:

- Implement users, roles, user roles, refresh tokens, and password reset token persistence.
- Implement password hashing.
- Implement JWT access token generation.
- Implement refresh token hashing, storage, rotation, and revocation.
- Add APIs:
  - `POST /api/v1/auth/login`
  - `POST /api/v1/auth/refresh`
  - `POST /api/v1/auth/logout`
  - `POST /api/v1/auth/forgot-password`
  - `POST /api/v1/auth/reset-password`
  - `GET /api/v1/auth/me`

Tests:

- Add unit tests for login validation.
- Add unit tests for refresh token rotation.

### Day 4: Authentication Frontend And Authorization

Frontend:

- Build login page with Reactive Forms.
- Add auth state service.
- Add token refresh handling in HTTP interceptor.
- Add route guards.
- Add logout flow.

Backend:

- Seed `Admin`, `HR`, `Manager`, and `Employee` roles.
- Seed first admin user for local development.
- Add authorization policies:
  - `CanManageEmployees`
  - `CanViewEmployeeDetails`
  - `CanManageDepartments`
  - `CanViewDashboard`
  - `CanManageUsers`

QA:

- Test login, logout, token refresh, and route protection.

### Day 5: Organization Master Data

Backend:

- Implement entities, repositories, validators, commands/queries, and controllers for:
  - Departments
  - Teams
  - Designations
  - Office locations
- Add soft delete behavior and filtered unique indexes.

Frontend:

- Build organization list and edit screens.
- Add lookup services for departments, teams, designations, and locations.

Tests:

- Add validation tests for duplicate codes and required names.

### Day 6: Employee Backend

Backend:

- Implement employee entity mapping and repository methods.
- Implement employee DTOs, validators, commands, and queries.
- Add APIs:
  - `GET /api/v1/employees`
  - `GET /api/v1/employees/{id}`
  - `POST /api/v1/employees`
  - `PUT /api/v1/employees/{id}`
  - `PATCH /api/v1/employees/{id}/profile`
  - `PATCH /api/v1/employees/{id}/status`
  - `DELETE /api/v1/employees/{id}`
  - `GET /api/v1/employees/{id}/direct-reports`
  - `GET /api/v1/employees/{id}/reporting-hierarchy`

Tests:

- Add unit tests for employee create/update validation.
- Add authorization tests for employee detail access.

### Day 7: Employee Frontend

Frontend:

- Build employee list with pagination, search, and filters.
- Build employee detail page.
- Build create/edit employee form.
- Add department, team, designation, manager, and location dropdowns.
- Add employee status update action.
- Add basic delete confirmation flow.

Backend:

- Fix API gaps found during frontend integration.

QA:

- Start employee CRUD test pass.

### Day 8: Documents, Dashboard, Attendance And Leave Scaffolding

Backend:

- Add employee document metadata endpoints or service interfaces for future Blob Storage implementation.
- Add basic dashboard summary endpoint:
  - Total employees
  - Active employees
  - Inactive employees
  - Department summary
- Scaffold attendance entities/services and basic check-in/check-out API contracts.
- Scaffold leave type, leave balance, and leave request service structure.

Frontend:

- Add dashboard page with employee and department summary.
- Add placeholder navigation entries for attendance and leave.
- Add basic attendance and leave service clients.

Tests:

- Add dashboard query tests.

### Day 9: Hardening And Integration

Backend:

- Review async/await usage.
- Review exception handling.
- Review soft delete behavior.
- Review audit field behavior.
- Add missing XML comments on public API contracts where required.
- Add API smoke tests for auth, organization, and employees.

Frontend:

- Review form validation messages.
- Review route guard behavior by role.
- Review responsive layout on common desktop and mobile widths.
- Fix integration issues.

QA/Devops:

- Run full local smoke test.
- Verify clean setup from database migration.

### Day 10: Stabilization, Demo, And Sprint Close

All:

- Fix critical and high-priority bugs.
- Verify acceptance criteria.
- Update README or setup notes if needed.
- Prepare sprint demo script.
- Document known gaps and next sprint candidates.

Demo flow:

1. Admin logs in.
2. Admin creates department, designation, and office location.
3. HR creates an employee.
4. HR views and updates employee details.
5. Employee logs in and views own profile.
6. Manager views direct reports.
7. Dashboard shows employee summary.

## 6. Acceptance Criteria

### Authentication

- Users can log in with valid credentials.
- Invalid login attempts return a standard error response.
- JWT access tokens are accepted by protected APIs.
- Refresh tokens are rotated and old refresh tokens are revoked.
- Logout revokes the active refresh token.
- Forgot/reset password APIs exist and validate requests.
- Angular blocks protected routes when the user is not authenticated.

### Authorization

- Admin can access all MVP management screens.
- HR can manage employees and organization data.
- Manager can view team/direct report data.
- Employee can view and update only allowed self-service profile fields.
- Unauthorized API requests return `401`.
- Forbidden API requests return `403`.

### Organization

- Admin/HR can create, update, list, and soft delete departments.
- Admin/HR can create, update, list, and soft delete teams.
- Admin/HR can create, update, list, and soft delete designations.
- Admin/HR can create, update, list, and soft delete office locations.
- Duplicate active codes are rejected.

### Employees

- Admin/HR can create an employee.
- Admin/HR can update employee details.
- Admin/HR can update employee status.
- Admin/HR can soft delete an employee.
- Employee list supports pagination, search, and core filters.
- Employee detail includes department, designation, manager, location, and status.
- Audit fields are populated on create, update, and soft delete.

### Dashboard

- Dashboard shows total employees.
- Dashboard shows active and inactive employee counts.
- Dashboard shows department summary.
- Dashboard is protected by role-based access.

### Quality

- Backend follows Clean Architecture project dependencies.
- EF Core migrations can create the database from scratch.
- No secrets are hardcoded.
- Unit tests cover critical validators and use cases.
- API smoke tests cover login and employee CRUD happy path.
- Serilog request logging and centralized exception handling are configured.

## 7. Definition Of Done

A story is done when:

- Code is implemented according to `AI_CONTRACT.md`.
- Validation is implemented with FluentValidation where applicable.
- APIs return standard success and error responses.
- Async I/O uses async/await.
- Database changes are covered by EF Core migration.
- Audit fields and soft delete behavior are respected.
- Unit tests or smoke tests cover the critical path.
- Angular forms use Reactive Forms.
- Protected Angular routes use route guards.
- API calls use HTTP interceptors.
- Work is locally runnable and documented.

## 8. Risks And Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Sprint scope is too broad | MVP foundation may be unstable | Keep attendance, leave, exports, and MFA as scaffolding only |
| Auth implementation takes longer than planned | Blocks frontend and protected APIs | Build auth first and keep MFA deferred |
| Employee form complexity grows | Delays employee CRUD | Start with required fields and add document/photo polish later |
| Database relationships create migration issues | Slows integration | Build schema incrementally and validate migrations daily |
| Role rules are unclear | Rework in frontend and backend | Use policy names from architecture and API spec |
| File upload storage needs Azure setup | Blocks documents | Store metadata/service interface now, Blob implementation later |

## 9. Next Sprint Candidates

Priority candidates for Sprint 2:

- Complete attendance check-in/check-out and attendance history.
- Complete attendance correction workflow.
- Complete leave request, approval, rejection, balance tracking, and holiday calendar.
- Implement employee document upload/download with Azure Blob Storage.
- Expand dashboard with attendance and leave metrics.
- Add Excel export for employees, attendance, and leave.
- Add more integration tests and architecture tests.
- Add Azure deployment pipeline and infrastructure as code.

