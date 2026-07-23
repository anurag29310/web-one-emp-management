# EMS Web App

React 18 + Vite + TypeScript frontend for the Employee Management System.

## Prerequisites

- Node.js 20+
- npm

## Setup

```bash
npm install
cp .env.example .env
```

`.env` controls the app's data source — see [Data source (Mock vs API)](#data-source-mock-vs-api) below.

## Run

```bash
npm run dev
```

Opens at [http://localhost:5173](http://localhost:5173). By default `VITE_DATA_SOURCE=mock`, so the app runs fully self-contained against predefined dummy data — no backend required. Sign in with any of the seeded mock accounts:

| Username / email | Password | Role |
| --- | --- | --- |
| `admin` / `admin@ems.local` | `Admin@123` | Admin |
| `hr.user` / `hr@ems.local` | `Hr@12345` | HR |
| `manager` / `manager@ems.local` | `Manager@123` | Manager |
| `employee` / `employee@ems.local` | `Employee@123` | Employee |

## Other scripts

```bash
npm run build      # type-checks (tsc -b) then builds for production
npm run typecheck   # type-check only, no build output
npm run lint        # ESLint
npm run preview     # preview the production build locally
```

## Data source (Mock vs API)

The whole app reads data through repository interfaces (see `src/app/features/*/api`). Which implementation backs them is controlled by a single environment variable — no UI or component code changes when you switch it:

```bash
# .env
VITE_DATA_SOURCE=mock   # dummy JSON, no network calls
VITE_DATA_SOURCE=api    # calls the real EMS backend

VITE_API_BASE_URL=https://localhost:7284/api/v1   # only used when VITE_DATA_SOURCE=api
```

To run against the real backend: start the API in `backend/EMS.API` (see the root `README.md`/`docs/DEVELOPER_SETUP.md`), set `VITE_DATA_SOURCE=api` and `VITE_API_BASE_URL` to match its launch profile, then `npm run dev`.

## Project structure

```text
src/app/
  core/       # config, Axios client + auth interceptors, auth context, routing, layout
  shared/     # domain models, cross-feature utils/components
  features/   # one folder per module (auth, dashboard, employees, departments, attendance, leave, administration)
```

Each implemented feature (`auth`, `dashboard`, `employees`, `departments`, `leave`) follows the same pattern under `api/`: a `*Repository` interface, a `mock*Repository`, an `api*Repository`, and an `index.ts` factory that picks between them based on `VITE_DATA_SOURCE`. `attendance` and `administration` are interface-only stubs — the backend has no controllers for those yet.

Note on `leave`: the backend has no leave-type listing endpoint, only a bare `leaveTypeId` guid on the request DTO. `features/leave/api/leaveTypes.ts` holds a small display-only lookup for known mock IDs (used for a friendlier hint in the apply form); it doesn't change how the form behaves in API mode — you still enter/paste the raw guid.
