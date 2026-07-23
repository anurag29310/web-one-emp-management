# WEB-One (EMS)

Lightweight Employee Management System (EMS) for demo and internal use. Implements Clean Architecture with backend APIs, React + React Native frontend, and Docker deployment artifacts.

## Platforms Supported

- **Web** - React 19 + Vite + Tailwind CSS (Modern browsers)
- **Mobile** - React Native 0.76 + Expo (iOS & Android)
- **Shared Code** - Common types, API clients, and utilities

Quick links
- Documentation: [docs/API_DOCUMENTATION.md](docs/API_DOCUMENTATION.md)
- Architecture: [docs/ARCHITECTURE_OVERVIEW.md](docs/ARCHITECTURE_OVERVIEW.md) & [docs/ARCHITECTURE_MOBILE.md](docs/ARCHITECTURE_MOBILE.md)
- Deployment: [docs/DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md)
- Developer setup: [docs/DEVELOPER_SETUP.md](docs/DEVELOPER_SETUP.md)

Repository layout

- `backend/` - .NET 9 solution (Domain, Application, Persistence, Infrastructure, API)
- `frontend/` - Monorepo (Web app, Mobile app, Shared code)
  - `web/` - React web application
  - `mobile/` - Expo React Native application
  - `shared/` - Shared TypeScript library
- `docs/` - project documentation and guides

Quick start (Docker + Full Stack)

1. Copy `.env.example` to `.env` and update values.
2. Start database + services:

```powershell
docker compose up --build
```

3. Backend API will be available at `http://localhost:5000` (see `docker-compose.yml`). 

**Web Frontend** runs at `http://localhost:5173` (Vite dev server):
```powershell
cd frontend
npm install
npm run dev:web
```

**Mobile Frontend** development:
```powershell
cd frontend
npm install
npm run dev:mobile
npm run android    # or ios
```

Notes and recommended production hardening
- Replace local file/email services with production providers (Azure Blob/S3, SendGrid, etc.); payslip PDFs already use PDFsharp (MIT, no licensing cost)
- Secure `Jwt:Key` in a secrets manager and rotate refresh tokens (see security review in the repo).
- Run database migrations before first run: see [docs/DEVELOPER_SETUP.md](docs/DEVELOPER_SETUP.md).

License & contact

This workspace is intended for internal/demo use. For questions, open an issue in the repository.
