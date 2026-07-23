# Developer Setup Guide

Prerequisites
- .NET 9 SDK (or matching version used in `global.json` if present)
- Node.js (>= 18) and npm or pnpm for the React frontend
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

3. Set a JWT signing key. The API fails fast at startup if `Jwt:Key` is missing or shorter than
   32 bytes (256 bits) — there is no built-in default. When running via `docker compose`, this
   comes from `JWT__KEY` in `.env` (see `.env.example`). When running the API directly with
   `dotnet run`, set it as a user secret instead of putting it in `appsettings.json`:

```powershell
dotnet user-secrets init --project backend/EMS.API
dotnet user-secrets set "Jwt:Key" "<a random string of at least 32 bytes>" --project backend/EMS.API
```

4. Run migrations and start the API:

```powershell
dotnet restore
dotnet build
dotnet ef database update --project backend/EMS.Persistence --startup-project backend/EMS.API
dotnet run --project backend/EMS.API
```

Frontend - local run

### Web Application

```powershell
cd frontend/web
npm install
npm run dev
# Vite dev server, opens at http://localhost:5173
```

### Mobile Application

```powershell
cd frontend/mobile
npm install

# Start the Expo dev server
npm run start

# In a separate terminal, launch an emulator or physical device:
npm run android    # Android Emulator or connected device
npm run ios        # iOS Simulator (macOS only)
npm run web        # Web browser preview (http://localhost:8081)
```

### Entire Frontend Workspace

```powershell
cd frontend

# Install all dependencies (monorepo setup)
npm install

# Development
npm run dev:web                # Start web dev server
npm run dev:mobile             # Start Expo dev server

# Build
npm run build:web              # Build web for production
npm run build:mobile           # Build mobile for production

# Validation
npm run lint                   # Lint all packages
npm run type-check             # Type check all packages
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
