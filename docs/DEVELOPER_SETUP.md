# Developer Setup Guide

Prerequisites
- .NET 9 SDK (or matching version used in `global.json` if present)
- Node.js (>= 16) and npm or pnpm for the Angular frontend
- Docker & Docker Compose (for local DB and full stack)

Clone

```powershell
git clone <repo-url>
cd WEB-One
```

Backend - local run

1. Copy `.env.example` to `.env` and set values for local dev (e.g., connection string to local Docker Postgres).
2. Start Postgres (option A: docker compose):

```powershell
docker compose up -d db
```

3. Run migrations and start the API:

```powershell
dotnet restore
dotnet build
dotnet ef database update --project backend/EMS.Persistence --startup-project backend/EMS.API
dotnet run --project backend/EMS.API
```

Frontend - local run

```powershell
cd frontend
npm install
npm start
# or: ng serve --open
```

Running tests

```powershell
dotnet test
```

Linting & checks
- Backend: use `dotnet format` / analyzers as configured.
- Frontend: use `npm run lint` if configured in `package.json`.

Notes
- Many infrastructure services (file storage, email, PDF) are stubbed for development. To test integrations, replace with appropriate service implementations and update DI in `Program.cs`.
- If you plan to run the full stack locally, use `docker compose up --build` at the repository root.
