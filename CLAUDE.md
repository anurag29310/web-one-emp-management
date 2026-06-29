# Employee Management System — CLAUDE.md

## Project Overview

Production-ready Employee Management System built with:

- Angular 20
- .NET 9
- SQL Server
- Docker
- Clean Architecture / CQRS / MediatR
- FluentValidation
- JWT Authentication + Refresh Tokens
- Serilog
- Hostinger VPS Deployment

---

## Source of Truth

Before implementing any feature, read these documents in order:

1. [docs/requirements.md](docs/requirements.md)
2. [docs/architecture.md](docs/architecture.md)
3. [docs/database-design.md](docs/database-design.md)
4. [docs/api-specification.md](docs/api-specification.md)
5. [docs/sprint-plan.md](docs/sprint-plan.md)
6. [AI_CONTRACT.md](AI_CONTRACT.md)
7. [DESIGN.md](DESIGN.md)

These documents are authoritative. Never ignore them.

---

## Architecture

Follow Clean Architecture, SOLID, CQRS, Repository Pattern, and Dependency Injection.

**Dependency direction (strict):**

```
EMS.API → EMS.Application → EMS.Domain
EMS.Persistence → EMS.Application → EMS.Domain
EMS.Infrastructure → EMS.Application → EMS.Domain
```

`EMS.Domain` must never depend on any other project.

---

## Backend Rules (.NET 9 / ASP.NET Core)

**Do:**
- Keep controllers thin — no business logic
- Place all business logic in the Application layer
- Use Commands and Queries (CQRS)
- Use FluentValidation for all input validation
- Use Serilog for logging
- Use `async/await` throughout
- Use dependency injection
- Implement global exception handling
- Access data only through the Persistence layer

**Never:**
- Put business logic in controllers
- Write SQL inside controllers
- Access `DbContext` directly from the API layer

---

## Database Rules

Follow [docs/database-design.md](docs/database-design.md) exactly.

**Required on all entities:**
- Audit fields
- Soft delete
- Proper foreign keys and indexes
- Entity configurations (Fluent API)
- EF Core migrations

Never create tables not defined in `database-design.md`.  
Never change schema without updating `database-design.md` first.

---

## Frontend Rules (Angular 20)

**Always use:**
- `angular-developer` agent
- `tailwind-4-docs`
- `web-design-guidelines`

**Follow:** [DESIGN.md](DESIGN.md), [docs/architecture.md](docs/architecture.md), [docs/api-specification.md](docs/api-specification.md)

**Requirements:**
- Standalone components
- Signals where appropriate
- Reactive Forms
- Route Guards
- HTTP Interceptors
- Lazy loading
- Responsive design
- Accessibility (a11y)

**Never:**
- Create API calls based on assumptions
- Invent backend fields
- Ignore `api-specification.md`

---

## API Rules

Follow [docs/api-specification.md](docs/api-specification.md).

Never rename endpoints, modify request contracts, or modify response contracts without updating `api-specification.md` first.

---

## Security Rules

**Always implement:**
- JWT Authentication
- Refresh Tokens
- Authorization Policies
- Input Validation
- File Validation
- Audit Logging

**Never store:**
- Plain-text passwords
- Secrets in source code
- Connection strings hardcoded in code

---

## Testing Rules

Always generate unit tests and integration tests. Add security and API tests where applicable. Cover both positive and negative scenarios.

---

## Documentation Rules

When adding or changing functionality, update the relevant docs:

| What changed | Update |
|---|---|
| Requirement | `requirements.md` |
| Architecture | `architecture.md` |
| Database schema | `database-design.md` |
| API contract | `api-specification.md` |

Code and documentation must never diverge.

---

## Implementation Workflow

For every feature:

1. Read all project documents
2. Verify requirements
3. Verify architecture
4. Verify database design
5. Verify API contracts
6. Implement the feature
7. Write tests
8. Update affected documentation

---

## Output Standards

All code must be:

- Production-ready and complete
- Error-handled and validated
- Logged with Serilog
- Covered by tests

No placeholders, mocks, or incomplete implementations. Prioritize maintainability, scalability, security, and performance.
