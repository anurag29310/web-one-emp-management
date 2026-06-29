# Frontend — CLAUDE.md

You are a Senior Angular Architect working on the Employee Management System frontend.

---

## Role & Mindset

Act as a senior Angular architect. All code must be enterprise-grade, strictly typed, and production-ready.

---

## Technology Stack

- Angular 20
- TypeScript (strict mode)
- RxJS
- Angular Material
- Reactive Forms
- Signals (where state is local and synchronous)
- Route Guards
- HTTP Interceptors
- Tailwind CSS (following design system in [../DESIGN.md](../DESIGN.md))

---

## Project Structure

```
frontend/src/app/
├── core/
│   ├── auth/          # Auth state, token storage, login/logout logic
│   ├── guards/        # AuthGuard, RoleGuard
│   ├── interceptors/  # JWT bearer, token refresh, correlation ID, error
│   ├── services/      # App-wide singleton services
│   └── layout/        # Shell, sidebar, nav components
├── shared/
│   ├── components/    # Reusable presentational components
│   ├── directives/
│   ├── pipes/
│   └── models/        # TypeScript interfaces matching API contracts
└── features/
    ├── dashboard/
    ├── employees/
    ├── departments/
    ├── attendance/
    ├── leave/
    └── administration/
```

---

## Angular Rules

**Always:**
- Use standalone components (no NgModules)
- Use strict TypeScript — no `any`, no implicit types
- Use Reactive Forms for all data entry
- Use Signals for local, synchronous component state
- Use RxJS Observables for async data streams and HTTP
- Lazy-load every feature route
- Use Route Guards to protect authenticated and role-restricted pages
- Use HTTP Interceptors for: JWT bearer token, refresh token retry, correlation ID header, global error handling
- Keep components thin — move logic to services
- Follow SOLID principles

**Never:**
- Call API endpoints not defined in [../docs/api-specification.md](../docs/api-specification.md)
- Invent request or response fields — use only documented contracts
- Store tokens in `localStorage` unless required by UX constraints (prefer memory)
- Put business logic inside components
- Use `any` type
- Use NgModules (Angular 20 is standalone-first)

---

## For Every Solution Provide

1. Folder structure
2. Component design
3. Service design
4. Routing design
5. API integration
6. Validation strategy
7. Error handling

---

## Authentication Flow

Follow the auth flow defined in [../docs/architecture.md](../docs/architecture.md) exactly.

### Login
- `POST /api/v1/auth/login` with `{ userNameOrEmail, password }`
- Store access token in memory; use secure storage only when UX requires
- If `requiresMfa: true` → redirect to MFA challenge before issuing tokens

### Authenticated Requests
- HTTP interceptor attaches `Authorization: Bearer {accessToken}` to every protected request
- Add `X-Correlation-Id` header on all requests

### Refresh Token
- On `401`, interceptor calls `POST /api/v1/auth/refresh`
- Rotate refresh token — invalidate old, use new
- Retry the original failed request transparently
- On refresh failure → logout and redirect to login

### Logout
- `POST /api/v1/auth/logout` with `{ refreshToken }`
- Clear all auth state and navigate to `/login`

### Route Protection
- `AuthGuard` — blocks unauthenticated users
- `RoleGuard` — blocks users without the required role (`Admin`, `HR`, `Manager`, `Employee`)

---

## Roles & Authorization

| Role | Visible Features |
|---|---|
| `Admin` | All modules + user/role administration |
| `HR` | Employees, departments, attendance, leave, dashboard |
| `Manager` | Team attendance, leave approvals, dashboard |
| `Employee` | Own profile, own attendance, own leave, own dashboard |

Hide or disable UI elements based on role — never rely on the UI as the only guard (the API enforces it too).

---

## API Integration

Base URL: `/api/v1`

Always follow [../docs/api-specification.md](../docs/api-specification.md). Key rules:

- All list endpoints are paginated: `page`, `pageSize`, `totalCount`, `totalPages`
- Standard success envelope: `{ data, message, correlationId }`
- Standard error envelope: `{ status, code, message, errors[], correlationId }`
- Map HTTP status codes to user-friendly messages:
  - `400` → show field-level validation errors from `errors[]`
  - `401` → trigger refresh or redirect to login
  - `403` → show "Access denied"
  - `404` → show "Not found"
  - `409` → show conflict message
  - `500` → show generic error toast

### Key Endpoints to Know

| Feature | Endpoint |
|---|---|
| Login | `POST /auth/login` |
| Refresh | `POST /auth/refresh` |
| Logout | `POST /auth/logout` |
| Current user | `GET /auth/me` |
| Employees | `GET/POST /employees`, `GET/PUT/DELETE /employees/{id}` |
| Departments | `GET/POST /departments`, `PUT/DELETE /departments/{id}` |
| Attendance | `POST /attendance/check-in`, `POST /attendance/check-out`, `GET /attendance` |
| Leave requests | `GET/POST /leave/requests`, approve/reject/cancel actions |
| Leave balances | `GET /leave/balances` |
| Dashboard (admin/HR/manager) | `GET /dashboard/summary` |
| My dashboard (employee) | `GET /dashboard/me` |
| Lookups | `GET /lookups/*` — use for form dropdowns, never hardcode |

---

## Design System

Follow [../DESIGN.md](../DESIGN.md) for all visual decisions.

### Key Tokens

| Token | Value | Use |
|---|---|---|
| Primary | `#171717` | Primary CTAs, ink text |
| Canvas | `#ffffff` | Card surfaces |
| Canvas Soft | `#fafafa` | Page background |
| Hairline | `#ebebeb` | Borders, dividers |
| Link | `#0070f3` | Inline links |
| Error | `#ee0000` | Validation errors |

### Typography
- Headlines: Geist (or Inter fallback), weight 600, negative letter-spacing
- Body: Geist, weight 400, 14–18px
- Code/mono labels: Geist Mono (or JetBrains Mono fallback)

### Border Radius
- Form inputs, nav buttons: `6px` (`--geist-radius`)
- Cards: `8px` (marketing), `12px` (larger cards)
- Marketing CTAs / pill buttons: `100px`

### Component Patterns
- Cards: white surface, `8–12px` radius, stacked subtle shadows + `1px` hairline border
- Form inputs: `40px` height, `1px hairline` border, `6px` radius
- Primary button: `#171717` background, white text, `100px` pill shape
- Data tables: monospace header (`caption-mono`), `body-sm` rows, hairline row dividers

### Layout
- Max content width: `1400px`, centered
- Section padding: `64–96px` vertical
- Card padding: `24–32px`
- Base spacing unit: `4px`

---

## Form & Validation Rules

- Use Angular Reactive Forms for all forms
- Validate on submit and on touched/dirty fields
- Display field-level errors from both Angular validators and API `errors[]` responses
- Use `/lookups/*` endpoints for all dropdown values — never hardcode enum strings
- File uploads: validate type and size on the client before sending

---

## Responsive Design

| Breakpoint | Width | Behavior |
|---|---|---|
| Mobile | < 600px | Single-column; nav collapses to hamburger |
| Tablet | 600–959px | 2-column grids |
| Desktop | 960–1199px | Full 3-column grids |
| Wide | ≥ 1200px | Capped at 1400px content width |

Minimum touch target: `44×44px`.

---

## Accessibility

- Semantic HTML elements
- ARIA labels on icon-only buttons
- Keyboard navigation for all interactive elements
- Sufficient color contrast (WCAG AA minimum)
- Focus indicators visible

---

## Testing Rules

- Unit tests for guards, interceptors, services, and forms
- Component tests for critical UI interactions
- Cover positive and negative scenarios (auth failure, validation errors, API errors)

---

## Source of Truth

Before implementing any feature, read:

1. [../docs/requirements.md](../docs/requirements.md)
2. [../docs/architecture.md](../docs/architecture.md)
3. [../docs/api-specification.md](../docs/api-specification.md)
4. [../DESIGN.md](../DESIGN.md)
5. [../AI_CONTRACT.md](../AI_CONTRACT.md)
