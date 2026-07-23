# Architecture Overview

## Backend Architecture

This project follows a Clean Architecture / Hexagonal-style layering:

- Domain: `backend/EMS.Domain` — core entities and value objects.
- Application: `backend/EMS.Application` — DTOs, MediatR commands/queries/handlers, interfaces.
- Persistence: `backend/EMS.Persistence` — EF Core DbContext, entity configurations, repositories.
- Infrastructure: `backend/EMS.Infrastructure` — implementations for file storage, email, PDF, export.
- API: `backend/EMS.API` — controllers, DI composition, authentication, middleware.

Key patterns & libraries
- CQRS via MediatR for commands and queries.
- EF Core for data access; migrations in `EMS.Persistence`.
- JWT authentication with refresh tokens (see auth services in `EMS.Infrastructure`).
- Local file storage used in development: `LocalFileStorageService`.

Implemented modules (server-side)
- Dashboard: aggregated counts and summaries.
- Document Management: upload/download/delete employee documents (with local storage stub).
- Notifications: persist notifications and send email via local outbox.
- Payroll: salary structures, payroll runs, payslip generation (real PDF via PDFsharp, MIT licensed).
- Reports: repository queries and CSV export.

Where to find code
- Domain entities: `backend/EMS.Domain/Entities`
- Application handlers: `backend/EMS.Application/Features`
- Repositories: `backend/EMS.Persistence/Repositories`
- API controllers: `backend/EMS.API/Controllers`

## Frontend Architecture

The frontend uses a **monorepo structure** supporting both web and mobile platforms:

- **Web**: React 19 + Vite + Tailwind CSS
- **Mobile**: React Native 0.76 + Expo
- **Shared**: Common types, API clients, utilities

Key patterns & libraries
- Shared library (`@ems/shared`) — Types, HTTP client, error classes, utilities
- TypeScript everywhere — Full type safety across platforms
- Zod validation — Runtime schema validation for API responses
- React Hook Form — Form state management with validation
- JWT token management — Automatic token refresh via axios interceptors
- Platform-specific storage — localStorage (web) vs AsyncStorage (mobile)

### Shared Code Library

The `frontend/shared` package contains:
- **Types**: Zod schemas for all entities, API responses, and DTOs
- **HTTP Client**: Axios instance with auth interceptors and token refresh
- **Errors**: Typed error classes (AppError, ValidationError, AuthenticationError, etc.)
- **Hooks**: Utilities for creating fetch, create, update, delete functions
- **Utils**: Date formatting, string utilities, array operations

### Web Application

The `frontend/web` package (React + Vite):
- **Authentication**: Login page with form validation
- **Protected Routes**: ProtectedRoute component for authorization
- **Features**: Dashboard, Employees, Departments, Leave (extensible)
- **Components**: Reusable UI components (Avatar, Modal, StatusBadge)
- **Styling**: Tailwind CSS v4 for responsive design

### Mobile Application

The `frontend/mobile` package (React Native + Expo) is currently an **auth-only skeleton**:
- **Authentication**: Native login screen with form validation, `AuthContext` wired to `@ems/shared`'s HTTP client and `AsyncStorage`
- **Navigation**: Root and tab layout shells (`app/(auth)`, `app/(app)`) are scaffolded
- **Features**: No feature screens yet — Dashboard, Employees, Leave, etc. still need to be built
- **Styling**: React Native StyleSheet with platform-specific optimizations

For detailed frontend architecture, see [docs/ARCHITECTURE_MOBILE.md](ARCHITECTURE_MOBILE.md)
