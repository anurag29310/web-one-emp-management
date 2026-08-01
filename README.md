# EMS — Employee Management System

A production-grade, multi-tenant Employee Management SaaS for small and mid-sized
organizations. One deployment serves many companies, each fully isolated to its own data, with a
Super Admin tier above every tenant for platform-wide onboarding, monitoring, and support.

Built with **.NET 9** (Clean Architecture / CQRS) on the backend and **React 19 + TypeScript**
on the frontend, sharing a common domain model with a companion **React Native (Expo)** mobile
app.

> **Status:** Active development. Core HR, attendance, leave, payroll, task, and multi-tenant
> platform modules are implemented; several Phase 3 modules are in progress. See
> [Future Roadmap](#future-roadmap) below and [docs/requirements.md](docs/requirements.md) for
> the full phased plan.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [Architecture Summary](#architecture-summary)
- [Folder Structure](#folder-structure)
- [Local Setup](#local-setup)
- [Docker Setup](#docker-setup)
- [Screenshots](#screenshots)
- [Future Roadmap](#future-roadmap)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

EMS covers the day-to-day HR lifecycle — employees, attendance, leave, payroll, tasks, client
management, and internal communication — behind JWT-secured, role-based access control, on top
of a multi-tenant data model that keeps every company's records isolated.

A platform-level **Super Admin** portal sits above all tenants to register companies, approve or
suspend them, monitor cross-company metrics, and audit platform activity, while each company's
own Admins, HR, Managers, and Employees only ever see their own organization's data.

The system follows Clean Architecture end to end: a framework-free domain core, CQRS-based
application logic via MediatR, EF Core/PostgreSQL persistence, and a thin API layer — designed so
business rules stay testable and independent of any particular framework or cloud provider.

## Features

### Platform / Multi-Tenancy

- Super Admin dashboard — cross-company counts, active/suspended/trial breakdown, recent
  registrations
- Company management — create, update, soft-delete, restore, search, and filter companies
- Self-service, public company registration (togglable), with optional approval gating
- Activate / suspend a company — instantly revokes all refresh tokens and blocks in-flight
  access tokens for every user in that company
- Force-logout a company's users independent of suspension
- Cross-company and per-company audit log views
- Platform settings (registration on/off, approval requirement)

### Core HR

- JWT authentication with refresh token rotation, forgot/reset password, and TOTP-based MFA
  (with recovery codes)
- Role-based access control — Admin, HR, Manager, Employee
- Employee lifecycle — create, update, soft-delete, profile photo, documents, emergency contact,
  address
- Departments, Teams, Designations, Office Locations, and reporting hierarchy
- Attendance — check-in/out, GPS capture, office geofencing, manual corrections, shift
  attendance, history
- Leave management — apply/approve/reject, configurable leave types, balance tracking, holiday
  calendar
- Dashboard metrics — employee counts, attendance and leave summaries, department breakdown

### Payroll & Expenses

- Salary structure, allowances, deductions, bonus, overtime, and payslip generation (PDF)
- Employee reimbursement claims — draft/submit workflow, multi-file attachments, mileage-based
  claims, approval workflow, automatic inclusion in the next payroll run

### Work & Client Management

- Task management — assign, reassign, track progress, priority/status workflow, photo/notes
  updates, linked client and GPS details
- Client Master — full CRM-lite record (contact info, GST, geolocation) shared by tasks

### Collaboration

- Company announcements and in-app/email notifications
- Internal messaging between employees and managers
- Recruitment (candidate tracking), asset allocation, and performance review modules

### Reporting & Export

- Excel and PDF export for employee, attendance, leave, and payroll data
- Centralized audit logging across auditable entities (create/update/delete, approvals, status
  changes)

### Cross-Cutting

- FluentValidation on every command/query
- Global exception handling with consistent HTTP error mapping
- Serilog structured logging
- Rate limiting on authentication endpoints
- Responsive, accessible UI (WCAG-conscious components, keyboard navigation)

## Technology Stack

**Backend**

- [.NET 9](https://dotnet.microsoft.com/) / ASP.NET Core Web API
- Clean Architecture (Domain / Application / Persistence / Infrastructure / API)
- CQRS via [MediatR](https://github.com/jbogard/MediatR)
- [FluentValidation](https://fluentvalidation.net/)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/) (Npgsql provider)
- [Serilog](https://serilog.net/) structured logging
- JWT Bearer authentication + refresh tokens, TOTP MFA
- [PDFsharp](https://www.pdfsharp.net/) (MIT) for payslip/report PDF generation

**Frontend (Web)**

- [React 19](https://react.dev/) + [Vite](https://vitejs.dev/) + TypeScript
- [React Router](https://reactrouter.com/) with protected routes
- [React Hook Form](https://react-hook-form.com/) + [Zod](https://zod.dev/) for forms/validation
- [Tailwind CSS 4](https://tailwindcss.com/)
- [Axios](https://axios-http.com/) with request/response interceptors (token attach + refresh)
- [Vitest](https://vitest.dev/) + React Testing Library for unit/component tests

**Mobile**

- [React Native](https://reactnative.dev/) + [Expo](https://expo.dev/)
- Shared TypeScript library (`frontend/shared`) for types, API client, and utilities used by
  both web and mobile

**Database & Infra**

- [PostgreSQL 15](https://www.postgresql.org/)
- [Docker](https://www.docker.com/) / Docker Compose (dev and production topologies)
- Nginx reverse proxy for production
- Hostinger VPS deployment target (Azure-compatible design)

## Architecture Summary

The backend follows strict Clean Architecture with a one-way dependency rule:

```
EMS.API  ─────────┐
EMS.Persistence ───┼──▶ EMS.Application ──▶ EMS.Domain
EMS.Infrastructure ┘
```

- **EMS.Domain** — entities, enums, value objects, domain events. No framework dependencies.
- **EMS.Application** — CQRS commands/queries (MediatR), DTOs, FluentValidation validators, and
  the interfaces (`IEmployeeRepository`, `IUnitOfWork`, `ITokenService`, …) that outer layers
  implement.
- **EMS.Persistence** — `ApplicationDbContext`, EF Core entity configurations, repositories, and
  migrations for PostgreSQL.
- **EMS.Infrastructure** — JWT/token services, file storage, email, PDF generation, and other
  external integrations.
- **EMS.API** — thin controllers, middleware (global exception handling, request logging),
  Swagger, and DI wiring only — no business logic.

Every business entity carries full audit fields (created/updated/deleted by + timestamp) and
soft delete, enforced via EF Core global query filters. Multi-tenancy is enforced at the
`Company` boundary for the core HR entities (Users, Employees, Departments, etc.); see
[docs/requirements.md](docs/requirements.md#multi-tenancy--super-admin-portal) for what's
tenant-scoped today versus planned.

The frontend mirrors this with a feature-based structure (`core` for auth/routing/API client,
`shared` for reusable components, `features/*` for one folder per business module), and talks to
the API exclusively through a typed Axios client defined against
[docs/api-specification.md](docs/api-specification.md).

For full detail, see [docs/architecture.md](docs/architecture.md),
[docs/ARCHITECTURE_OVERVIEW.md](docs/ARCHITECTURE_OVERVIEW.md), and
[docs/ARCHITECTURE_MOBILE.md](docs/ARCHITECTURE_MOBILE.md).

## Folder Structure

```
web-one-emp-management/
├── backend/
│   ├── EMS.API/                 # Controllers, middleware, Program.cs — thin HTTP layer
│   ├── EMS.Application/         # CQRS commands/queries, DTOs, validators, interfaces
│   ├── EMS.Domain/              # Entities, enums, value objects (no dependencies)
│   ├── EMS.Persistence/         # DbContext, EF configurations, repositories, migrations
│   ├── EMS.Infrastructure/      # Auth/token services, storage, email, PDF, logging
│   └── EMS.Tests/               # Unit, integration, and architecture tests
│
├── frontend/
│   ├── web/                     # React 19 + Vite web application
│   │   └── src/app/
│   │       ├── core/            # Auth, routing, Axios client, layout
│   │       ├── shared/          # Reusable components, hooks, utils, models
│   │       └── features/        # One folder per module (employees, attendance, payroll, …)
│   ├── mobile/                  # React Native (Expo) application
│   └── shared/                  # Shared TypeScript types/API client used by web + mobile
│
├── docs/                        # Requirements, architecture, DB design, API spec, guides
├── deploy/                      # Nginx config and deployment artifacts
├── docker-compose.yml           # Local full-stack development
├── docker-compose.prod.yml      # Production topology
├── AI_CONTRACT.md               # Ground rules for AI-assisted development on this repo
└── DESIGN.md                    # UI/UX design system reference
```

## Local Setup

**Prerequisites**

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 18+
- [Docker](https://www.docker.com/) (for PostgreSQL, or the full stack)

**1. Clone and configure**

```powershell
git clone <repo-url>
cd web-one-emp-management
copy .env.example .env
# edit .env with local DB credentials and a JWT signing key
```

**2. Start PostgreSQL**

```powershell
docker compose up -d db
```

**3. Set the JWT signing key** (must be ≥ 32 bytes / 256 bits — the API fails fast at startup
otherwise)

```powershell
dotnet user-secrets init --project backend/EMS.API
dotnet user-secrets set "Jwt:Key" "<a random string of at least 32 bytes>" --project backend/EMS.API
```

**4. Run the backend** (EF Core migrations apply automatically on startup)

```powershell
dotnet restore
dotnet build
dotnet run --project backend/EMS.API
# API available at http://localhost:5000, Swagger at /swagger
```

**5. Run the frontend**

```powershell
cd frontend/web
npm install
npm run dev
# Web app at http://localhost:5173
```

**Mobile (optional)**

```powershell
cd frontend/mobile
npm install
npm run start
npm run android   # or: npm run ios / npm run web
```

**Running tests**

```powershell
dotnet test                    # backend: unit, integration, architecture tests
cd frontend/web && npm run test   # frontend: Vitest + React Testing Library
```

Full details, including monorepo-wide scripts, are in
[docs/DEVELOPER_SETUP.md](docs/DEVELOPER_SETUP.md).

## Docker Setup

Run the entire stack (PostgreSQL + API + web frontend) with a single command:

```powershell
copy .env.example .env
# edit .env — set POSTGRES_* credentials and JWT__KEY
docker compose up --build
```

| Service  | URL                     |
|----------|-------------------------|
| Frontend | http://localhost        |
| API      | http://localhost:5000   |
| Postgres | localhost:5432          |

For production, use the dedicated compose file, which puts the frontend and API behind a single
Nginx origin:

```powershell
docker compose -f docker-compose.prod.yml build
docker compose -f docker-compose.prod.yml up -d
```

See [docs/DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md) for TLS/reverse-proxy configuration,
production hardening notes (blob storage, transactional email provider, secrets management), and
rollback guidance.

## Screenshots

> Screenshots coming soon. Once the UI stabilizes, this section will include:
>
> - Login & MFA enrollment
> - Admin dashboard
> - Employee directory & profile
> - Attendance check-in/out with geofencing
> - Leave request & approval flow
> - Payroll & payslip generation
> - Super Admin platform dashboard
>
> *(Contributions welcome — see [Contributing](#contributing) if you'd like to help populate this
> section.)*

## Future Roadmap

Tracked in full in [docs/requirements.md](docs/requirements.md) and
[docs/sprint-plan.md](docs/sprint-plan.md). Highlights:

**Near-term**

- Subscription management (plans, billing, trial expiry enforcement)
- Per-company feature flags / module entitlements
- Tenant-scoping for remaining business-process entities (attendance, leave, payroll, tasks,
  reimbursements, recruitment, assets, performance, messaging, notifications, announcements,
  clients)
- Rate limiting on refresh-token and forgot-password endpoints

**Later**

- Recruitment & onboarding — interview scheduling, offer generation, joining checklists
- Asset management — allocation and return tracking
- Performance management — goals, KPIs, promotions
- Dark mode and multi-language support
- QR / biometric attendance
- Slack / Microsoft Teams integrations
- ERP / payroll sync
- OCR receipt scanning and expense policy validation
- Digital approval workflows with electronic signatures
- Offline-capable mobile app

## Contributing

Contributions are welcome. Before opening a PR:

1. Read [docs/requirements.md](docs/requirements.md), [docs/architecture.md](docs/architecture.md),
   [docs/database-design.md](docs/database-design.md), and
   [docs/api-specification.md](docs/api-specification.md) — these are the source of truth for
   this project and take precedence over ad-hoc changes.
2. Follow the Clean Architecture dependency rules described above.
3. Add unit/integration tests for new functionality (backend: xUnit; frontend: Vitest).
4. Update the relevant doc under `docs/` if you change a requirement, schema, or API contract.

See [AI_CONTRACT.md](AI_CONTRACT.md) for the ground rules this repo applies to AI-assisted
changes, and [DESIGN.md](DESIGN.md) for the UI/UX design system.

## License

No license has been published for this repository yet. If you intend to open source this
project, add a `LICENSE` file (e.g., MIT or Apache-2.0) before accepting external contributions
or distribution.
