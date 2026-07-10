# Employee Management System - Master Agent

## Project Context

This project is a production-ready Employee Management System built using:

* React 18+ (Vite, TypeScript, React Router)
* .NET 9
* SQL Server
* Docker
* Clean Architecture
* CQRS
* MediatR
* FluentValidation
* JWT Authentication
* Refresh Tokens
* Serilog
* Hostinger VPS Deployment

---

## Source Of Truth

Before implementing any feature, ALWAYS read and follow:

1. docs/requirements.md
2. docs/architecture.md
3. docs/database-design.md
4. docs/api-specification.md
5. docs/sprint-plan.md
6. AI_CONTRACT.md
7. DESIGN.md

These documents are authoritative.

Never ignore them.

---

## Architecture Rules

Follow:

* Clean Architecture
* SOLID Principles
* CQRS Pattern
* Repository Pattern
* Dependency Injection

Dependencies:

EMS.API
↓
EMS.Application
↓
EMS.Domain

EMS.Persistence
↓
EMS.Application
↓
EMS.Domain

EMS.Infrastructure
↓
EMS.Application
↓
EMS.Domain

EMS.Domain must never depend on any other project.

---

## Backend Rules

Technology:

* .NET 9
* ASP.NET Core
* Entity Framework Core
* SQL Server

Requirements:

* Controllers must remain thin
* Business logic belongs in Application layer
* Use Commands and Queries
* Use FluentValidation
* Use Serilog
* Use async/await
* Use dependency injection
* Use global exception handling

Never:

* Put business logic in controllers
* Put SQL inside controllers
* Access DbContext directly from API layer

---

## Database Rules

Follow database-design.md.

Requirements:

* Audit Fields
* Soft Delete
* Proper Foreign Keys
* Proper Indexes
* Entity Configurations
* Migrations

Never create tables not defined in database-design.md.

Never change schema without updating database-design.md.

---

## Frontend Rules

Always use:

* tailwind-4-docs
* web-design-guidelines

Follow:

* DESIGN.md
* architecture.md
* api-specification.md

Requirements:

* React 18+ (Vite, TypeScript)
* Functional Components with Hooks
* React Hook Form + Zod
* Protected Route Components (React Router)
* Axios Interceptors
* Lazy Loading
* Responsive Design
* Accessibility

Never:

* Create APIs from assumptions
* Invent backend fields
* Ignore api-specification.md

---

## API Rules

Follow api-specification.md.

Never:

* Rename endpoints
* Modify request contracts
* Modify response contracts

without updating api-specification.md first.

---

## Security Rules

Always implement:

* JWT Authentication
* Refresh Tokens
* Authorization Policies
* Input Validation
* File Validation
* Audit Logging

Never store:

* Plain-text passwords
* Secrets in source code
* Connection strings in code

---

## Testing Rules

Always generate:

* Unit Tests
* Integration Tests

When applicable:

* Security Tests
* API Tests

Test positive and negative scenarios.

---

## Division Of Testing Responsibility

* Claude/the agent implements the feature and writes the automated tests required above.
* Claude/the agent does NOT manually drive the running app in a browser (starting dev servers, clicking through flows, taking screenshots) to verify UI behavior.
* The developer runs the app and performs manual/exploratory testing themselves.

Automated tests are still required for every feature — this only removes manual verification from the agent's workflow.

---

## Documentation Rules

When creating new functionality:

Update:

* requirements.md (if requirement changes)
* architecture.md (if architecture changes)
* database-design.md (if schema changes)
* api-specification.md (if API changes)

Do not allow code and documentation to diverge.

---

## Implementation Workflow

Before any implementation:

1. Read project documents
2. Verify requirements
3. Verify architecture
4. Verify database design
5. Verify API contracts
6. Implement feature
7. Add tests
8. Update documentation if required

---

## Output Expectations

Generate:

* Production-ready code
* Complete implementations
* Error handling
* Validation
* Logging
* Tests

Avoid placeholders, mock implementations, and incomplete code.

Always prioritize maintainability, scalability, security, and performance.
