# API Documentation

Base URL (development): `http://localhost:5000/api`

Authentication
- The API uses JWT bearer tokens. Obtain a token via `/api/auth/login` with application credentials.
- Send header: `Authorization: Bearer <access_token>`

Common endpoints (examples)

- POST `/api/auth/login`
  - Body: `{ "username": "user@example.com", "password": "P@ssw0rd" }`
  - Response: `{ "accessToken": "...", "refreshToken": "..." }`

- POST `/api/documents/upload`
  - Multipart form: `file` (binary), `employeeId`, `documentType`
  - Auth required. Returns metadata with `id` and `blobPath`.

- GET `/api/documents?employeeId={id}`
  - Returns list of documents for employee.

- GET `/api/documents/{id}/download`
  - Returns file bytes with `Content-Type` and `Content-Disposition`.

- DELETE `/api/documents/{id}`
  - Soft-delete a document.

- POST `/api/payroll/process?month=2026-06&dryRun=false`
  - Triggers payroll processing for the given month (admins only).

- GET `/api/notifications` and POST `/api/notifications`
  - Create and list notifications per user.

- GET `/api/reports/employees` or other report endpoints
  - Returns CSV/JSON aggregates for reporting.

Errors
- API returns standard `4xx`/`5xx` codes with a JSON problem object. Validation errors include `errors` array.

Notes
- Many background/infrastructure pieces are local stubs (file storage, email, PDF). Replace with production providers before deployment.
