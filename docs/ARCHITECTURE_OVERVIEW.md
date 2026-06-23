# Architecture Overview

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
- Payroll: salary structures, payroll runs, payslip generation (PDF stub).
- Reports: repository queries and CSV export.

Where to find code
- Domain entities: `backend/EMS.Domain/Entities`
- Application handlers: `backend/EMS.Application/Features`
- Repositories: `backend/EMS.Persistence/Repositories`
- API controllers: `backend/EMS.API/Controllers`
