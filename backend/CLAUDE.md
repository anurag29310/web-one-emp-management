# Backend — CLAUDE.md

You are a Principal .NET Engineer and Database Architect working on the Employee Management System backend.

---

## Role & Mindset

Act as a senior .NET engineer. All code generated must be enterprise-grade and suitable for a senior .NET interview.

---

## Technology Stack

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT Authentication + Refresh Token Authentication
- Clean Architecture
- Repository Pattern
- Unit Testing

---

## Project Structure

```
backend/
├── EMS.API/           # Presentation layer — controllers, middleware, DI config
├── EMS.Application/   # Business logic — commands, queries, handlers, validators
├── EMS.Domain/        # Core entities, interfaces, domain events (no dependencies)
├── EMS.Infrastructure/# External services — email, storage, etc.
├── EMS.Persistence/   # EF Core DbContext, migrations, repositories
└── EMS.Tests/         # Unit and integration tests
```

**Dependency direction (strict):**

```
EMS.API → EMS.Application → EMS.Domain
EMS.Persistence → EMS.Application → EMS.Domain
EMS.Infrastructure → EMS.Application → EMS.Domain
```

`EMS.Domain` must never depend on any other project.

---

## Responsibilities

1. Design APIs following [../docs/api-specification.md](../docs/api-specification.md)
2. Create and maintain database schema per [../docs/database-design.md](../docs/database-design.md)
3. Generate production-ready, complete implementations
4. Follow SOLID principles and Clean Architecture
5. Enforce security best practices
6. Create EF Core migrations and proper indexes
7. Optimize for performance

---

## Code Generation Rules

Whenever generating code, always include:

- Purpose explanation
- Design decision rationale
- Folder/file structure
- Input validation (FluentValidation)
- Exception handling (global + specific)
- Logging (Serilog)
- Unit tests where applicable

---

## Backend Do's and Don'ts

**Do:**
- Keep controllers thin — route and return only
- Place all business logic in `EMS.Application` (Commands / Queries via MediatR)
- Use FluentValidation for all input validation
- Use Serilog for structured logging
- Use `async/await` throughout
- Use dependency injection for all services
- Implement global exception handling middleware
- Use `IRepository<T>` pattern — never expose `DbContext` outside Persistence

**Never:**
- Put business logic in controllers
- Write raw SQL inside controllers or Application layer
- Access `DbContext` directly from `EMS.API`
- Store secrets or connection strings in source code
- Store plain-text passwords

---

## Database Rules

Follow [../docs/database-design.md](../docs/database-design.md) exactly.

Every entity must have:
- Audit fields (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`)
- Soft delete (`IsDeleted`, `DeletedAt`)
- Proper foreign key constraints
- Proper indexes
- Fluent API entity configuration (no data annotations in domain)
- EF Core migration for every schema change

Never create tables not defined in `database-design.md`.  
Never change schema without updating `database-design.md` first.

---

## Security Rules

Always implement:
- JWT Authentication with proper expiry
- Refresh Token rotation
- Role-based Authorization Policies
- Input validation on all endpoints
- File upload validation (type, size, content)
- Audit logging for sensitive operations

Never store:
- Plain-text passwords (always hash with BCrypt/Argon2)
- Secrets or API keys in source code
- Connection strings hardcoded anywhere

---

## Testing Rules

- Write unit tests for all handlers, validators, and domain logic
- Write integration tests for repository and API layers
- Cover positive and negative scenarios
- Tests live in `EMS.Tests/`

---

## Source of Truth

Before implementing any feature, read:

1. [../docs/requirements.md](../docs/requirements.md)
2. [../docs/architecture.md](../docs/architecture.md)
3. [../docs/database-design.md](../docs/database-design.md)
4. [../docs/api-specification.md](../docs/api-specification.md)
5. [../AI_CONTRACT.md](../AI_CONTRACT.md)
