# WEB-One (EMS)

Lightweight Employee Management System (EMS) for demo and internal use. Implements Clean Architecture with backend APIs, React frontend, and Docker deployment artifacts.

Quick links
- Documentation: [docs/API_DOCUMENTATION.md](docs/API_DOCUMENTATION.md)
- Architecture: [docs/ARCHITECTURE_OVERVIEW.md](docs/ARCHITECTURE_OVERVIEW.md)
- Deployment: [docs/DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md)
- Developer setup: [docs/DEVELOPER_SETUP.md](docs/DEVELOPER_SETUP.md)

Repository layout

- `backend/` - .NET solution (Domain, Application, Persistence, Infrastructure, API)
- `frontend/` - React SPA (Vite)
- `docs/` - project documentation and guides

Quick start (Docker)

1. Copy `.env.example` to `.env` and update values.
2. Start database + services:

```powershell
docker compose up --build
```

3. Backend API will be available at `http://localhost:5000` (see `docker-compose.yml`). Frontend served at `http://localhost:5173` (Vite dev server) or via nginx proxy depending on compose profile.

Notes and recommended production hardening
- Replace local file/email/pdf services with production providers (Azure Blob/S3, SendGrid, QuestPDF, etc.)
- Secure `Jwt:Key` in a secrets manager and rotate refresh tokens (see security review in the repo).
- Run database migrations before first run: see [docs/DEVELOPER_SETUP.md](docs/DEVELOPER_SETUP.md).

License & contact

This workspace is intended for internal/demo use. For questions, open an issue in the repository.
