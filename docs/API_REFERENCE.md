# EMS API Reference

Complete, code-derived reference for every REST endpoint exposed by `EMS.API`. Unlike [`api-specification.md`](api-specification.md) (the design-stage contract), this document is generated directly from the current controllers, MediatR commands/queries, FluentValidation validators, and the global exception-handling middleware — it reflects actual runtime behavior, including inconsistencies and gaps found in the code as of this writing.

**How to read this doc**: each endpoint lists Purpose, URL, Method, Headers, Authentication, Request, Response, Validation, Error Codes, Examples, and Best Practices. Where the code diverges from `api-specification.md`, or where a real inconsistency/gap was found (missing validators, inconsistent response envelopes, status-code mismatches, missing authorization checks), it is called out explicitly inline rather than silently smoothed over — treat those notes as engineering follow-ups, not documentation choices.

## Conventions used throughout

- **Base URL**: most controllers are versioned under `/api/v1`; a few (`EmployeeDocumentsController`, `NotificationsController`, `AnnouncementsController`, `HealthController`) are not — this is called out per section.
- **Auth header**: `Authorization: Bearer <accessToken>` unless the endpoint is explicitly marked public.
- **Success envelope**: most endpoints wrap responses as `ApiResponse<T>` (`{ "data": ..., "message": "..." }`); some controllers (Notifications, Announcements, Employee Documents) return bare JSON instead — flagged per section.
- **Error envelope**: `ApiErrorResponse` (`{ "status": 400, "code": "VALIDATION_ERROR", "message": "...", "errors": [...] }`) for most 4xx/5xx; a number of `Get-by-id` actions return a bare `NotFound()` with no body instead — flagged per endpoint.
- **Global exception mapping** (`ExceptionHandlingMiddleware`), used throughout: `ValidationException` → `400`; `InvalidOperationException` with a "not found" message → `404`; other `InvalidOperationException` → `409`; `UnauthorizedAccessException` → `403` (not `401` — see the Auth section for why this matters).

---

# EMS API Reference — Authentication, Users, Roles

> Derived from the actual controller/handler/validator code in `backend/EMS.API/Controllers/{Auth,Users,Roles}Controller.cs` and `backend/EMS.Application/Features/{Auth,Users,Roles}/**`, cross-checked against `docs/api-specification.md`. Where code and doc disagree, this reference follows the code and calls out the discrepancy.

## Global notes (apply to every endpoint below)

**Envelope.** All success responses are wrapped by `EMS.API.Controllers.ApiResponse<T>`:

```json
{ "data": { }, "message": "Request completed successfully.", "correlationId": "f9b2f37a1e7b4f6" }
```

All error responses are wrapped by `ApiErrorResponse` (built by `EMS.API/Middleware/ExceptionHandlingMiddleware.cs`):

```json
{ "status": 400, "code": "VALIDATION_ERROR", "message": "...", "errors": [{ "propertyName": "Email", "errorMessage": "..." }], "correlationId": "f9b2f37a1e7b4f6" }
```

**Exception → HTTP status mapping (actual code, `ExceptionHandlingMiddleware.HandleAsync`):**

| Thrown exception | Status | Code |
| --- | --- | --- |
| `FluentValidation.ValidationException` | 400 | `VALIDATION_ERROR` |
| `InvalidOperationException` whose message contains "not found" | 404 | `NOT_FOUND` |
| `InvalidOperationException` (any other message) | 409 | `CONFLICT` |
| `UnauthorizedAccessException` | **403** | `FORBIDDEN` |
| anything else | 500 | `INTERNAL_ERROR` |

**Important discrepancy to flag:** several controller actions carry `[ProducesResponseType(typeof(ApiErrorResponse), 401)]` annotations (e.g. `POST /auth/login`, `POST /auth/refresh`, `POST /auth/change-password`), and `docs/api-specification.md` §2.5 also documents `401` for "missing or invalid authentication". But the handlers behind these actions throw plain `UnauthorizedAccessException` for business failures (bad credentials, expired/reused refresh token, wrong current password, etc.), and the middleware above converts **every** `UnauthorizedAccessException` to **403 Forbidden**, not 401. So in the running system:
- **401** only ever comes from the ASP.NET Core JWT authentication middleware itself — i.e. no token, a malformed token, or an expired token on an `[Authorize]` endpoint (never reaches the controller/handler).
- **403** is what a client actually receives for "bad credentials", "invalid/expired/reused refresh token", "incorrect current/account password", and (per ASP.NET Core's default) authorization-policy failures (e.g. a non-Admin calling a `CanManageUsers` endpoint).

This reference documents the annotated/spec status codes per endpoint but notes this 401-vs-403 reality inline wherever it applies. Treat the controller's `[ProducesResponseType(..., 401)]` attributes as aspirational/stale until the handlers are changed to throw a dedicated 401 exception type.

**No global model-validation short-circuit found** — validators run via FluentValidation (presumably through a MediatR pipeline behavior, not visible in the three controllers themselves); when a validator fails, `ValidationException` surfaces and is mapped to 400 as above.

---

## Table of Contents

**Identity & Access**
- [Authentication API](#authentication-api)
- [Users API](#users-api)
- [Roles API](#roles-api)

**Organization & HR Core**
- [Employees API](#employees-api)
- [Employee Documents API](#employee-documents-api)
- [Departments API](#departments-api)
- [Designations API](#designations-api)
- [Office Locations API](#office-locations-api)
- [Teams API](#teams-api)

**Attendance, Leave & Scheduling**
- [Attendance API](#attendance-api)
- [Leave API](#leave-api)
- [Leave Types API](#leave-types-api)
- [Shifts API](#shifts-api)
- [Holidays API](#holidays-api)

**Payroll, Reimbursements & Assets**
- [Payroll API](#payroll-api)
- [Reimbursements API](#reimbursements-api)
- [Assets API](#assets-api)

**Recruitment, Performance & Tasks**
- [Candidates API](#candidates-api)
- [Performance API](#performance-api)
- [Tasks API](#tasks-api)

**Communication**
- [Messaging API](#messaging-api)
- [Notifications API](#notifications-api)
- [Announcements API](#announcements-api)

**Multi-Tenant / Platform Admin**
- [Clients API](#clients-api)
- [Company Registration API](#company-registration-api)
- [Platform Audit Logs API](#platform-audit-logs-api)
- [Platform Companies API](#platform-companies-api)
- [Platform Dashboard API](#platform-dashboard-api)
- [Platform Settings API](#platform-settings-api)

**Dashboard, Reporting & Ops**
- [Dashboard API](#dashboard-api)
- [Reports API](#reports-api)
- [Exports API](#exports-api)
- [Audit Logs API](#audit-logs-api)
- [Health API](#health-api)

> Each module also carries inline "Cross-cutting notes" / uncertainty callouts near its section — these are real findings from reading the code (missing validators, inconsistent envelopes, auth gaps), not documentation artifacts. Search the file for **"flagging"**, **"gap"**, or **"discrepancy"** to jump to all of them.

---

## Authentication API

Controller: `backend/EMS.API/Controllers/AuthController.cs`
Route prefix: `[Route("api/v1/auth")]` → all paths below are `/api/v1/auth/...`.
No controller-level `[Authorize]` — each action opts in individually.

### POST /api/v1/auth/login

**Purpose** — Authenticate with username-or-email + password. Returns access/refresh tokens directly for non-MFA accounts, or an MFA challenge for accounts with MFA enabled (client must then call `POST /auth/mfa/verify`).

**URL** — `/api/v1/auth/login`

**Method** — POST

**Headers** — `Content-Type: application/json`. No `Authorization` header (public).

**Authentication** — Public (`[AllowAnonymous]`). Rate-limited via `[EnableRateLimiting("LoginPolicy")]`.

**Request**
```json
{
  "userNameOrEmail": "jane.doe",
  "password": "P@ssw0rd!"
}
```
Both fields required (see Validation). Maps to `LoginCommand` (`EMS.Application.Features.Auth.LoginCommand`) — only these two fields exist on the command; no other fields are accepted.

**Response** — `200 OK`, `ApiResponse<LoginResult>`:

Non-MFA account:
```json
{
  "data": {
    "accessToken": "eyJhbGciOi...",
    "refreshToken": "base64guid+guidhex...",
    "expiresInSeconds": 900,
    "requiresMfa": false,
    "mfaChallengeId": null
  },
  "message": "Request completed successfully.",
  "correlationId": "f9b2f37a1e7b4f6"
}
```
MFA-enabled account (tokens withheld):
```json
{
  "data": {
    "accessToken": null,
    "refreshToken": null,
    "expiresInSeconds": 0,
    "requiresMfa": true,
    "mfaChallengeId": "3e1f...-...-..."
  }
}
```
`LoginResult` fields: `accessToken` (string?), `refreshToken` (string?), `expiresInSeconds` (int), `requiresMfa` (bool), `mfaChallengeId` (Guid?). `expiresInSeconds` is hardcoded to `900` (15 min) in `LoginCommandHandler`, matching the 15-minute access-token lifetime hardcoded in `JwtTokenService` (`DateTime.UtcNow.AddMinutes(15)`).

**Validation** — `LoginCommandValidator` (`Features/Auth/Validators/LoginCommandValidator.cs`): `UserNameOrEmail` NotEmpty; `Password` NotEmpty. No format/length rules — length/complexity is only enforced at password-set time, not at login.

**Error Codes**
- `400 VALIDATION_ERROR` — empty username/email or password.
- **`403 FORBIDDEN`** (annotated as 401 in code/spec, actually 403 — see Global notes) — invalid credentials, disabled account (`IsActive == false`), or the user's company is not `Active`/`Trial` (`LoginCommandHandler` throws `UnauthorizedAccessException` for all three, with distinct messages but identical status).
- `429 RATE_LIMIT_EXCEEDED` — more than 5 requests/60s from the same IP (`LoginPolicy`, configurable via `RateLimiting:Login:PermitLimit` / `WindowSeconds`); response includes a `Retry-After` header.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userNameOrEmail":"jane.doe","password":"P@ssw0rd!"}'
```
```json
{"data":{"accessToken":"eyJ...","refreshToken":"AbCd...","expiresInSeconds":900,"requiresMfa":false,"mfaChallengeId":null},"message":"Request completed successfully.","correlationId":"a1b2c3d4e5f6a7b8"}
```

**Best Practices**
- Store the access token in memory only; store the refresh token in an httpOnly cookie or secure storage — never localStorage for either in a production-grade client.
- Treat any 403 from this endpoint as "retry with corrected credentials", not "re-authenticate" — don't auto-retry.
- When `requiresMfa: true`, immediately prompt for a TOTP/recovery code and call `POST /auth/mfa/verify` with the returned `mfaChallengeId`; the challenge expires after 5 minutes server-side.

---

### POST /api/v1/auth/mfa/verify

**Purpose** — Complete a login that was paused for MFA. Exchanges the `mfaChallengeId` from `POST /auth/login` plus a TOTP code (or an unused recovery code) for access/refresh tokens.

**URL** — `/api/v1/auth/mfa/verify`

**Method** — POST

**Headers** — `Content-Type: application/json`. No `Authorization` header (public).

**Authentication** — Public (`[AllowAnonymous]`). Rate-limited via `[EnableRateLimiting("MfaVerifyPolicy")]`.

**Request**
```json
{
  "mfaChallengeId": "3e1f2a4b-....-....-....-............",
  "code": "123456"
}
```
`VerifyMfaCommand`: `MfaChallengeId` (Guid, required), `Code` (string, required — either a 6-digit TOTP code or one of the account's unused recovery codes).

**Response** — `200 OK`, `ApiResponse<LoginResult>` — same shape as the non-MFA branch of `POST /auth/login` (tokens populated, `requiresMfa: false`).

**Validation** — **No FluentValidation validator found** for `VerifyMfaCommand` (not present under `Features/Auth/Validators/`). Field presence/shape is not explicitly validated before the handler runs; an empty/missing `code` simply fails TOTP/recovery-code verification and returns the generic 403 below.

**Error Codes**
- **`403 FORBIDDEN`** (annotated 401) — challenge id unknown, already consumed, expired (>5 min old), or the code does not match the TOTP secret or any unused recovery code. `VerifyMfaCommandHandler` deliberately returns one generic message ("Invalid or expired verification code.") for all of these to avoid leaking which condition failed.
- `429 RATE_LIMIT_EXCEEDED` — more than 10 requests/60s per IP (`MfaVerifyPolicy`), tightened specifically because a TOTP code is only 6 digits and valid for ~30–90s (brute force is realistic within that window). `Retry-After` header included.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/auth/mfa/verify \
  -H "Content-Type: application/json" \
  -d '{"mfaChallengeId":"3e1f2a4b-0000-0000-0000-000000000000","code":"123456"}'
```

**Best Practices**
- Do not let the client cache or reuse the `mfaChallengeId` past a single verify attempt sequence — it is single-use once consumed and time-boxed to 5 minutes.
- Offer the recovery-code path as a fallback input in the same field as the TOTP code (the API accepts either in `code`).

---

### POST /api/v1/auth/refresh

**Purpose** — Exchange a valid refresh token for a new access token and a rotated (new) refresh token. Used by the axios interceptor / silent-refresh flow when the access token expires.

**URL** — `/api/v1/auth/refresh`

**Method** — POST

**Headers** — `Content-Type: application/json`. No `Authorization` header required (public — the refresh token itself is the credential).

**Authentication** — Public (`[AllowAnonymous]`). Not rate-limited (no `[EnableRateLimiting]` attribute on this action).

**Request**
```json
{ "refreshToken": "AbCd...guidhex..." }
```
`RefreshTokenCommand`: `RefreshToken` (string, required) — the only field.

**Response** — `200 OK`, `ApiResponse<RefreshTokenResult>`:
```json
{
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "NewTokenValue...",
    "expiresAtUtc": "2026-08-31T12:00:00Z"
  }
}
```
`RefreshTokenResult`: `accessToken` (string), `refreshToken` (string — the new rotated token), `expiresAtUtc` (DateTime — the **refresh** token's expiry, 30 days from issue per `RefreshTokenService`, not the access token's). Note this differs in shape from `LoginResult` (no `expiresInSeconds`).

**Validation** — `RefreshTokenCommandValidator`: `RefreshToken` NotEmpty.

**Error Codes**
- `400 VALIDATION_ERROR` — empty refresh token.
- **`403 FORBIDDEN`** (annotated 401) — token not found, token already revoked (triggers reuse detection: **all** refresh tokens for that user are revoked immediately as a compromise response), or token expired.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"AbCd...guidhex..."}'
```

**Best Practices**
- Always replace the stored refresh token with the one returned — the old one is revoked server-side the instant this call succeeds (rotation), so reusing it will trip reuse detection and log the user out of every session.
- On a 403 from this endpoint, do not retry — force a full re-login; the family may have been revoked.

---

### POST /api/v1/auth/logout

**Purpose** — Revoke a single refresh token (log out the current session/device only).

**URL** — `/api/v1/auth/logout`

**Method** — POST

**Headers** — `Content-Type: application/json`, `Authorization: Bearer {accessToken}`.

**Authentication** — `[Authorize]` (any authenticated user, no specific role/policy).

**Request**
```json
{ "refreshToken": "AbCd...guidhex..." }
```
`LogoutCommand`: `RefreshToken` (string, required-by-convention, but see Validation).

**Response** — `204 No Content`.

**Validation** — **No FluentValidation validator found** for `LogoutCommand`. The handler is idempotent by design: unknown or already-revoked tokens are silently accepted and return 204 rather than erroring (`LogoutCommandHandler.Handle`: `if (token == null || token.IsRevoked) return;`).

**Error Codes**
- `401 Unauthorized` — missing/invalid access token (from the `[Authorize]` gate itself, before the handler runs — this one genuinely is 401, not routed through the exception middleware).
- No 400/403/404 from the handler itself; it never throws for a bad/missing refresh token.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/auth/logout \
  -H "Authorization: Bearer eyJ..." -H "Content-Type: application/json" \
  -d '{"refreshToken":"AbCd...guidhex..."}'
```

**Best Practices**
- Call this on explicit user sign-out and discard both tokens client-side immediately, regardless of response — the call is fire-and-forget safe since it never errors on a bad token.

---

### POST /api/v1/auth/logout-all

**Purpose** — Revoke every active refresh token for the current user (sign out of all devices/sessions).

**URL** — `/api/v1/auth/logout-all`

**Method** — POST

**Headers** — `Authorization: Bearer {accessToken}`. No request body.

**Authentication** — `[Authorize]`. User id is taken from the `sub`/`NameIdentifier` claim on the access token, not from the request.

**Request** — No body. No path/query params.

**Response** — `204 No Content`.

**Validation** — N/A (no request body).

**Error Codes**
- `401 Unauthorized` — missing/invalid access token, or the `sub`/`NameIdentifier` claim is missing/unparseable as a `Guid` (`GetCurrentUserId()` throws `UnauthorizedAccessException("User identity could not be resolved.")` — note this one *is* thrown as `UnauthorizedAccessException` inside the controller, so per the mapping table it would actually surface as **403**, not 401, despite the name).

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/auth/logout-all \
  -H "Authorization: Bearer eyJ..."
```

**Best Practices**
- Use this for "sign out everywhere" / "I think my account is compromised" flows, and pair it with a forced password change (`change-password`) for the latter case.

---

### POST /api/v1/auth/forgot-password

**Purpose** — Request a password-reset email. Always responds success to prevent user enumeration.

**URL** — `/api/v1/auth/forgot-password`

**Method** — POST

**Headers** — `Content-Type: application/json`. No `Authorization` header (public).

**Authentication** — Public (`[AllowAnonymous]`). No rate limit attribute found on this action (unlike login/mfa-verify).

**Request**
```json
{ "email": "jane.doe@example.com" }
```
`ForgotPasswordCommand`: `Email` (string, required).

**Response** — `204 No Content` — always, whether or not the email matches an active account (`ForgotPasswordCommandHandler` returns early/silently for unknown or inactive users).

**Validation** — `ForgotPasswordCommandValidator`: `Email` NotEmpty + `EmailAddress()` format check.

**Error Codes**
- `400 VALIDATION_ERROR` — empty or malformed email.
- No 401/403/404 — by design, this endpoint never reveals whether the email exists.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/auth/forgot-password \
  -H "Content-Type: application/json" -d '{"email":"jane.doe@example.com"}'
```

**Best Practices**
- Show the same "if that email exists, we've sent a reset link" message client-side regardless of response, reinforcing the server's anti-enumeration behavior.
- Consider client-side throttling of repeat submits since the endpoint itself is not rate-limited in code today.

---

### POST /api/v1/auth/reset-password

**Purpose** — Complete a password reset using the token emailed by `forgot-password`.

**URL** — `/api/v1/auth/reset-password`

**Method** — POST

**Headers** — `Content-Type: application/json`. No `Authorization` header (public).

**Authentication** — Public (`[AllowAnonymous]`).

**Request**
```json
{
  "email": "jane.doe@example.com",
  "resetToken": "a1b2c3...",
  "newPassword": "N3wP@ssword!"
}
```
`ResetPasswordCommand`: `Email`, `ResetToken`, `NewPassword` — all required strings.

**Response** — `204 No Content`. Side effects: password updated, **all** refresh tokens for the user revoked (forces re-login everywhere), reset token invalidated.

**Validation** — `ResetPasswordCommandValidator`:
- `Email`: NotEmpty, `EmailAddress()`.
- `ResetToken`: NotEmpty.
- `NewPassword`: NotEmpty, `MinimumLength(8)`, must match `[A-Z]`, `[a-z]`, `[0-9]`, and `[^a-zA-Z0-9]` (i.e., at least one upper, one lower, one digit, one special character).

**Error Codes**
- `400 VALIDATION_ERROR` — weak password, missing fields, malformed email.
- `409 CONFLICT` — `ResetPasswordCommandHandler` throws `InvalidOperationException("Password reset token is invalid or has expired.")` when the token doesn't validate, or when the email doesn't match the token's owner. Per the mapping table this is 409, not 400/404, since the message doesn't contain "not found".

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{"email":"jane.doe@example.com","resetToken":"a1b2c3...","newPassword":"N3wP@ssword!"}'
```

**Best Practices**
- After a successful reset, redirect straight to login — every existing session (including the one that may still be open in another tab) is invalidated server-side.
- Treat the 409 here as terminal for that token — send the user back to `forgot-password` rather than retrying.

---

### POST /api/v1/auth/change-password

**Purpose** — Change password for the currently authenticated user (requires knowing the current password).

**URL** — `/api/v1/auth/change-password`

**Method** — POST

**Headers** — `Content-Type: application/json`, `Authorization: Bearer {accessToken}`.

**Authentication** — `[Authorize]`. `UserId` is taken from the access token, not the request body (the controller overwrites `cmd.UserId` with `GetCurrentUserId()`).

**Request**
```json
{
  "currentPassword": "OldP@ss1",
  "newPassword": "N3wP@ssword!"
}
```
`ChangePasswordCommand`: `UserId` (Guid — set server-side, ignore/omit from client payload), `CurrentPassword` (required), `NewPassword` (required).

**Response** — `204 No Content`. Note: unlike `reset-password`, this does **not** revoke other sessions.

**Validation** — `ChangePasswordCommandValidator`:
- `CurrentPassword`: NotEmpty.
- `NewPassword`: NotEmpty, `MinimumLength(8)`, must contain upper/lower/digit/special character (same regex set as reset-password), and `NotEqual(CurrentPassword)` — new password must differ from current.

**Error Codes**
- `400 VALIDATION_ERROR` — weak/missing new password, or new password equals current password.
- **`403 FORBIDDEN`** (annotated 401) — `CurrentPassword` does not match (`UnauthorizedAccessException("Current password is incorrect.")`).
- `409 CONFLICT` — would occur if `UserId` resolves to no user (`InvalidOperationException("User not found.")` — message contains no "not found" case-sensitively... actually it does contain "not found", so this is **404**, not 409). In practice unreachable via this endpoint since `UserId` always comes from a valid access token.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/auth/change-password \
  -H "Authorization: Bearer eyJ..." -H "Content-Type: application/json" \
  -d '{"currentPassword":"OldP@ss1","newPassword":"N3wP@ssword!"}'
```

**Best Practices**
- Force re-entry of the current password in the UI immediately before submit (don't cache it) since it's the sole authorization check beyond the bearer token.
- Consider prompting the user to also hit `logout-all` afterward if they suspect compromise — this endpoint alone leaves other sessions alive.

---

### POST /api/v1/auth/mfa/setup

**Purpose** — Begin MFA enrollment: generates a new TOTP secret for the current user and returns it as a manual-entry key plus an `otpauth://` URI for client-side QR rendering. MFA is **not** yet enabled — must be confirmed via `mfa/enable`.

**URL** — `/api/v1/auth/mfa/setup`

**Method** — POST

**Headers** — `Authorization: Bearer {accessToken}`. No request body.

**Authentication** — `[Authorize]`.

**Request** — No body.

**Response** — `200 OK`, `ApiResponse<MfaSetupResult>`:
```json
{
  "data": {
    "manualEntryKey": "JBSWY3DPEHPK3PXP",
    "otpAuthUri": "otpauth://totp/EMS:jane.doe@example.com?secret=JBSWY3DPEHPK3PXP&issuer=EMS"
  }
}
```

**Validation** — N/A (no request body; `UserId` comes from the token).

**Error Codes**
- `409 CONFLICT` — MFA already enabled for this account (`InvalidOperationException("MFA is already enabled for this account.")`).
- Calling this repeatedly before `mfa/enable` is safe/idempotent — each call overwrites the previously generated (unconfirmed) secret.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/auth/mfa/setup -H "Authorization: Bearer eyJ..."
```

**Best Practices**
- Render `otpAuthUri` as a QR code client-side; also show `manualEntryKey` as text for users who need to type it into their authenticator app.
- Don't persist the secret client-side beyond the enrollment screen — it's only needed to render the QR/manual key once.

---

### POST /api/v1/auth/mfa/enable

**Purpose** — Confirm MFA enrollment with a code from the authenticator app. Turns MFA on and returns 10 one-time recovery codes, shown only in this response.

**URL** — `/api/v1/auth/mfa/enable`

**Method** — POST

**Headers** — `Authorization: Bearer {accessToken}`, `Content-Type: application/json`.

**Authentication** — `[Authorize]`. `UserId` set server-side from the token.

**Request**
```json
{ "code": "123456" }
```
`EnableMfaCommand`: `UserId` (set server-side), `Code` (required string).

**Response** — `200 OK`, `ApiResponse<EnableMfaResult>`, with a custom message:
```json
{
  "data": { "recoveryCodes": ["ABCD-1234-EFGH", "..." /* 10 total */] },
  "message": "MFA enabled. Store these recovery codes securely — they will not be shown again."
}
```

**Validation** — **No FluentValidation validator found** for `EnableMfaCommand`. An empty/wrong code simply fails TOTP verification (see Error Codes).

**Error Codes**
- **`403 FORBIDDEN`** (annotated 401) — code does not validate against the stored secret (`UnauthorizedAccessException("Invalid authentication code.")`).
- `409 CONFLICT` — MFA already enabled, or `mfa/setup` was never called first (`InvalidOperationException("MFA is already enabled for this account.")` / `"Call POST /auth/mfa/setup before enabling MFA."`).

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/auth/mfa/enable \
  -H "Authorization: Bearer eyJ..." -H "Content-Type: application/json" -d '{"code":"123456"}'
```

**Best Practices**
- Instruct the user to save/print/download the 10 recovery codes immediately — the API has no endpoint to retrieve them again after this response (only `mfa/recovery-codes/regenerate`, which invalidates the old set).

---

### POST /api/v1/auth/mfa/disable

**Purpose** — Turn MFA off for the current account. Requires the account password as a second confirmation beyond the bearer token.

**URL** — `/api/v1/auth/mfa/disable`

**Method** — POST

**Headers** — `Authorization: Bearer {accessToken}`, `Content-Type: application/json`.

**Authentication** — `[Authorize]`.

**Request**
```json
{ "password": "P@ssw0rd!" }
```
`DisableMfaCommand`: `UserId` (server-set), `Password` (required).

**Response** — `204 No Content`. Side effect: existing recovery codes are all cleared (`ReplaceMfaRecoveryCodesAsync(..., Array.Empty<...>())`).

**Validation** — **No FluentValidation validator found** for `DisableMfaCommand`.

**Error Codes**
- **`403 FORBIDDEN`** (annotated 401) — password incorrect (`UnauthorizedAccessException("Incorrect password.")`).
- `409 CONFLICT` — would surface if `UserId` didn't resolve to a user (`InvalidOperationException("User not found.")`) — but per the "not found" substring rule this actually maps to **404**, not 409; unreachable in practice since `UserId` is always a valid authenticated user.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/auth/mfa/disable \
  -H "Authorization: Bearer eyJ..." -H "Content-Type: application/json" -d '{"password":"P@ssw0rd!"}'
```

**Best Practices**
- Warn the user in the UI that disabling MFA also destroys all unused recovery codes — re-enabling later requires a fresh `mfa/setup` + `mfa/enable` round trip.

---

### POST /api/v1/auth/mfa/recovery-codes/regenerate

**Purpose** — Invalidate all existing recovery codes and issue 10 new ones. Requires the account password as confirmation.

**URL** — `/api/v1/auth/mfa/recovery-codes/regenerate`

**Method** — POST

**Headers** — `Authorization: Bearer {accessToken}`, `Content-Type: application/json`.

**Authentication** — `[Authorize]`.

**Request**
```json
{ "password": "P@ssw0rd!" }
```
`RegenerateMfaRecoveryCodesCommand`: `UserId` (server-set), `Password` (required).

**Response** — `200 OK`, `ApiResponse<RegenerateMfaRecoveryCodesResult>`:
```json
{
  "data": { "recoveryCodes": ["..." /* 10 new codes */] },
  "message": "New recovery codes issued. Store them securely — they will not be shown again."
}
```

**Validation** — **No FluentValidation validator found** for this command.

**Error Codes**
- **`403 FORBIDDEN`** (annotated 401) — incorrect password.
- `409 CONFLICT` — MFA is not currently enabled on the account (`InvalidOperationException("MFA is not enabled for this account.")`).

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/auth/mfa/recovery-codes/regenerate \
  -H "Authorization: Bearer eyJ..." -H "Content-Type: application/json" -d '{"password":"P@ssw0rd!"}'
```

**Best Practices**
- Prompt the user to discard/shred any previously saved recovery codes once this succeeds — the old set is fully invalidated, not appended to.

---

### GET /api/v1/auth/me

**Purpose** — Fetch the profile of the currently authenticated user (for populating the app shell / auth context on load).

**URL** — `/api/v1/auth/me`

**Method** — GET

**Headers** — `Authorization: Bearer {accessToken}`.

**Authentication** — `[Authorize]`.

**Request** — No body/params.

**Response** — `200 OK`, `ApiResponse<CurrentUserDto>`:
```json
{
  "data": {
    "id": "3e1f2a4b-....",
    "userName": "jane.doe",
    "email": "jane.doe@example.com",
    "role": "Admin",
    "isActive": true,
    "isMfaEnabled": false
  }
}
```
`CurrentUserDto`: `id` (Guid), `userName` (string), `email` (string), `role` (string?, single role name — `user.Role?.Name`, null if unassigned), `isActive` (bool), `isMfaEnabled` (bool). Note: no `employeeId`/`companyId`/permissions list is included here, unlike `UserDto` from the Users API.

**Validation** — N/A.

**Error Codes**
- `401 Unauthorized` — missing/invalid access token.
- (Theoretical) `404`/`InvalidOperationException("User not found.")` if the token's subject no longer exists — this is not wrapped in try/catch by the controller, so it flows to the middleware and maps to 404 `NOT_FOUND`.

**Examples**
```bash
curl https://api.example.com/api/v1/auth/me -H "Authorization: Bearer eyJ..."
```

**Best Practices**
- Call once on app bootstrap/refresh to hydrate auth context; don't poll — combine with the axios 401 interceptor to detect stale sessions instead.

---

## Users API

Controller: `backend/EMS.API/Controllers/UsersController.cs`
Route prefix: `[Route("api/v1/users")]` → all paths below are `/api/v1/users/...`.
**Controller-level** `[Authorize(Policy = "CanManageUsers")]` — applies to every action in this controller. Policy defined in `Program.cs`: `options.AddPolicy("CanManageUsers", policy => policy.RequireRole("Admin"));` — i.e. **Admin role only**, no per-action override.

### GET /api/v1/users

**Purpose** — List user accounts, optionally filtered by role/active-status and including soft-deleted ones.

**URL** — `/api/v1/users`

**Method** — GET

**Headers** — `Authorization: Bearer {accessToken}`.

**Authentication** — `Admin` role only (`CanManageUsers` policy).

**Request** — Query params (all optional), backing `GetUsersQuery`:
| Param | Type | Default |
| --- | --- | --- |
| `includeDeleted` | bool | `false` |
| `roleId` | Guid? | none |
| `isActive` | bool? | none (no filter) |

**Response** — `200 OK`, `ApiResponse<IEnumerable<UserDto>>`:
```json
{
  "data": [
    {
      "id": "3e1f2a4b-....",
      "userName": "jane.doe",
      "email": "jane.doe@example.com",
      "isActive": true,
      "roleId": "9c2e....",
      "roleName": "Admin",
      "employeeId": "7b1a....",
      "isDeleted": false,
      "createdAtUtc": "2026-01-10T09:00:00Z",
      "updatedAtUtc": null
    }
  ]
}
```
Not paginated in code (no `page`/`pageSize`/`totalCount` despite `docs/api-specification.md` §2.6 documenting generic list pagination) — this endpoint returns the full filtered set as a flat array.

**Validation** — No validator class found for `GetUsersQuery` (query params are plain optional types with model-binder defaults; no FluentValidation validator registered for it).

**Error Codes**
- `403 Forbidden` — caller authenticated but not in `Admin` role (ASP.NET Core authorization-policy failure — this is genuinely a 403, distinct from the exception-middleware cases above).
- `401 Unauthorized` — missing/invalid token.

**Examples**
```bash
curl "https://api.example.com/api/v1/users?isActive=true&roleId=9c2e0000-0000-0000-0000-000000000000" \
  -H "Authorization: Bearer eyJ..."
```

**Best Practices**
- Cache/debounce client-side filtering rather than re-querying per keystroke since there's no server-side search/pagination to lean on.

---

### GET /api/v1/users/{id}

**Purpose** — Fetch a single user account by id.

**URL** — `/api/v1/users/{id}`

**Method** — GET

**Headers** — `Authorization: Bearer {accessToken}`.

**Authentication** — `Admin` role only.

**Request** — Path param `id` (Guid, required, constrained by route `{id:guid}`).

**Response** — `200 OK`, `ApiResponse<UserDto>` (same shape as list item above).

**Validation** — Route constraint `:guid` returns a framework-level 404 for non-Guid ids (never reaches the handler).

**Error Codes**
- `404 Not Found` — no user with that id (`GetUserByIdQueryHandler` returns null → controller returns plain `NotFound()`, i.e. **not** wrapped in `ApiErrorResponse` — this is a bare 404 with no JSON body, unlike other 404s in this reference that go through the middleware).
- `401`/`403` — as above.

**Examples**
```bash
curl https://api.example.com/api/v1/users/3e1f2a4b-0000-0000-0000-000000000000 \
  -H "Authorization: Bearer eyJ..."
```

**Best Practices**
- Handle this endpoint's 404 defensively as "no body" — don't assume every 404 across the API has an `ApiErrorResponse` JSON payload.

---

### POST /api/v1/users

**Purpose** — Create a new user account (e.g. provisioning an employee's login, or a standalone admin account).

**URL** — `/api/v1/users`

**Method** — POST

**Headers** — `Authorization: Bearer {accessToken}`, `Content-Type: application/json`.

**Authentication** — `Admin` role only.

**Request**
```json
{
  "userName": "jane.doe",
  "email": "jane.doe@example.com",
  "temporaryPassword": "Temp@1234",
  "roleId": "9c2e0000-0000-0000-0000-000000000000",
  "employeeId": null,
  "isActive": true
}
```
`CreateUserCommand`: `UserName` (required), `Email` (required), `TemporaryPassword` (required), `RoleId` (Guid?, optional), `EmployeeId` (Guid?, optional), `IsActive` (bool, default `true`).

**Response** — `201 Created`, `Location` header pointing at `GET /api/v1/users/{id}`, body `ApiResponse<UserDto>` with message `"User created successfully."`.

**Validation** — `CreateUserCommandValidator`:
- `UserName`: NotEmpty, `MaximumLength(256)`, must not already exist (`UserNameExistsAsync`).
- `Email`: NotEmpty, `EmailAddress()`, `MaximumLength(256)`, must not already exist (`EmailExistsAsync`).
- `TemporaryPassword`: NotEmpty, `MinimumLength(8)`, must contain upper/lower/digit/special character.
- `RoleId`: if provided, must reference an existing role.

**Error Codes**
- `400 VALIDATION_ERROR` — any of the above rules fail, field-level (e.g. `{"propertyName":"Email","errorMessage":"Email already exists."}`).
- `409 CONFLICT` — the controller also documents `409` via `[ProducesResponseType(..., 409)]`; in practice, duplicate username/email is caught by the **validator** first (→ 400 `VALIDATION_ERROR`), but the handler has its own defense-in-depth checks (`CreateUserCommandHandler` throws `InvalidOperationException("Username already exists.")` / `"Email already exists."` / `"Role {id} not found."` for a race between validation and insert) which map to 409 (or 404 for the "not found" role case, since that message contains "not found").

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/users \
  -H "Authorization: Bearer eyJ..." -H "Content-Type: application/json" \
  -d '{"userName":"jane.doe","email":"jane.doe@example.com","temporaryPassword":"Temp@1234","roleId":"9c2e0000-0000-0000-0000-000000000000","isActive":true}'
```

**Best Practices**
- Generate a random, one-time `temporaryPassword` server-side in your admin UI rather than letting an admin type a guessable one; force a password change on first login (not automated by this API — must be enforced by client flow).
- Treat username/email conflicts as recoverable inline-form errors (400), not toast/redirect-worthy failures.

---

### PUT /api/v1/users/{id}

**Purpose** — Update an existing user's profile fields (username, email, role, linked employee).

**URL** — `/api/v1/users/{id}`

**Method** — PUT

**Headers** — `Authorization: Bearer {accessToken}`, `Content-Type: application/json`.

**Authentication** — `Admin` role only.

**Request** — Path param `id` (Guid) **must match** body `id`. Body (`UpdateUserCommand`):
```json
{
  "id": "3e1f2a4b-0000-0000-0000-000000000000",
  "userName": "jane.doe2",
  "email": "jane.doe2@example.com",
  "roleId": "9c2e0000-0000-0000-0000-000000000000",
  "employeeId": null
}
```
All of `userName`/`email` required; `roleId`/`employeeId` optional. Note: no `isActive`/`temporaryPassword` fields here — status and password have their own endpoints.

**Response** — `200 OK`, `ApiResponse<UserDto>`.

**Validation** — Controller-level: `id != cmd.Id` → `400 ID_MISMATCH` (`"Route id does not match body id."`) before hitting MediatR at all. Then `UpdateUserCommandValidator`: `UserName` NotEmpty/MaxLength(256)/unique-excluding-self; `Email` NotEmpty/EmailAddress/MaxLength(256)/unique-excluding-self; `RoleId` must exist if provided.

**Error Codes**
- `400 ID_MISMATCH` — route/body id mismatch (bespoke code, not FluentValidation — check `code` field, not just status, to distinguish from `VALIDATION_ERROR`).
- `400 VALIDATION_ERROR` — duplicate username/email (excluding self), bad email format, unknown role.
- `404 Not Found` — user id doesn't exist (`UpdateUserCommandHandler`: `InvalidOperationException($"User {id} not found.")` → 404 via the "not found" substring rule).

**Examples**
```bash
curl -X PUT https://api.example.com/api/v1/users/3e1f2a4b-0000-0000-0000-000000000000 \
  -H "Authorization: Bearer eyJ..." -H "Content-Type: application/json" \
  -d '{"id":"3e1f2a4b-0000-0000-0000-000000000000","userName":"jane.doe2","email":"jane.doe2@example.com","roleId":"9c2e0000-0000-0000-0000-000000000000"}'
```

**Best Practices**
- This is a full-replace PUT (no PATCH-style partial update for the profile fields) — always send the current `userName`/`email`/`roleId`/`employeeId` together, not just the changed field.
- Check the `code` field on 400 responses (`ID_MISMATCH` vs `VALIDATION_ERROR`) to render the right UI error.

---

### PATCH /api/v1/users/{id}/status

**Purpose** — Activate or deactivate a user account. Deactivating immediately revokes all of that user's refresh tokens (forces logout everywhere).

**URL** — `/api/v1/users/{id}/status`

**Method** — PATCH

**Headers** — `Authorization: Bearer {accessToken}`, `Content-Type: application/json`.

**Authentication** — `Admin` role only.

**Request** — Path param `id` (Guid). Body (`UpdateUserStatusCommand`, `id` is overwritten server-side from the route so it's optional/ignored in the body):
```json
{ "isActive": false }
```

**Response** — `200 OK`, `ApiResponse<UserDto>` reflecting the new status.

**Validation** — No validator class found for `UpdateUserStatusCommand`.

**Error Codes**
- `404 Not Found` — user id doesn't exist (`InvalidOperationException($"User {id} not found.")`).

**Examples**
```bash
curl -X PATCH https://api.example.com/api/v1/users/3e1f2a4b-0000-0000-0000-000000000000/status \
  -H "Authorization: Bearer eyJ..." -H "Content-Type: application/json" -d '{"isActive":false}'
```

**Best Practices**
- Warn admins in the UI that deactivation is not just a flag flip — it force-logs-out the user's active sessions immediately.

---

### DELETE /api/v1/users/{id}

**Purpose** — Soft-delete a user account (sets `IsDeleted`; recoverable via restore). Also revokes all of the user's refresh tokens.

**URL** — `/api/v1/users/{id}`

**Method** — DELETE

**Headers** — `Authorization: Bearer {accessToken}`.

**Authentication** — `Admin` role only.

**Request** — Path param `id` (Guid). No body.

**Response** — `204 No Content`.

**Validation** — N/A.

**Error Codes**
- `404 Not Found` — user id doesn't exist (`InvalidOperationException($"User {id} not found.")`).

**Examples**
```bash
curl -X DELETE https://api.example.com/api/v1/users/3e1f2a4b-0000-0000-0000-000000000000 \
  -H "Authorization: Bearer eyJ..."
```

**Best Practices**
- This is a soft delete — pair the confirmation UI with a note that the account can be restored via `POST /users/{id}/restore`, and that it immediately kills the user's active sessions.

---

### POST /api/v1/users/{id}/restore

**Purpose** — Restore a previously soft-deleted user account.

**URL** — `/api/v1/users/{id}/restore`

**Method** — POST

**Headers** — `Authorization: Bearer {accessToken}`.

**Authentication** — `Admin` role only.

**Request** — Path param `id` (Guid). No body.

**Response** — `204 No Content`.

**Validation** — N/A.

**Error Codes**
- `404 Not Found` — no such user **including soft-deleted ones** (`GetByIdIncludingDeletedAsync` returns null → `InvalidOperationException($"User {id} not found.")`).
- `409 CONFLICT` — user exists but is not currently deleted (`InvalidOperationException("User is not deleted and cannot be restored.")`).

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/users/3e1f2a4b-0000-0000-0000-000000000000/restore \
  -H "Authorization: Bearer eyJ..."
```

**Best Practices**
- Only surface this action in the UI when `isDeleted: true` on the fetched `UserDto`, to avoid the predictable 409.

---

## Roles API

Controller: `backend/EMS.API/Controllers/RolesController.cs`
Route prefix: `[Route("api/v1/roles")]` → all paths below are `/api/v1/roles/...`.
**Controller-level** `[Authorize(Policy = "CanManageUsers")]` (Admin-only), **except** `GET /roles` which overrides with `[Authorize(Policy = "CanViewRoles")]` at the action level. Policies (`Program.cs`):
- `CanManageUsers` → `RequireRole("Admin")`
- `CanViewRoles` → `RequireRole("Admin", "HR")`

### GET /api/v1/roles

**Purpose** — List roles (for populating role dropdowns in user-management UIs, etc.), optionally including soft-deleted ones.

**URL** — `/api/v1/roles`

**Method** — GET

**Headers** — `Authorization: Bearer {accessToken}`.

**Authentication** — `Admin` or `HR` role (`CanViewRoles` policy — action-level override of the controller's `CanManageUsers`).

**Request** — Query param `includeDeleted` (bool, optional, default `false`), backing `GetRolesQuery`.

**Response** — `200 OK`, `ApiResponse<IEnumerable<RoleDto>>`:
```json
{
  "data": [
    {
      "id": "9c2e0000-0000-0000-0000-000000000000",
      "name": "Admin",
      "description": "Full system access",
      "isDeleted": false,
      "createdAtUtc": "2026-01-01T00:00:00Z",
      "updatedAtUtc": null
    }
  ]
}
```
Not paginated (full array), same as Users list.

**Validation** — No validator class found for `GetRolesQuery`.

**Error Codes**
- `403 Forbidden` — caller is neither `Admin` nor `HR`.
- `401 Unauthorized` — missing/invalid token.

**Examples**
```bash
curl "https://api.example.com/api/v1/roles?includeDeleted=false" -H "Authorization: Bearer eyJ..."
```

**Best Practices**
- HR-role clients should treat this as read-only — every other Roles endpoint requires `Admin` specifically, so don't expose create/edit/delete controls to HR-only users even though they can list roles.

---

### GET /api/v1/roles/{id}

**Purpose** — Fetch a single role by id.

**URL** — `/api/v1/roles/{id}`

**Method** — GET

**Headers** — `Authorization: Bearer {accessToken}`.

**Authentication** — `Admin` only (controller-level `CanManageUsers`; this action has no override, so — unlike the list endpoint — HR cannot call this one).

**Request** — Path param `id` (Guid, `{id:guid}` route constraint).

**Response** — `200 OK`, `ApiResponse<RoleDto>` (same shape as list item).

**Validation** — Route constraint handles non-Guid ids at the framework level.

**Error Codes**
- `404 Not Found` — role not found; controller returns bare `NotFound()` (no `ApiErrorResponse` JSON body, same pattern as `GET /users/{id}`).
- `403 Forbidden` — caller is HR (not Admin) — flagged here because it's an easy asymmetry to miss vs. the list endpoint being HR-accessible.

**Examples**
```bash
curl https://api.example.com/api/v1/roles/9c2e0000-0000-0000-0000-000000000000 \
  -H "Authorization: Bearer eyJ..."
```

**Best Practices**
- Don't assume HR users who can list roles can also fetch role detail by id — gate the "view role details" UI action to Admin only.

---

### POST /api/v1/roles

**Purpose** — Create a new role.

**URL** — `/api/v1/roles`

**Method** — POST

**Headers** — `Authorization: Bearer {accessToken}`, `Content-Type: application/json`.

**Authentication** — `Admin` only.

**Request**
```json
{ "name": "Team Lead", "description": "Manages a small team, no admin access." }
```
`CreateRoleCommand`: `Name` (required), `Description` (optional).

**Response** — `201 Created`, `Location` header → `GET /api/v1/roles/{id}`, body `ApiResponse<RoleDto>` with message `"Role created successfully."`.

**Validation** — `CreateRoleCommandValidator`:
- `Name`: NotEmpty, `MaximumLength(50)`, must not already exist (`NameExistsAsync`).
- `Description`: `MaximumLength(250)` (optional field, no NotEmpty).

**Error Codes**
- `400 VALIDATION_ERROR` — empty/too-long name, too-long description, duplicate name.
- `409 CONFLICT` — documented via `[ProducesResponseType(..., 409)]`; the handler also has a defense-in-depth duplicate-name check (`CreateRoleCommandHandler`: `InvalidOperationException("Role name already exists.")`) for races past validation.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/roles \
  -H "Authorization: Bearer eyJ..." -H "Content-Type: application/json" \
  -d '{"name":"Team Lead","description":"Manages a small team, no admin access."}'
```

**Best Practices**
- Role names beyond the four documented in `docs/api-specification.md` §2.7 (`Admin`, `HR`, `Manager`, `Employee`) are technically creatable via this endpoint (only uniqueness + length are enforced) — but such custom roles won't map to any of the app's hardcoded `RequireRole(...)` policy checks (`CanManageUsers`, `CanViewRoles`, etc.), so they'll have no effective permissions until backend policies are extended. Confirm with the backend team before offering free-text role creation in the UI.

---

### PUT /api/v1/roles/{id}

**Purpose** — Update an existing role's name/description.

**URL** — `/api/v1/roles/{id}`

**Method** — PUT

**Headers** — `Authorization: Bearer {accessToken}`, `Content-Type: application/json`.

**Authentication** — `Admin` only.

**Request** — Path `id` must match body `id`. Body (`UpdateRoleCommand`):
```json
{ "id": "9c2e0000-0000-0000-0000-000000000000", "name": "Team Lead", "description": "Updated description." }
```

**Response** — `200 OK`, `ApiResponse<RoleDto>`.

**Validation** — Controller: `id != cmd.Id` → `400 ID_MISMATCH`. `UpdateRoleCommandValidator`: `Name` NotEmpty/MaxLength(50)/unique-excluding-self; `Description` MaxLength(250).

**Error Codes**
- `400 ID_MISMATCH` — route/body id mismatch.
- `400 VALIDATION_ERROR` — invalid/duplicate name, description too long.
- `404 Not Found` — role id doesn't exist (`InvalidOperationException($"Role {id} not found.")`).

**Examples**
```bash
curl -X PUT https://api.example.com/api/v1/roles/9c2e0000-0000-0000-0000-000000000000 \
  -H "Authorization: Bearer eyJ..." -H "Content-Type: application/json" \
  -d '{"id":"9c2e0000-0000-0000-0000-000000000000","name":"Team Lead","description":"Updated description."}'
```

**Best Practices**
- Same `ID_MISMATCH` vs `VALIDATION_ERROR` code-checking guidance as `PUT /users/{id}` applies here.

---

### DELETE /api/v1/roles/{id}

**Purpose** — Soft-delete a role. Blocked if any active (non-deleted) user is still assigned to it.

**URL** — `/api/v1/roles/{id}`

**Method** — DELETE

**Headers** — `Authorization: Bearer {accessToken}`.

**Authentication** — `Admin` only.

**Request** — Path param `id` (Guid). No body.

**Response** — `204 No Content`.

**Validation** — N/A.

**Error Codes**
- `404 Not Found` — role id doesn't exist (`InvalidOperationException($"Role {id} not found.")`).
- `409 CONFLICT` — role is still assigned to at least one active user (`DeleteRoleCommandHandler`: `IsInUseAsync` check → `InvalidOperationException("Role is assigned to one or more active users and cannot be deleted.")`). This is the one endpoint in this reference where 409 is the primary/expected business error, not just a defense-in-depth fallback.

**Examples**
```bash
curl -X DELETE https://api.example.com/api/v1/roles/9c2e0000-0000-0000-0000-000000000000 \
  -H "Authorization: Bearer eyJ..."
```

**Best Practices**
- Before showing a delete button, cross-check the role isn't in use (e.g. via `GET /users?roleId={id}&isActive=true`) to avoid a predictable 409 and give the admin an inline "reassign these N users first" hint instead.

---

### POST /api/v1/roles/{id}/restore

**Purpose** — Restore a previously soft-deleted role.

**URL** — `/api/v1/roles/{id}/restore`

**Method** — POST

**Headers** — `Authorization: Bearer {accessToken}`.

**Authentication** — `Admin` only.

**Request** — Path param `id` (Guid). No body.

**Response** — `204 No Content`.

**Validation** — N/A.

**Error Codes**
- `404 Not Found` — no such role including soft-deleted ones (`GetByIdIncludingDeletedAsync` null → `InvalidOperationException($"Role {id} not found.")`).
- `409 CONFLICT` — role exists but is not currently deleted (`InvalidOperationException("Role is not deleted and cannot be restored.")`).

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/roles/9c2e0000-0000-0000-0000-000000000000/restore \
  -H "Authorization: Bearer eyJ..."
```

**Best Practices**
- Same guidance as `POST /users/{id}/restore` — only show this action when `isDeleted: true`.
## Employees API

Base route: `/api/v1/employees` (`[Authorize]` at class level; every action additionally requires the `CanViewEmployees` policy — `Admin`, `HR`, `Manager` — or the stricter `CanManageEmployees` policy — `Admin`, `HR` — as noted per endpoint, from `backend/EMS.API/Program.cs`).

> **Note on the plain `Employee` role**: every action on this controller requires either `CanViewEmployees` (Admin/HR/Manager) or `CanManageEmployees` (Admin/HR). Neither policy includes the bare `Employee` role, so an ordinary employee cannot call `GET /employees`, `GET /employees/{id}`, or even `PATCH /employees/{id}/profile` (self-service profile update) for their own record through this controller today. If self-service is intended, this is a gap worth confirming with the team rather than an inferred design choice.

### GET /api/v1/employees — List employees

**Purpose**: Paginated, filterable, sortable list of employees for directory/admin screens.
**URL**: `/api/v1/employees`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanViewEmployees` (Admin, HR, Manager).
**Request** (query string, bound to `GetEmployeesQuery`):
```
GET /api/v1/employees?page=1&pageSize=20&search=jane&departmentId=...&teamId=...&designationId=...&officeLocationId=...&status=Active&sortBy=lastName&sortDir=asc
```
All fields optional except paging defaults (`page=1`, `pageSize=20`).
**Response**: `200 OK`
```json
{
  "data": {
    "data": [
      {
        "id": "b1e2...",
        "employeeCode": "EMP-1024",
        "firstName": "Jane",
        "middleName": null,
        "lastName": "Doe",
        "fullName": "Jane Doe",
        "email": "jane.doe@company.com",
        "phoneNumber": "+1-555-0100",
        "dateOfBirth": "1990-05-14",
        "gender": "Female",
        "address": { "addressLine1": "12 Main St", "addressLine2": null, "city": "Austin", "state": "TX", "postalCode": "73301", "country": "USA" },
        "emergencyContact": { "name": "John Doe", "phone": "+1-555-0101", "relation": "Spouse" },
        "joinDate": "2022-03-01",
        "exitDate": null,
        "departmentId": "d1...",
        "departmentName": "Engineering",
        "teamId": "t1...",
        "teamName": "Platform",
        "designationId": "de1...",
        "designationName": "Senior Engineer",
        "managerId": "m1...",
        "officeLocationId": "o1...",
        "officeLocationName": "Austin HQ",
        "profilePhotoDocumentId": null,
        "employmentStatus": "Active",
        "isActive": true,
        "isDeleted": false,
        "createdAtUtc": "2022-03-01T09:00:00Z",
        "updatedAtUtc": null
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 125,
    "totalPages": 7
  },
  "message": "Request completed successfully"
}
```
**Validation**: No FluentValidation validator exists for `GetEmployeesQuery` — page/pageSize/sort are not bounds-checked in the Application layer (e.g. no explicit cap on `pageSize`); confirm this is intentional before relying on it for abuse-prevention.
**Error Codes**: `401` (no/invalid token), `403 FORBIDDEN` (authenticated but not Admin/HR/Manager).
**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" "https://api.example.com/api/v1/employees?page=1&pageSize=20&search=jane"
```
**Best Practices**:
- Always pass `pageSize` explicitly for list UIs; don't rely on the server default staying 20.
- Filter server-side (`departmentId`, `teamId`, etc.) rather than fetching all pages and filtering client-side.

### GET /api/v1/employees/{id} — Get employee by ID

**Purpose**: Fetch full detail for one employee record.
**URL**: `/api/v1/employees/{id}`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanViewEmployees` (Admin, HR, Manager).
**Request**: Path `id` (GUID).
**Response**: `200 OK` — `ApiResponse<EmployeeDto>` (same shape as the list item above). `404` if not found — returned as a **bare `NotFound()`** with no JSON body/envelope, inconsistent with the `ApiErrorResponse` envelope used elsewhere.
**Validation**: None (simple GUID route binding).
**Error Codes**: `401`, `403 FORBIDDEN`, `404` (empty body).
**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/employees/b1e2c3d4-...
```
**Best Practices**: Don't assume a JSON error body on 404 for this endpoint specifically — check status code only.

### POST /api/v1/employees — Create employee

**Purpose**: Onboard a new employee record.
**URL**: `/api/v1/employees`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageEmployees` (Admin, HR).
**Request** (`CreateEmployeeCommand`):
```json
{
  "employeeCode": "EMP-1099",
  "firstName": "Alex",
  "middleName": null,
  "lastName": "Kim",
  "email": "alex.kim@company.com",
  "phoneNumber": "+1-555-0199",
  "dateOfBirth": "1995-02-20",
  "gender": "Male",
  "address": { "addressLine1": "1 Loop Rd", "city": "Austin", "state": "TX", "postalCode": "73301", "country": "USA" },
  "emergencyContact": { "name": "Sam Kim", "phone": "+1-555-0198", "relation": "Sibling" },
  "joinDate": "2026-08-01",
  "departmentId": "d1...",
  "teamId": "t1...",
  "designationId": "de1...",
  "managerId": "m1...",
  "officeLocationId": "o1...",
  "employmentStatus": "Active"
}
```
Required: `employeeCode`, `firstName`, `lastName`, `joinDate`, `designationId`, `officeLocationId`. Optional: everything else, including `email`, but if provided it's validated and uniqueness-checked.
**Response**: `201 Created`, `Location` header pointing to `GET /api/v1/employees/{id}`, body `ApiResponse<EmployeeDto>` with message `"Employee created successfully."`.
**Validation** (`EmployeeCommandValidator` → `CreateEmployeeCommand`):
- `EmployeeCode`: required, max 50, must be unique within the caller's company (`EmployeeCodeExistsAsync`).
- `FirstName`, `LastName`: required, max 100.
- `Email`: valid email format when provided, must be unique within the company when provided.
- `DesignationId`: required, must reference an existing designation in the company.
- `OfficeLocationId`: required, must reference an existing office location in the company.
- `TeamId`: optional; if set, must reference an existing team, and (if `DepartmentId` is also set) the team must belong to that department.
**Error Codes**: `400 VALIDATION_ERROR` (e.g. duplicate `employeeCode`/`email`, missing designation/office location, team/department mismatch), `401`, `403 FORBIDDEN`.
**Examples**:
```bash
curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"employeeCode":"EMP-1099","firstName":"Alex","lastName":"Kim","joinDate":"2026-08-01","designationId":"de1...","officeLocationId":"o1..."}' \
  https://api.example.com/api/v1/employees
```
**Best Practices**:
- Resolve `designationId`/`officeLocationId`/`teamId` from their respective list endpoints first — invalid references fail as `400`, not `404`.
- Treat `employeeCode` uniqueness as company-scoped, not global, when generating codes client-side.

### PUT /api/v1/employees/{id} — Full update employee

**Purpose**: Replace an employee's editable fields (admin/HR edit form).
**URL**: `/api/v1/employees/{id}`
**Method**: PUT
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageEmployees` (Admin, HR).
**Request** (`UpdateEmployeeCommand`, same shape as create plus `id` in the body): route `id` must equal body `id`, or the request is rejected before reaching the handler.
**Response**: `200 OK`, `ApiResponse<EmployeeDto>`.
**Validation**: Same rules as create (`UpdateEmployeeCommandValidator`), with uniqueness checks excluding the current record's own `id`.
**Error Codes**: `400 ID_MISMATCH` (`{"status":400,"code":"ID_MISMATCH","message":"Route id does not match body id."}`) when route/body `id` differ, `400 VALIDATION_ERROR`, `401`, `403 FORBIDDEN`, `404`.
**Examples**:
```bash
curl -X PUT -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"id":"b1e2...","employeeCode":"EMP-1024","firstName":"Jane","lastName":"Doe","joinDate":"2022-03-01","designationId":"de1...","officeLocationId":"o1..."}' \
  https://api.example.com/api/v1/employees/b1e2...
```
**Best Practices**: Always set the body `id` to match the URL segment — a mismatch is rejected client-side-style by the controller itself before validation runs.

### PATCH /api/v1/employees/{id}/profile — Self-service profile update

**Purpose**: Update the low-risk subset of a profile (phone, address, emergency contact) without full edit rights.
**URL**: `/api/v1/employees/{id}/profile`
**Method**: PATCH
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanViewEmployees` (Admin, HR, Manager) — **not** open to the employee themselves under the plain `Employee` role, despite the "self-service" naming/intent; see the note at the top of this section.
**Request** (`UpdateEmployeeProfileCommand`):
```json
{ "phoneNumber": "+1-555-0111", "address": { "city": "Dallas", "state": "TX" }, "emergencyContact": { "name": "John Doe", "phone": "+1-555-0101", "relation": "Spouse" } }
```
All fields optional.
**Response**: `204 No Content`.
**Validation**: No dedicated FluentValidation validator found for `UpdateEmployeeProfileCommand` — confirm this is intentional before treating any of these fields as constrained.
**Error Codes**: `401`, `403 FORBIDDEN`, `404`.
**Examples**:
```bash
curl -X PATCH -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"phoneNumber":"+1-555-0111"}' https://api.example.com/api/v1/employees/b1e2.../profile
```

### PATCH /api/v1/employees/{id}/status — Update employment status

**Purpose**: Transition an employee's status (e.g. `Active` → `Terminated`/`OnLeave`), optionally recording an exit date and reason.
**URL**: `/api/v1/employees/{id}/status`
**Method**: PATCH
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageEmployees` (Admin, HR).
**Request** (`UpdateEmployeeStatusCommand`):
```json
{ "status": "Terminated", "exitDate": "2026-08-15", "reason": "Resignation" }
```
`status` required (free-text string in code — not a validated enum at the Application layer; see Validation note). `exitDate`, `reason` optional.
**Response**: `204 No Content`.
**Validation**: No FluentValidation validator found for `UpdateEmployeeStatusCommand` — `status` is not restricted to a known set of values (`Active`/`Inactive`/`Terminated`/`OnLeave` per docs/api-specification.md) at this layer; an arbitrary string could be persisted. Flagging as a real gap, not documenting invented constraints.
**Error Codes**: `400 VALIDATION_ERROR` (if a validator is later added), `401`, `403 FORBIDDEN`, `404`.
**Examples**:
```bash
curl -X PATCH -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"status":"Terminated","exitDate":"2026-08-15","reason":"Resignation"}' \
  https://api.example.com/api/v1/employees/b1e2.../status
```
**Best Practices**: Client-side, restrict the `status` value to the known set from `docs/api-specification.md` even though the server doesn't enforce it today — don't rely on server-side validation for this field until confirmed fixed.

### DELETE /api/v1/employees/{id} — Soft-delete employee

**Purpose**: Remove an employee from active views without losing history (soft delete).
**URL**: `/api/v1/employees/{id}`
**Method**: DELETE
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageEmployees` (Admin, HR).
**Request**: Path `id`.
**Response**: `204 No Content`.
**Validation**: None beyond existence.
**Error Codes**: `401`, `403 FORBIDDEN`, `404`.
**Examples**: `curl -X DELETE -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/employees/b1e2...`

### POST /api/v1/employees/{id}/restore — Restore soft-deleted employee

**Purpose**: Reverse a soft delete.
**URL**: `/api/v1/employees/{id}/restore`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageEmployees` (Admin, HR).
**Request**: Path `id`, no body.
**Response**: `204 No Content`.
**Validation**: None beyond existence/deleted-state (handler-level; no dedicated validator).
**Error Codes**: `400 VALIDATION_ERROR` (e.g. if not currently deleted, depending on handler logic), `401`, `403 FORBIDDEN`, `404`.
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/employees/b1e2.../restore`

### GET /api/v1/employees/{id}/reporting-hierarchy — Get manager chain

**Purpose**: Return the chain of managers above this employee, for org-chart breadcrumbs.
**URL**: `/api/v1/employees/{id}/reporting-hierarchy`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanViewEmployees` (Admin, HR, Manager).
**Request**: Path `id`.
**Response**: `200 OK`, `ApiResponse<IEnumerable<EmployeeDto>>` — ordered from immediate manager upward.
**Validation**: None.
**Error Codes**: `401`, `403 FORBIDDEN`, `404` if the employee itself doesn't exist.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/employees/b1e2.../reporting-hierarchy`

### GET /api/v1/employees/{id}/direct-reports — Get direct reports

**Purpose**: List employees who report directly to this employee (for manager dashboards).
**URL**: `/api/v1/employees/{id}/direct-reports`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanViewEmployees` (Admin, HR, Manager).
**Request**: Path `id`; query `page` (default 1), `pageSize` (default 20).
**Response**: `200 OK`, `ApiResponse<IEnumerable<EmployeeDto>>`. Note: despite paging inputs, the response is a plain `IEnumerable`, not a `PagedResult` — no `totalCount`/`totalPages` metadata is returned for this endpoint.
**Validation**: None.
**Error Codes**: `401`, `403 FORBIDDEN`.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" "https://api.example.com/api/v1/employees/b1e2.../direct-reports?page=1&pageSize=20"`
**Best Practices**: Don't rely on total-count metadata from this endpoint for pagination UI; it isn't returned.

---

## Employee Documents API

Base route: `/api/employees/{employeeId}/documents` — **note this controller is not under `/api/v1`**, unlike every other controller in this group; a real inconsistency worth flagging. Responses are also **not** wrapped in the `ApiResponse<T>` envelope used elsewhere — endpoints return raw JSON/values directly.

### POST /api/employees/{employeeId}/documents — Upload employee document

**Purpose**: Upload an ID/contract/certificate file against an employee's record.
**URL**: `/api/employees/{employeeId}/documents`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: multipart/form-data`
**Authentication**: Any authenticated user (`[Authorize]`, no policy) — but the **controller itself** enforces ownership in code: allowed if caller is `Admin`/`HR`, or if the caller's own user id (from the `NameIdentifier` claim) equals `employeeId`. Otherwise `403 Forbid()`. This is hand-rolled in the action, not a declarative policy — worth being aware of if policies are audited centrally later.
**Request**: Path `employeeId`. Multipart form fields: `file` (required, binary), `documentType` (required, form field), `expiresAtUtc` (optional, form field, ISO date).
**Response**: `201 Created`, `Location` header to the download endpoint, **body is a bare `Guid`** (the new document id) — not wrapped in `ApiResponse<T>`.
**Validation** (`UploadDocumentCommandValidator`):
- `EmployeeId`: required.
- `DocumentType`: required, max 100.
- `FileName`: required, max 255, no invalid filename characters, no path separators (`..`, `/`, `\`) — prevents path traversal.
- `ContentType`: must be one of `application/pdf`, `image/jpeg`, `image/png`, `application/msword`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`.
- File extension must match the declared content type.
- `Content`: required (non-empty), max size **10 MB**.
- **Magic-byte signature check**: the actual file bytes must match the declared content type's known signature (e.g. `%PDF-`, JPEG/PNG/DOC/DOCX headers) — defends against a spoofed `Content-Type` header smuggling in a disallowed file type.
**Error Codes**: `400` (`"file is required"` as plain text if no file — not the `ApiErrorResponse` envelope — or `VALIDATION_ERROR` from the pipeline for the rules above), `401`, `403` (ownership check failure, bare `Forbid()`), `404` if `employeeId` doesn't resolve in the handler.
**Examples**:
```bash
curl -X POST -H "Authorization: Bearer $TOKEN" \
  -F "file=@passport.pdf;type=application/pdf" -F "documentType=Passport" \
  https://api.example.com/api/employees/b1e2.../documents
```
**Best Practices**:
- Always set the multipart part's content-type to match the real file — the signature check will reject mismatches even if the extension "looks right."
- Budget for the flat 10 MB cap; there's no per-document-type override.

### GET /api/employees/{employeeId}/documents — List employee documents

**Purpose**: List uploaded documents for an employee, optionally filtered by type/search.
**URL**: `/api/employees/{employeeId}/documents`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user (`[Authorize]`, no policy, **no ownership check in code** — unlike Upload/Delete, any authenticated user can list any employee's documents via this endpoint). Flagging as a likely gap relative to the upload/delete actions on the same controller.
**Request**: Path `employeeId`; query `page` (default 1), `pageSize` (default 20), `search`, `documentType` (all optional).
**Response**: `200 OK`, bare JSON array (`IEnumerable<DocumentDto>`, no envelope, no paging metadata even though `page`/`pageSize` are accepted):
```json
[
  { "id": "doc1...", "employeeId": "b1e2...", "documentType": "Passport", "originalFileName": "passport.pdf", "contentType": "application/pdf", "fileSizeBytes": 245678, "uploadedAtUtc": "2026-07-01T10:00:00Z", "expiresAtUtc": "2031-07-01T00:00:00Z" }
]
```
**Validation**: None.
**Error Codes**: `401`.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" "https://api.example.com/api/employees/b1e2.../documents?documentType=Passport"`

### GET /api/employees/{employeeId}/documents/{documentId}/download — Download document

**Purpose**: Retrieve the raw file bytes of an uploaded document.
**URL**: `/api/employees/{employeeId}/documents/{documentId}/download`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user (`[Authorize]`, no policy, no ownership check in code — the handler looks up by `documentId` alone; `employeeId` in the route is not cross-checked against the document's actual owner).
**Request**: Path `employeeId` (unused for authorization), `documentId`.
**Response**: `200 OK`, raw file bytes with `Content-Type` and filename from storage. `404` (empty body) if not found.
**Validation**: None.
**Error Codes**: `401`, `404`.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" -OJ https://api.example.com/api/employees/b1e2.../documents/doc1.../download`

### DELETE /api/employees/{employeeId}/documents/{documentId} — Delete document

**Purpose**: Remove an uploaded document.
**URL**: `/api/employees/{employeeId}/documents/{documentId}`
**Method**: DELETE
**Headers**: `Authorization: Bearer <token>`
**Authentication**: `[Authorize(Roles = "Admin,HR")]` — role-based, not the `CanManageEmployees`/`CanViewEmployees` policy names used elsewhere; functionally equivalent set of roles today, but a separate declaration to keep in sync if roles change.
**Request**: Path `employeeId` (unused), `documentId`.
**Response**: `204 No Content`.
**Validation**: None.
**Error Codes**: `401`, `403`, `404`.
**Examples**: `curl -X DELETE -H "Authorization: Bearer $TOKEN" https://api.example.com/api/employees/b1e2.../documents/doc1...`

---

## Departments API

Base route: `/api/v1/departments` (`[Authorize]` at class level — any authenticated user can `GET`; mutations require policy `CanManageDepartments` = Admin, HR).

### GET /api/v1/departments — List departments

**Purpose**: List all active (non-deleted) departments for dropdowns/admin screens.
**URL**: `/api/v1/departments`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user.
**Request**: No parameters.
**Response**: `200 OK`
```json
{ "data": [ { "id": "d1...", "name": "Engineering", "code": "ENG", "description": "Product engineering", "headEmployeeId": "m1...", "isDeleted": false, "createdAtUtc": "2022-01-01T00:00:00Z", "updatedAtUtc": null } ] }
```
**Validation**: None (no query parameters accepted at all — this endpoint does not support pagination/search).
**Error Codes**: `401`.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/departments`
**Best Practices**: Don't attempt to paginate this endpoint — it returns the full active list in one response.

### GET /api/v1/departments/{id} — Get department by ID

**Purpose**: Fetch a single department.
**URL**: `/api/v1/departments/{id}`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user.
**Request**: Path `id`.
**Response**: `200 OK`, `ApiResponse<DepartmentDto>`. `404` bare (no envelope) if not found.
**Validation**: None.
**Error Codes**: `401`, `404` (empty body).
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/departments/d1...`

### POST /api/v1/departments — Create department

**Purpose**: Add a new department.
**URL**: `/api/v1/departments`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageDepartments` (Admin, HR).
**Request** (`CreateDepartmentCommand`):
```json
{ "name": "Engineering", "code": "ENG", "description": "Product engineering", "headEmployeeId": "m1..." }
```
Required: `name`. Optional: `code`, `description`, `headEmployeeId` (not existence-checked against the Employees table by this validator).
**Response**: `201 Created`, `Location` header, `ApiResponse<DepartmentDto>` with message `"Department created successfully."`.
**Validation** (`CreateDepartmentCommandValidator`): `Name` required, max 150, unique within company. `Code` max 50, unique within company when provided (empty is allowed). `Description` max 500.
**Error Codes**: `400 VALIDATION_ERROR` (duplicate name/code), `401`, `403 FORBIDDEN`, `409` (if the exception middleware maps a name/code collision raised outside validation to conflict — validated case returns 400, not 409, since it's caught by FluentValidation before the handler runs).
**Examples**:
```bash
curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Engineering","code":"ENG"}' https://api.example.com/api/v1/departments
```

### PUT /api/v1/departments/{id} — Update department

**Purpose**: Rename/edit a department.
**URL**: `/api/v1/departments/{id}`
**Method**: PUT
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageDepartments`.
**Request** (`UpdateDepartmentCommand`): same shape as create plus `id`; route/body `id` mismatch → `400 ID_MISMATCH` before validation.
**Response**: `200 OK`, `ApiResponse<DepartmentDto>`.
**Validation**: Same as create, uniqueness checks exclude the current record.
**Error Codes**: `400 ID_MISMATCH`, `400 VALIDATION_ERROR`, `401`, `403 FORBIDDEN`, `404`.
**Examples**: `curl -X PUT -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"id":"d1...","name":"Engineering","code":"ENG"}' https://api.example.com/api/v1/departments/d1...`

### DELETE /api/v1/departments/{id} — Soft-delete department

**Purpose**: Remove a department from active use.
**URL**: `/api/v1/departments/{id}`
**Method**: DELETE
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageDepartments`.
**Request**: Path `id`.
**Response**: `204 No Content`.
**Validation**: None — the handler does **not** check for existing employees/teams referencing this department before soft-deleting it (`DeleteDepartmentCommandHandler` only checks existence). No `409` guard for "department still has employees," unlike some other resources in this system — worth flagging as a potential data-integrity gap since employees keep a dangling `departmentId`.
**Error Codes**: `401`, `403 FORBIDDEN`, `404` (handler throws `InvalidOperationException` with "not found" message, mapped by the global exception middleware to `404`).
**Examples**: `curl -X DELETE -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/departments/d1...`

### POST /api/v1/departments/{id}/restore — Restore department

**Purpose**: Reverse a soft delete.
**URL**: `/api/v1/departments/{id}/restore`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageDepartments`.
**Request**: Path `id`, no body.
**Response**: `204 No Content`.
**Validation**: None documented beyond existence (no dedicated validator).
**Error Codes**: `400 VALIDATION_ERROR`, `401`, `403 FORBIDDEN`, `404`.
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/departments/d1.../restore`

### GET /api/v1/departments/{id}/employees — List department employees

**Purpose**: List employees in a department (paged).
**URL**: `/api/v1/departments/{id}/employees`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanViewEmployees` (Admin, HR, Manager) — stricter than the department's own base `[Authorize]`.
**Request**: Path `id`; query `page` (default 1), `pageSize` (default 20).
**Response**: `200 OK`, `ApiResponse<IEnumerable<EmployeeDto>>` (no paging metadata in the response body despite paged input, same pattern as `/employees/{id}/direct-reports`).
**Validation**: None.
**Error Codes**: `401`, `403 FORBIDDEN`.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" "https://api.example.com/api/v1/departments/d1.../employees?page=1&pageSize=20"`

### GET /api/v1/departments/{id}/teams — List department teams

**Purpose**: List teams belonging to a department.
**URL**: `/api/v1/departments/{id}/teams`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user (no extra policy — inherits class-level `[Authorize]` only, unlike the sibling `/employees` sub-route on this same controller).
**Request**: Path `id`.
**Response**: `200 OK`, `ApiResponse<IEnumerable<TeamDto>>`.
**Validation**: None.
**Error Codes**: `401`.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/departments/d1.../teams`

---

## Designations API

Base route: `/api/v1/designations` (`[Authorize]` at class level; mutations require `CanManageDepartments`; no restore endpoint exists for this resource, unlike Departments/Employees — a `DeleteDesignationCommand` soft-deletes but there is no matching `RestoreDesignationCommand`/route).

### GET /api/v1/designations — List designations

**Purpose**: List all active designations (job titles/levels) for dropdowns.
**URL**: `/api/v1/designations`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user.
**Request**: None.
**Response**: `200 OK`, `ApiResponse<IEnumerable<DesignationDto>>`:
```json
{ "data": [ { "id": "de1...", "name": "Senior Engineer", "code": "SR-ENG", "level": 4, "isDeleted": false, "createdAtUtc": "2022-01-01T00:00:00Z", "updatedAtUtc": null } ] }
```
**Validation**: None; no filtering/paging supported.
**Error Codes**: `401`.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/designations`

### GET /api/v1/designations/{id} — Get designation by ID

**Purpose**: Fetch a single designation.
**URL**: `/api/v1/designations/{id}`
**Method**: GET
**Authentication**: Any authenticated user.
**Request**: Path `id`.
**Response**: `200 OK`, `ApiResponse<DesignationDto>`. `404` bare on not found.
**Validation**: None.
**Error Codes**: `401`, `404` (empty body).
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/designations/de1...`

### POST /api/v1/designations — Create designation

**Purpose**: Add a new job title/designation.
**URL**: `/api/v1/designations`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageDepartments`.
**Request** (`CreateDesignationCommand`): `{ "name": "Senior Engineer", "code": "SR-ENG", "level": 4 }`. Required: `name`, `code`. Optional: `level`.
**Response**: `201 Created`, `Location` header, `ApiResponse<DesignationDto>` with message `"Designation created successfully."`.
**Validation** (`CreateDesignationCommandValidator`): `Name` required, max 150, unique within company. `Code` required, max 50, unique within company.
**Error Codes**: `400 VALIDATION_ERROR`, `401`, `403 FORBIDDEN`.
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"name":"Senior Engineer","code":"SR-ENG","level":4}' https://api.example.com/api/v1/designations`

### PUT /api/v1/designations/{id} — Update designation

**Purpose**: Edit an existing designation.
**URL**: `/api/v1/designations/{id}`
**Method**: PUT
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageDepartments`.
**Request** (`UpdateDesignationCommand`): same as create plus `id`; mismatch → `400 ID_MISMATCH`.
**Response**: `200 OK`, `ApiResponse<DesignationDto>`.
**Validation**: Same as create (uniqueness excludes current record).
**Error Codes**: `400 ID_MISMATCH`, `400 VALIDATION_ERROR`, `401`, `403 FORBIDDEN`, `404`.
**Examples**: `curl -X PUT -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"id":"de1...","name":"Senior Engineer","code":"SR-ENG"}' https://api.example.com/api/v1/designations/de1...`

### DELETE /api/v1/designations/{id} — Soft-delete designation

**Purpose**: Retire a designation from active use.
**URL**: `/api/v1/designations/{id}`
**Method**: DELETE
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageDepartments`.
**Request**: Path `id`.
**Response**: `204 No Content`.
**Validation**: None — no guard against deleting a designation still referenced by active employees (`DesignationId` is required, non-nullable, on `Employee`); a dangling reference risk similar to Departments.
**Error Codes**: `401`, `403 FORBIDDEN`, `404` (`InvalidOperationException` "not found" → 404 via global middleware).
**Examples**: `curl -X DELETE -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/designations/de1...`
**Best Practices**: There is no restore endpoint for designations — treat delete as effectively permanent from the API's perspective even though the underlying record is soft-deleted in the database.

---

## Office Locations API

Base route: `/api/v1/office-locations` (`[Authorize]` at class level; mutations require `CanManageDepartments`; no restore endpoint, same gap as Designations).

### GET /api/v1/office-locations — List office locations

**Purpose**: List all active office locations, including geofencing config, for dropdowns and attendance check-in validation.
**URL**: `/api/v1/office-locations`
**Method**: GET
**Authentication**: Any authenticated user.
**Request**: None.
**Response**: `200 OK`, `ApiResponse<IEnumerable<OfficeLocationDto>>`:
```json
{ "data": [ { "id": "o1...", "name": "Austin HQ", "code": "AUS-HQ", "addressLine1": "1 Loop Rd", "addressLine2": null, "city": "Austin", "state": "TX", "country": "USA", "timeZoneId": "America/Chicago", "latitude": 30.2672, "longitude": -97.7431, "geofenceRadiusMeters": 200, "isDeleted": false, "createdAtUtc": "2022-01-01T00:00:00Z", "updatedAtUtc": null } ] }
```
**Validation**: None; no filtering/paging.
**Error Codes**: `401`.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/office-locations`

### GET /api/v1/office-locations/{id} — Get office location by ID

**Purpose**: Fetch a single office location.
**URL**: `/api/v1/office-locations/{id}`
**Method**: GET
**Authentication**: Any authenticated user.
**Request**: Path `id`.
**Response**: `200 OK`, `ApiResponse<OfficeLocationDto>`. `404` bare on not found.
**Validation**: None.
**Error Codes**: `401`, `404` (empty body).
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/office-locations/o1...`

### POST /api/v1/office-locations — Create office location

**Purpose**: Add a new office location, optionally with geofencing for attendance check-in.
**URL**: `/api/v1/office-locations`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageDepartments`.
**Request** (`CreateOfficeLocationCommand`):
```json
{
  "name": "Austin HQ", "code": "AUS-HQ",
  "addressLine1": "1 Loop Rd", "addressLine2": null, "city": "Austin", "state": "TX", "country": "USA",
  "timeZoneId": "America/Chicago",
  "latitude": 30.2672, "longitude": -97.7431, "geofenceRadiusMeters": 200
}
```
Required: `name`, `code`, `city`, `country`, `timeZoneId`. Optional: `addressLine1`, `addressLine2`, `state`, and the geofence trio (`latitude`/`longitude`/`geofenceRadiusMeters`), which must be all-set or all-empty together.
**Response**: `201 Created`, `Location` header, `ApiResponse<OfficeLocationDto>` with message `"Office location created successfully."`.
**Validation** (`CreateOfficeLocationCommandValidator`): `Name` required, max 150. `Code` required, max 50, unique within company. `City` required, max 100. `Country` required, max 100. `TimeZoneId` required, max 100. `AddressLine1`/`AddressLine2` max 250. `State` max 100. `Latitude` in `[-90, 90]` when provided. `Longitude` in `[-180, 180]` when provided. `GeofenceRadiusMeters` must be `> 0` when provided. Cross-field rule: latitude/longitude/radius must all be present together or all absent — partial geofence data is rejected.
**Error Codes**: `400 VALIDATION_ERROR` (including the all-or-nothing geofence rule and duplicate `code`), `401`, `403 FORBIDDEN`.
**Examples**:
```bash
curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Austin HQ","code":"AUS-HQ","city":"Austin","country":"USA","timeZoneId":"America/Chicago"}' \
  https://api.example.com/api/v1/office-locations
```
**Best Practices**: If enabling geofenced attendance for a location, set `latitude`, `longitude`, and `geofenceRadiusMeters` together in the same request — partial values are rejected outright, not silently ignored.

### PUT /api/v1/office-locations/{id} — Update office location

**Purpose**: Edit an office location.
**URL**: `/api/v1/office-locations/{id}`
**Method**: PUT
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageDepartments`.
**Request** (`UpdateOfficeLocationCommand`): same shape as create plus `id`; mismatch → `400 ID_MISMATCH`.
**Response**: `200 OK`, `ApiResponse<OfficeLocationDto>`.
**Validation**: Same rules as create; `Code` uniqueness excludes current record. Note: unlike create, `Code` is `NotEmpty()` + `MustAsync` unconditionally on update (same as create — required either way).
**Error Codes**: `400 ID_MISMATCH`, `400 VALIDATION_ERROR`, `401`, `403 FORBIDDEN`, `404`.
**Examples**: `curl -X PUT -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"id":"o1...","name":"Austin HQ","code":"AUS-HQ","city":"Austin","country":"USA","timeZoneId":"America/Chicago"}' https://api.example.com/api/v1/office-locations/o1...`

### DELETE /api/v1/office-locations/{id} — Soft-delete office location

**Purpose**: Retire an office location.
**URL**: `/api/v1/office-locations/{id}`
**Method**: DELETE
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageDepartments`.
**Request**: Path `id`.
**Response**: `204 No Content`.
**Validation**: None — no guard against employees still referencing this location (`OfficeLocationId` is required on `Employee`).
**Error Codes**: `401`, `403 FORBIDDEN`, `404` (`InvalidOperationException` "not found" → 404).
**Examples**: `curl -X DELETE -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/office-locations/o1...`
**Best Practices**: No restore endpoint exists for this resource either — same caveat as Designations.

---

## Teams API

Base route: `/api/v1/teams` (`[Authorize]` at class level; mutations require `CanManageDepartments`; no restore endpoint).

### GET /api/v1/teams — List teams

**Purpose**: List all active teams across all departments.
**URL**: `/api/v1/teams`
**Method**: GET
**Authentication**: Any authenticated user.
**Request**: None (not filterable by department here — use `GET /departments/{id}/teams` for that).
**Response**: `200 OK`, `ApiResponse<IEnumerable<TeamDto>>`:
```json
{ "data": [ { "id": "t1...", "departmentId": "d1...", "departmentName": "Engineering", "name": "Platform", "code": "ENG-PLAT", "leadEmployeeId": "m1...", "isDeleted": false, "createdAtUtc": "2022-01-01T00:00:00Z", "updatedAtUtc": null } ] }
```
**Validation**: None.
**Error Codes**: `401`.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/teams`

### GET /api/v1/teams/{id} — Get team by ID

**Purpose**: Fetch a single team.
**URL**: `/api/v1/teams/{id}`
**Method**: GET
**Authentication**: Any authenticated user.
**Request**: Path `id`.
**Response**: `200 OK`, `ApiResponse<TeamDto>`. `404` bare on not found.
**Validation**: None.
**Error Codes**: `401`, `404` (empty body).
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/teams/t1...`

### POST /api/v1/teams — Create team

**Purpose**: Add a new team within a department.
**URL**: `/api/v1/teams`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageDepartments`.
**Request** (`CreateTeamCommand`): `{ "departmentId": "d1...", "name": "Platform", "code": "ENG-PLAT", "leadEmployeeId": "m1..." }`. Required: `departmentId`, `name`, `code`. Optional: `leadEmployeeId` (not existence-checked).
**Response**: `201 Created`, `Location` header, `ApiResponse<TeamDto>` with message `"Team created successfully."`.
**Validation** (`CreateTeamCommandValidator`): `DepartmentId` required, must reference an existing department in the company. `Name` required, max 150. `Code` required, max 50, unique **within the same department** (not globally unique — two departments can each have a team coded e.g. `A`).
**Error Codes**: `400 VALIDATION_ERROR` (missing/invalid department, duplicate code within department), `401`, `403 FORBIDDEN`.
**Examples**:
```bash
curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"departmentId":"d1...","name":"Platform","code":"ENG-PLAT"}' https://api.example.com/api/v1/teams
```
**Best Practices**: Team `code` uniqueness is scoped to `departmentId`, not global — don't assume a code collision error means the code is taken system-wide.

### PUT /api/v1/teams/{id} — Update team

**Purpose**: Edit a team, including moving it to a different department.
**URL**: `/api/v1/teams/{id}`
**Method**: PUT
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageDepartments`.
**Request** (`UpdateTeamCommand`): same shape as create plus `id`; mismatch → `400 ID_MISMATCH`.
**Response**: `200 OK`, `ApiResponse<TeamDto>`.
**Validation**: Same as create; `Code` uniqueness within department excludes current record.
**Error Codes**: `400 ID_MISMATCH`, `400 VALIDATION_ERROR`, `401`, `403 FORBIDDEN`, `404`.
**Examples**: `curl -X PUT -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"id":"t1...","departmentId":"d1...","name":"Platform","code":"ENG-PLAT"}' https://api.example.com/api/v1/teams/t1...`

### DELETE /api/v1/teams/{id} — Soft-delete team

**Purpose**: Retire a team.
**URL**: `/api/v1/teams/{id}`
**Method**: DELETE
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageDepartments`.
**Request**: Path `id`.
**Response**: `204 No Content`.
**Validation**: None — employees keep a nullable `teamId` reference; a deleted team leaves those references dangling (not cascaded or cleared).
**Error Codes**: `401`, `403 FORBIDDEN`, `404` (`InvalidOperationException` "not found" → 404).
**Examples**: `curl -X DELETE -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/teams/t1...`

### GET /api/v1/teams/{id}/employees — List team employees

**Purpose**: List employees on a team (paged).
**URL**: `/api/v1/teams/{id}/employees`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanViewEmployees` (Admin, HR, Manager) — stricter than the base `[Authorize]` on the rest of this controller's GETs.
**Request**: Path `id`; query `page` (default 1), `pageSize` (default 20).
**Response**: `200 OK`, `ApiResponse<IEnumerable<EmployeeDto>>` (no paging metadata in body, same pattern as the other `.../employees` sub-routes).
**Validation**: None.
**Error Codes**: `401`, `403 FORBIDDEN`.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" "https://api.example.com/api/v1/teams/t1.../employees?page=1&pageSize=20"`
# Attendance, Leave, Leave Types, Shifts, Holidays — API Reference

All endpoints in this document require a valid JWT bearer token (`[Authorize]` is applied at the controller level on every controller below) unless stated otherwise. All controllers are versioned under `api/v1` and return `Content-Type: application/json` (`[Produces("application/json")]`).

**Common envelope shapes** (from `EMS.API.Controllers.AuthController` — shared across the API):

Success:
```json
{
  "data": { /* endpoint-specific payload */ },
  "message": "Request completed successfully.",
  "correlationId": "a1b2c3d4e5f6a7b8"
}
```

Error (produced by `EMS.API.Middleware.ExceptionHandlingMiddleware`, which maps exceptions thrown from MediatR handlers to HTTP status codes):
```json
{
  "status": 400,
  "code": "VALIDATION_ERROR",
  "message": "One or more validation errors occurred.",
  "errors": [ { "propertyName": "TotalDays", "errorMessage": "'Total Days' must be greater than '0'." } ],
  "correlationId": "a1b2c3d4e5f6a7b8"
}
```

Exception → status/code mapping used throughout this doc:
- `FluentValidation.ValidationException` → 400 `VALIDATION_ERROR` (field-level `errors` array)
- `InvalidOperationException` whose message contains "not found" → 404 `NOT_FOUND`
- Any other `InvalidOperationException` (business-rule violation, e.g. duplicate check-in, non-pending state transition) → 409 `CONFLICT`
- `UnauthorizedAccessException` (ownership/scope violation raised inside a handler) → 403 `FORBIDDEN`
- Any other unhandled exception → 500 `INTERNAL_ERROR`

Note: ASP.NET Core's own `[Authorize]`/policy pipeline returns plain 401 (no bearer token / expired token) or 403 (authenticated but role/policy check fails) *before* reaching the middleware above, so those responses do not necessarily follow the `ApiErrorResponse` shape unless a custom `AuthenticationFailedContext`/`OnForbidden` handler is wired (not found in these controllers — treat 401/403 bodies as framework default unless verified otherwise).

---

## Attendance API

Controller: `EMS.API.Controllers.AttendanceController`, route prefix `api/v1/attendance`, class-level `[Authorize]`.

### POST /api/v1/attendance/check-in

**Purpose**: Punch in for the current (or specified) work day. A client (mobile/web) calls this once per employee per day when the employee arrives at work; captures GPS location for geofence and audit purposes.

**URL**: `/api/v1/attendance/check-in`

**Method**: POST

**Headers**: `Authorization: Bearer <token>` (required), `Content-Type: application/json`. `User-Agent` is captured server-side into `DeviceInfo`; the caller's remote IP is captured into `IpAddress` — neither is client-settable.

**Authentication**: Any authenticated user. Employee-role callers may only check in for themselves (enforced in the handler by resolving `RequestingUserId` → linked `EmployeeId` and comparing to `EmployeeId` in the body); Admin/HR (`IsPrivileged`) may check in on behalf of any employee.

**Request** (`CheckInCommand`):
```json
{
  "employeeId": "guid (required)",
  "checkInAtUtc": "2026-08-01T09:02:00Z (required, must not be default)",
  "notes": "string, optional, max 500 chars",
  "latitude": -90..90 (decimal, required),
  "longitude": -180..180 (decimal, required)
}
```
`deviceInfo`, `ipAddress`, `requestingUserId`, `isPrivileged` are server-set; ignore if sent.

**Response**: `200 OK`
```json
{
  "data": {
    "id": "guid",
    "employeeId": "guid",
    "shiftId": "guid|null",
    "attendanceDate": "2026-08-01T00:00:00Z",
    "checkInAtUtc": "2026-08-01T09:02:00Z",
    "checkOutAtUtc": null,
    "status": "Present",
    "isLateArrival": false,
    "isEarlyLeave": false,
    "totalWorkMinutes": null,
    "notes": null,
    "checkInLatitude": 12.9716,
    "checkInLongitude": 77.5946,
    "checkInAddress": "reverse-geocoded address or null",
    "checkInDeviceInfo": "Mozilla/5.0 ...",
    "checkInIpAddress": "203.0.113.5",
    "checkOutLatitude": null, "checkOutLongitude": null, "checkOutAddress": null,
    "checkOutDeviceInfo": null, "checkOutIpAddress": null,
    "createdAtUtc": "2026-08-01T09:02:01Z",
    "updatedAtUtc": null
  },
  "message": "Checked in successfully.",
  "correlationId": "..."
}
```

**Validation** (`CheckInCommandValidator`):
- `EmployeeId`: not empty
- `CheckInAtUtc`: not equal to `default(DateTime)`
- `Notes`: max length 500
- `Latitude`: inclusive between -90 and 90
- `Longitude`: inclusive between -180 and 180

**Error Codes**:
- `400 VALIDATION_ERROR` — missing/invalid fields, GPS out of range
- `403 FORBIDDEN` — employee attempting to check in on another employee's behalf (`UnauthorizedAccessException: "You can only check in on your own behalf."`)
- `409 CONFLICT` — `"Already checked in for this date."` if a record for that employee/date already has `CheckInAtUtc` set
- `409 CONFLICT` — `"Punch In must originate within {radius}m of {office}."` when the employee's office has geofencing configured (Latitude/Longitude/GeofenceRadiusMeters all set) and the supplied coordinates fall outside the radius
- `401 Unauthorized` — missing/invalid JWT

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/attendance/check-in \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"employeeId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","checkInAtUtc":"2026-08-01T09:02:00Z","latitude":12.9716,"longitude":77.5946}'
```
Response: `200 OK` with the `AttendanceRecordDto` shown above.

**Best Practices**:
- Send the device's true GPS reading (not a cached/stale value) — geofence checks use it directly.
- Calling check-in twice on the same day is idempotent-ish: it updates the existing record's check-in fields rather than erroring, *unless* `CheckInAtUtc` was already set, in which case it 409s.
- Punch Out is never geofenced — only Punch In enforces the office radius.

---

### POST /api/v1/attendance/check-out

**Purpose**: Punch out for the day, closing the attendance record opened by check-in. Computes worked minutes and early-leave flag.

**URL**: `/api/v1/attendance/check-out`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`.

**Authentication**: Any authenticated user; self-only unless caller is Admin/HR (same pattern as check-in).

**Request** (`CheckOutCommand`):
```json
{
  "employeeId": "guid (required)",
  "checkOutAtUtc": "2026-08-01T18:05:00Z (required)",
  "notes": "string, optional, max 500",
  "latitude": -90..90 (required),
  "longitude": -180..180 (required)
}
```

**Response**: `200 OK` — same `AttendanceRecordDto` shape as check-in, now with `checkOutAtUtc`, `totalWorkMinutes`, `isEarlyLeave`, and `checkOut*` fields populated.

**Validation** (`CheckOutCommandValidator`): identical rule set to check-in (EmployeeId not empty; CheckOutAtUtc not default; Notes max 500; Latitude/Longitude range).

**Error Codes**:
- `400 VALIDATION_ERROR`
- `403 FORBIDDEN` — checking out on someone else's behalf without privilege
- `409 CONFLICT` — `"Attendance record not found for this date; check in first."` (no check-in exists yet for that date)
- `409 CONFLICT` — `"Already checked out for this date."`
- `401 Unauthorized`

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/attendance/check-out \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"employeeId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","checkOutAtUtc":"2026-08-01T18:05:00Z","latitude":12.9718,"longitude":77.5950}'
```

**Best Practices**:
- Must be preceded by a check-in on the same UTC calendar date (`checkOutAtUtc.Date` is used to look up the day's record) — clients should surface the 409 clearly as "check in first."
- No geofence restriction applies to checkout, so it is safe to allow off-site punch-outs (e.g., field staff).

---

### GET /api/v1/attendance

**Purpose**: List/search attendance records with pagination and filters — the primary "attendance history" view for self, team, or company-wide reporting.

**URL**: `/api/v1/attendance`

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Any authenticated user. Scope is enforced server-side: plain Employees are always restricted to their own `EmployeeId` (any `employeeId`/`departmentId`/`managerId` filter is ignored for them); Managers see their direct reports (plus themselves) unless `employeeId` is supplied, in which case it must be one of their reports or a 403 is thrown; Admin/HR see everything and may filter freely by `employeeId`/`departmentId`/`managerId`.

**Request** — query params (`GetAttendanceRecordsQuery`):
| Param | Type | Required | Notes |
|---|---|---|---|
| `page` | int | no | default 1 |
| `pageSize` | int | no | default 20; server clamps to 1–100, else falls back to 20 |
| `employeeId` | guid | no | ignored for plain Employee callers |
| `departmentId` | guid | no | Admin/HR only effectively |
| `managerId` | guid | no | Admin/HR only; filters to that manager's direct reports |
| `dateFrom` | date | no | |
| `dateTo` | date | no | |
| `status` | string | no | one of `Present`, `Absent`, `Late`, `HalfDay`, `OnLeave`, `Holiday` |
| `isLateArrival` | bool | no | |
| `isEarlyLeave` | bool | no | |

**Response**: `200 OK`
```json
{
  "data": {
    "data": [ { /* AttendanceRecordDto, see check-in response */ } ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 42,
    "totalPages": 3
  },
  "message": "Request completed successfully."
}
```

**Validation**: No FluentValidation validator found for `GetAttendanceRecordsQuery` — page/pageSize are defensively clamped in the handler rather than rejected.

**Error Codes**:
- `403 FORBIDDEN` — Manager supplies an `employeeId` outside their team (`"You can only view attendance records for your own team."`)
- `401 Unauthorized`

**Examples**:
```bash
curl "https://api.example.com/api/v1/attendance?page=1&pageSize=20&status=Late" \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**:
- Don't rely on client-side filtering by employee for non-privileged users — the server ignores/overrides the filter anyway; use it purely as a UX hint.
- Cap `pageSize` requests to ≤100; anything else silently resets to 20.

---

### GET /api/v1/attendance/{id}

**Purpose**: Fetch a single attendance record's full detail (e.g., to display a punch's GPS/device metadata).

**URL**: `/api/v1/attendance/{id}`

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Admin/HR, the record's own employee, or a Manager whose direct report owns the record. All other callers get a `404` (handler returns `null` rather than distinguishing "not found" from "not yours", to avoid leaking existence).

**Request** — path param `id` (guid, required).

**Response**: `200 OK` with `AttendanceRecordDto` (see check-in above) wrapped in `ApiResponse<T>`.

**Validation**: N/A (no request body).

**Error Codes**:
- `404 Not Found` — record doesn't exist, or caller lacks visibility (indistinguishable by design)
- `401 Unauthorized`

**Examples**:
```bash
curl https://api.example.com/api/v1/attendance/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**: Treat 404 as "not visible to you," not necessarily "doesn't exist" — do not use this endpoint to probe record existence.

---

### POST /api/v1/attendance

**Purpose**: Admin/HR manually creates an attendance record for a day that wasn't captured via check-in/out (e.g., backdated correction, marking a day `Absent`/`Holiday`/`OnLeave`).

**URL**: `/api/v1/attendance`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanManageAttendanceRecords` → roles `Admin`, `HR`.

**Request** (`CreateAttendanceRecordCommand`):
```json
{
  "employeeId": "guid (required)",
  "shiftId": "guid, optional — must reference an existing shift in the caller's company",
  "attendanceDate": "2026-08-01 (required, not default)",
  "checkInAtUtc": "datetime, optional",
  "checkOutAtUtc": "datetime, optional — required to be >= checkInAtUtc when both are present",
  "status": "Present|Absent|Late|HalfDay|OnLeave|Holiday (required, case-insensitive)",
  "notes": "string, optional, max 500"
}
```

**Response**: `201 Created`, `Location` header pointing to `GET /api/v1/attendance/{id}`, body: `ApiResponse<AttendanceRecordDto>` with message `"Attendance record created successfully."`.

**Validation** (`CreateAttendanceRecordCommandValidator`):
- `EmployeeId`: not empty
- `AttendanceDate`: not default
- `Status`: not empty, must parse as `AttendanceStatus` enum
- `Notes`: max 500
- `ShiftId`: if supplied, must resolve via `IAttendanceRepository.GetShiftByIdAsync` scoped to the caller's `CompanyId`, else `"Shift not found."`
- `CheckOutAtUtc >= CheckInAtUtc` when both supplied, else `"Check-out time must be on or after check-in time."`

**Error Codes**:
- `400 VALIDATION_ERROR` — bad status string, invalid shift, checkout before checkin
- `403 Forbidden` — caller not Admin/HR (policy-level, before handler runs)
- `409 CONFLICT` — `"An attendance record already exists for this employee on this date."`
- `401 Unauthorized`

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/attendance \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"employeeId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","attendanceDate":"2026-08-01","status":"Absent"}'
```

**Best Practices**:
- One record per employee per calendar date — check for an existing record (via `GET /api/v1/attendance?employeeId=...&dateFrom=...&dateTo=...`) before creating to avoid the 409.
- Use `UpdateAttendanceRecordCommand` (PUT) instead if a record for that date already exists.

---

### PUT /api/v1/attendance/{id}

**Purpose**: Admin/HR corrects an existing attendance record (times, shift, status, notes) — e.g., after manually reconciling a missed punch.

**URL**: `/api/v1/attendance/{id}`

**Method**: PUT

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanManageAttendanceRecords` → `Admin`, `HR`.

**Request** — path param `id` (guid), body (`UpdateAttendanceRecordCommand`, `Id` overwritten from route):
```json
{
  "shiftId": "guid, optional",
  "checkInAtUtc": "datetime, optional",
  "checkOutAtUtc": "datetime, optional — must be >= checkInAtUtc when both present",
  "status": "Present|Absent|Late|HalfDay|OnLeave|Holiday (required)",
  "notes": "string, optional, max 500"
}
```

**Response**: `200 OK`, `ApiResponse<AttendanceRecordDto>` with the updated record (recomputed `isLateArrival`, `isEarlyLeave`, `totalWorkMinutes`).

**Validation** (`UpdateAttendanceRecordCommandValidator`): `Id` not empty; `Status` required + valid enum; `Notes` max 500; `ShiftId` must exist if supplied; checkout ≥ checkin when both present.

**Error Codes**:
- `400 VALIDATION_ERROR`
- `403 Forbidden` — non Admin/HR caller
- `404 NOT_FOUND` — `"Attendance record {id} not found."`
- `401 Unauthorized`

**Examples**:
```bash
curl -X PUT https://api.example.com/api/v1/attendance/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"status":"Present","checkInAtUtc":"2026-08-01T09:00:00Z","checkOutAtUtc":"2026-08-01T18:00:00Z"}'
```

**Best Practices**: Send the full desired state — this is a full replace of the mutable fields (not a patch); omitted `checkInAtUtc`/`checkOutAtUtc` are set to `null`.

---

### DELETE /api/v1/attendance/{id}

**Purpose**: Soft-delete an erroneous attendance record.

**URL**: `/api/v1/attendance/{id}`

**Method**: DELETE

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Policy `CanManageAttendanceRecords` → `Admin`, `HR`.

**Request** — path param `id` (guid).

**Response**: `204 No Content`.

**Validation**: N/A.

**Error Codes**:
- `404 NOT_FOUND` — `"Attendance record {id} not found."`
- `403 Forbidden` — non Admin/HR
- `401 Unauthorized`

**Examples**:
```bash
curl -X DELETE https://api.example.com/api/v1/attendance/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**: This is a soft delete (`IsDeleted`/audit fields per project conventions) — the record is excluded from future queries but not physically removed.

---

### GET /api/v1/attendance/corrections

**Purpose**: List attendance-correction requests pending or decided — the approval queue for Admin/HR/Managers.

**URL**: `/api/v1/attendance/corrections`

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Policy `CanReviewAttendanceCorrections` → `Admin`, `HR`, `Manager`. Non-Manager, non-Admin/HR callers effectively see nothing (empty page) since the policy itself already excludes plain Employees from reaching this endpoint. Managers are scoped to corrections requested by their direct reports (+ themselves if a report); supplying an `employeeId` outside their team throws 403.

**Request** — query params (`GetAttendanceCorrectionsQuery`):
| Param | Type | Required | Notes |
|---|---|---|---|
| `page` | int | no | default 1 |
| `pageSize` | int | no | default 20, clamped 1–100 |
| `employeeId` | guid | no | must be within Manager's team if not Admin/HR |
| `status` | string | no | `Pending`, `Approved`, `Rejected` |

**Response**: `200 OK`
```json
{
  "data": {
    "data": [
      {
        "id": "guid",
        "attendanceRecordId": "guid",
        "requestedByEmployeeId": "guid",
        "approvedByEmployeeId": "guid|null",
        "requestedCheckInAtUtc": "datetime|null",
        "requestedCheckOutAtUtc": "datetime|null",
        "reason": "string",
        "status": "Pending",
        "decisionAtUtc": "datetime|null",
        "decisionComments": "string|null",
        "createdAtUtc": "datetime"
      }
    ],
    "page": 1, "pageSize": 20, "totalCount": 5, "totalPages": 1
  }
}
```

**Validation**: No FluentValidation validator found for this query.

**Error Codes**:
- `403 FORBIDDEN` — Manager requests `employeeId` outside their team (`"You can only view corrections for your own team."`)
- `403 Forbidden` — caller doesn't hold `Admin`/`HR`/`Manager` role (policy-level)
- `401 Unauthorized`

**Examples**:
```bash
curl "https://api.example.com/api/v1/attendance/corrections?status=Pending" \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**: Filter by `status=Pending` for an approval-queue view; use pagination for large teams.

---

### GET /api/v1/attendance/corrections/{id}

**Purpose**: Fetch a single correction request's detail.

**URL**: `/api/v1/attendance/corrections/{id}`

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Any authenticated user; visible to Admin/HR, the requesting employee, or a Manager whose direct report requested it. Others get 404.

**Request** — path param `id` (guid).

**Response**: `200 OK`, `ApiResponse<AttendanceCorrectionDto>` (shape shown above).

**Validation**: N/A.

**Error Codes**: `404 Not Found` (missing or not visible); `401 Unauthorized`.

**Examples**:
```bash
curl https://api.example.com/api/v1/attendance/corrections/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**: Same "404 means not visible OR doesn't exist" caveat as other detail endpoints.

---

### POST /api/v1/attendance/corrections

**Purpose**: Employee requests a correction (revised check-in/out time) to one of their own attendance records — e.g., forgot to punch out.

**URL**: `/api/v1/attendance/corrections`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Any authenticated user with a linked employee account. `RequestingUserId` is always the caller — corrections are always requested on the caller's own behalf regardless of role (there is no "privileged" bypass here, unlike check-in/out).

**Request** (`CreateAttendanceCorrectionCommand`):
```json
{
  "attendanceRecordId": "guid (required)",
  "requestedCheckInAtUtc": "datetime, optional",
  "requestedCheckOutAtUtc": "datetime, optional",
  "reason": "string (required, max 500)"
}
```
At least one of `requestedCheckInAtUtc` / `requestedCheckOutAtUtc` must be supplied.

**Response**: `201 Created`, `Location` → `GET /api/v1/attendance/corrections/{id}`, body `ApiResponse<AttendanceCorrectionDto>` with `status: "Pending"`.

**Validation** (`CreateAttendanceCorrectionCommandValidator`):
- `AttendanceRecordId`: not empty
- `Reason`: not empty, max 500
- Object-level rule: at least one of `RequestedCheckInAtUtc`/`RequestedCheckOutAtUtc` must have a value, else `"At least one of requestedCheckInAtUtc or requestedCheckOutAtUtc must be provided."`

**Error Codes**:
- `400 VALIDATION_ERROR`
- `403 FORBIDDEN` — caller has no linked employee account, or the target record belongs to a different employee (`"You can only request corrections for your own attendance records."`)
- `404 NOT_FOUND` — `"Attendance record not found."` (thrown via `InvalidOperationException` containing "not found")
- `401 Unauthorized`

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/attendance/corrections \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"attendanceRecordId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","requestedCheckOutAtUtc":"2026-08-01T18:00:00Z","reason":"Forgot to punch out"}'
```

**Best Practices**: There is no self-approval path — corrections must be approved by Admin/HR/Manager via the endpoints below, even for the requester's own manager-approved record.

---

### POST /api/v1/attendance/corrections/{id}/approve

**Purpose**: Approve a pending correction request, applying the requested check-in/out time(s) to the underlying attendance record and recomputing late/early/worked-minutes.

**URL**: `/api/v1/attendance/corrections/{id}/approve`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanReviewAttendanceCorrections` → `Admin`, `HR`, `Manager`.

**Request** — path param `id` (guid); body (`DecisionRequest`, optional):
```json
{ "comments": "string, optional" }
```

**Response**: `204 No Content`.

**Validation**: No FluentValidation validator for `ApproveAttendanceCorrectionCommand`/`DecisionRequest` — `Comments` is unrestricted free text.

**Error Codes**:
- `404 NOT_FOUND` — `"Attendance correction {id} not found."`
- `409 CONFLICT` — `"Only pending corrections can be approved."`
- `409 CONFLICT` — `"You cannot approve your own attendance correction."` (approver's linked employee = requester's employee)
- `403 Forbidden` — caller lacks the policy role
- `401 Unauthorized`

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/attendance/corrections/3fa85f64-5717-4562-b3fc-2c963f66afa6/approve \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"comments":"Confirmed with security logs"}'
```

**Best Practices**: Approving only overwrites the fields the correction actually requested (e.g., a checkout-only correction leaves `CheckInAtUtc` untouched); `TotalWorkMinutes`/late/early flags are always recomputed against the (possibly unchanged) shift.

---

### POST /api/v1/attendance/corrections/{id}/reject

**Purpose**: Reject a pending correction request.

**URL**: `/api/v1/attendance/corrections/{id}/reject`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanReviewAttendanceCorrections` → `Admin`, `HR`, `Manager`.

**Request** — path param `id`; body (`DecisionRequest`, optional): `{ "comments": "string, optional" }`. Note: unlike approve, the handler does not block a Manager from rejecting their own correction (no self-check in `RejectAttendanceCorrectionCommandHandler`) — verify if this asymmetry is intentional before relying on it.

**Response**: `204 No Content`.

**Validation**: None found.

**Error Codes**:
- `404 NOT_FOUND` — `"Attendance correction {id} not found."`
- `409 CONFLICT` — `"Only pending corrections can be rejected."`
- `403 Forbidden` — caller lacks policy role
- `401 Unauthorized`

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/attendance/corrections/3fa85f64-5717-4562-b3fc-2c963f66afa6/reject \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"comments":"No supporting evidence"}'
```

**Best Practices**: Provide `comments` for audit trail even though it's optional — `decisionComments` is surfaced back on the correction/employee-facing views.

---

## Leave API

Controller: `EMS.API.Controllers.LeaveController`, route prefix `api/v1/leave`, class-level `[Authorize]`.

### GET /api/v1/leave/requests

**Purpose**: List/search leave requests — an employee's own history, or (for privileged roles) any employee's, with filters and pagination.

**URL**: `/api/v1/leave/requests`

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Any authenticated user. Non-privileged (plain Employee) callers are always scoped to their own `employeeId`, regardless of the `employeeId` filter passed. Privileged = `Admin`, `HR`, or `Manager` (note: Manager here is **not** restricted to their direct reports at the query layer — `IsPrivilegedLeaveRole()` grants full visibility to any Manager, unlike the Attendance module's team-scoped Manager logic; verify this is intended before treating it as team-scoped).

**Request** — query params (`GetLeavesQuery`):
| Param | Type | Required | Notes |
|---|---|---|---|
| `page` | int | no | default 1 |
| `pageSize` | int | no | default 20, clamped 1–100 |
| `employeeId` | guid | no | ignored for non-privileged callers |
| `leaveTypeId` | guid | no | |
| `year` | int | no | |
| `status` | string | no | `Pending`, `Approved`, `Rejected`, `Cancelled` |

**Response**: `200 OK`
```json
{
  "data": {
    "data": [
      {
        "id": "guid",
        "employeeId": "guid",
        "leaveTypeId": "guid",
        "approverEmployeeId": "guid|null",
        "startDate": "2026-08-10",
        "endDate": "2026-08-12",
        "totalDays": 3,
        "reason": "string|null",
        "status": "Pending",
        "createdAtUtc": "datetime",
        "decisionAtUtc": "datetime|null",
        "decisionComments": "string|null"
      }
    ],
    "page": 1, "pageSize": 20, "totalCount": 12, "totalPages": 1
  }
}
```

**Validation**: No FluentValidation validator found for `GetLeavesQuery`.

**Error Codes**: `401 Unauthorized`. No 403 path — non-privileged filter is silently overridden rather than rejected.

**Examples**:
```bash
curl "https://api.example.com/api/v1/leave/requests?status=Pending&year=2026" \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**: Don't assume the `employeeId` filter is honored for non-admin/HR/manager callers — always read the response's actual `employeeId` field rather than trusting the request echo.

---

### GET /api/v1/leave/requests/{id}

**Purpose**: Fetch a single leave request's detail.

**URL**: `/api/v1/leave/requests/{id}`

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Any authenticated user; non-privileged callers may only fetch their own request (others 404).

**Request** — path param `id` (guid).

**Response**: `200 OK`, `ApiResponse<LeaveRequestDto>` (shape as above).

**Validation**: N/A.

**Error Codes**: `404 Not Found`; `401 Unauthorized`.

**Examples**:
```bash
curl https://api.example.com/api/v1/leave/requests/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Authorization: Bearer $TOKEN"
```

---

### POST /api/v1/leave/requests

**Purpose**: Submit a new leave application.

**URL**: `/api/v1/leave/requests`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Any authenticated user. Non-privileged callers may only apply on their own behalf (`EmployeeId` must match the caller's linked employee).

**Request** (`CreateLeaveRequestCommand`):
```json
{
  "employeeId": "guid (required)",
  "leaveTypeId": "guid (required)",
  "startDate": "2026-08-10 (required, not default)",
  "endDate": "2026-08-12 (required, >= startDate)",
  "totalDays": 3 (decimal, required, > 0),
  "reason": "string, optional"
}
```

**Response**: `201 Created`, `Location` → `GET /api/v1/leave/requests/{id}`, `ApiResponse<LeaveRequestDto>`, `status: "Pending"`, message `"Leave request submitted successfully."`

**Validation** (`CreateLeaveRequestCommandValidator`):
- `EmployeeId`, `LeaveTypeId`: not empty
- `StartDate`: not default
- `EndDate` ≥ `StartDate` — `"End date must be on or after start date."`
- `TotalDays` > 0
- Async check: `TotalDays` must not exceed the employee's available balance for that leave type/year (`repo.GetLeaveBalanceAsync(employeeId, leaveTypeId, startDate.Year)`); if no balance record exists yet, the check passes (no cap enforced) — `"Requested days exceed the available leave balance."`

**Error Codes**:
- `400 VALIDATION_ERROR` — including insufficient balance
- `403 FORBIDDEN` — non-privileged caller applying for someone else (`"You can only apply for leave on your own behalf."`)
- `401 Unauthorized`

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/leave/requests \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"employeeId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","leaveTypeId":"9c858901-8a57-4791-81fe-4c455b099bc9","startDate":"2026-08-10","endDate":"2026-08-12","totalDays":3,"reason":"Family trip"}'
```

**Best Practices**:
- Fetch `GET /api/v1/leave/balances?employeeId=...` first to avoid the 400 balance-exceeded error.
- `totalDays` is client-computed and not cross-validated against `startDate`/`endDate` span server-side (e.g., half-days, weekends/holidays exclusion is not enforced here) — compute it carefully client-side.

---

### PUT /api/v1/leave/requests/{id}

**Purpose**: Edit a still-pending leave request (dates, days, reason) before it's been decided.

**URL**: `/api/v1/leave/requests/{id}`

**Method**: PUT

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Any authenticated user; non-privileged callers may only update their own request.

**Request** — path param `id`; body (`UpdateLeaveRequestCommand`, `Id` set from route):
```json
{
  "startDate": "2026-08-10 (required, not default)",
  "endDate": "2026-08-12 (required, >= startDate)",
  "totalDays": 3 (required, > 0),
  "reason": "string, optional"
}
```

**Response**: `204 No Content`.

**Validation** (`UpdateLeaveRequestCommandValidator`): `StartDate` not default; `EndDate` ≥ `StartDate`; `TotalDays` > 0. Note: unlike create, there is **no** balance re-check on update.

**Error Codes**:
- `400 VALIDATION_ERROR`
- `403 FORBIDDEN` — updating someone else's request without privilege
- `404 NOT_FOUND` — `"Leave request {id} not found."`
- `409 CONFLICT` — `"Only pending leave requests can be updated."`
- `401 Unauthorized`

**Examples**:
```bash
curl -X PUT https://api.example.com/api/v1/leave/requests/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"startDate":"2026-08-11","endDate":"2026-08-13","totalDays":3}'
```

**Best Practices**: Only works while `status == Pending`; once approved/rejected/cancelled, the client must guide the user to cancel-and-reapply instead.

---

### POST /api/v1/leave/requests/{id}/approve

**Purpose**: Approve a pending leave request; deducts the approved days from the employee's leave balance.

**URL**: `/api/v1/leave/requests/{id}/approve`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanApproveLeave` → roles `Admin`, `HR`, `Manager`.

**Request** — path param `id`; body (`DecisionRequest`, optional): `{ "comments": "string, optional" }`.

**Response**: `204 No Content`.

**Validation**: None found for `ApproveLeaveCommand`.

**Error Codes**:
- `404 NOT_FOUND` — `"Leave request {id} not found."`
- `409 CONFLICT` — `"Only pending leave requests can be approved."`
- `409 CONFLICT` — `"You cannot approve your own leave request."`
- `403 Forbidden` — caller lacks `Admin`/`HR`/`Manager` role
- `401 Unauthorized`

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/leave/requests/3fa85f64-5717-4562-b3fc-2c963f66afa6/approve \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"comments":"Approved"}'
```

**Best Practices**: If the employee has no `LeaveBalance` record for that leave type/year, approval still succeeds but no balance deduction happens (the handler silently skips the update when `balance == null`) — ensure balances are provisioned ahead of time via leave-type/balance setup, otherwise `Available` won't reflect usage.

---

### POST /api/v1/leave/requests/{id}/reject

**Purpose**: Reject a pending leave request.

**URL**: `/api/v1/leave/requests/{id}/reject`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanApproveLeave` → `Admin`, `HR`, `Manager`.

**Request** — path param `id`; body (`DecisionRequest`, optional): `{ "comments": "string, optional" }`.

**Response**: `204 No Content`.

**Validation**: None found.

**Error Codes**:
- `404 NOT_FOUND` — `"Leave request {id} not found."`
- `409 CONFLICT` — `"Only pending leave requests can be rejected."`
- `409 CONFLICT` — `"You cannot reject your own leave request."`
- `403 Forbidden`
- `401 Unauthorized`

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/leave/requests/3fa85f64-5717-4562-b3fc-2c963f66afa6/reject \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"comments":"Insufficient coverage on those dates"}'
```

---

### POST /api/v1/leave/requests/{id}/cancel

**Purpose**: Withdraw a still-pending leave request (self-service cancellation before a decision is made).

**URL**: `/api/v1/leave/requests/{id}/cancel`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Any authenticated user; non-privileged callers may only cancel their own request.

**Request** — path param `id` only, no body.

**Response**: `204 No Content`.

**Validation**: N/A.

**Error Codes**:
- `403 FORBIDDEN` — cancelling someone else's request without privilege
- `404 NOT_FOUND` — `"Leave request {id} not found."`
- `409 CONFLICT` — `"Only pending leave requests can be cancelled."`
- `401 Unauthorized`

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/leave/requests/3fa85f64-5717-4562-b3fc-2c963f66afa6/cancel \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**: Cancellation is not available once a request is approved/rejected — no balance is restored by this handler because approval-time deduction hasn't happened yet on a still-pending request.

---

### GET /api/v1/leave/balances

**Purpose**: View an employee's leave balances (per leave type/year) — opening balance, accrued, used, adjusted, and computed available days.

**URL**: `/api/v1/leave/balances`

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Any authenticated user. Non-privileged callers may only fetch their own balances; if a non-privileged caller supplies a non-empty `employeeId` that isn't their own, the handler throws 403 (`"You may only view your own leave balances."`). Privileged (`Admin`/`HR`/`Manager`) may fetch any employee's.

**Request** — query param `employeeId` (guid, required by the query-string binding, though the handler tolerates `Guid.Empty` for non-privileged callers by substituting the caller's own employee id).

**Response**: `200 OK`
```json
{
  "data": [
    {
      "id": "guid",
      "employeeId": "guid",
      "leaveTypeId": "guid",
      "year": 2026,
      "openingBalance": 12,
      "accrued": 6,
      "used": 3,
      "adjusted": 0,
      "available": 15
    }
  ]
}
```

**Validation**: N/A (no command validator; authorization check happens in the handler).

**Error Codes**: `403 FORBIDDEN` (non-privileged caller requesting another employee's balances); `401 Unauthorized`.

**Examples**:
```bash
curl "https://api.example.com/api/v1/leave/balances?employeeId=3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**: Employees can omit `employeeId` (or pass an empty guid) to get their own balances without needing to know their own employee id.

---

## Leave Types API

Controller: `EMS.API.Controllers.LeaveTypeController`, route prefix `api/v1/leave-types`, class-level `[Authorize]`.

### GET /api/v1/leave-types

**Purpose**: List all leave types configured for the caller's company (e.g., Annual, Sick, Unpaid) — used to populate leave-application dropdowns.

**URL**: `/api/v1/leave-types`

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Any authenticated user (no policy restriction on read).

**Request**: none.

**Response**: `200 OK`
```json
{
  "data": [
    {
      "id": "guid",
      "name": "Annual Leave",
      "code": "AL",
      "isPaid": true,
      "requiresApproval": true,
      "annualEntitlementDays": 18,
      "createdAtUtc": "datetime",
      "updatedAtUtc": "datetime|null"
    }
  ]
}
```
Note: this list is scoped to non-deleted leave types for the caller's company (`GetLeaveTypesAsync(companyId)`); soft-deleted types are excluded.

**Validation**: N/A.

**Error Codes**: `401 Unauthorized`.

**Examples**:
```bash
curl https://api.example.com/api/v1/leave-types -H "Authorization: Bearer $TOKEN"
```

---

### GET /api/v1/leave-types/{id}

**Purpose**: Fetch a single leave type's detail/config.

**URL**: `/api/v1/leave-types/{id}`

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Any authenticated user.

**Request** — path param `id` (guid).

**Response**: `200 OK`, `ApiResponse<LeaveTypeDto>`.

**Error Codes**: `404 Not Found`; `401 Unauthorized`.

**Examples**:
```bash
curl https://api.example.com/api/v1/leave-types/9c858901-8a57-4791-81fe-4c455b099bc9 \
  -H "Authorization: Bearer $TOKEN"
```

---

### POST /api/v1/leave-types

**Purpose**: Create a new leave type (Admin/HR configuration task).

**URL**: `/api/v1/leave-types`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanManageLeaveTypes` → roles `Admin`, `HR`.

**Request** (`CreateLeaveTypeCommand`):
```json
{
  "name": "string (required, max 100)",
  "code": "string, optional, max 50 — must be unique within the company if supplied",
  "isPaid": true,
  "requiresApproval": true,
  "annualEntitlementDays": 18 (decimal, optional, >= 0 if supplied)
}
```

**Response**: `201 Created`, `Location` → `GET /api/v1/leave-types/{id}`, `ApiResponse<LeaveTypeDto>`, message `"Leave type created successfully."`

**Validation** (`CreateLeaveTypeCommandValidator`):
- `Name`: not empty, max 100
- `Code`: max 50; async uniqueness check per company (`LeaveTypeCodeExistsAsync`) — `"Leave type code already exists."`
- `AnnualEntitlementDays`: ≥ 0 when supplied

**Error Codes**:
- `400 VALIDATION_ERROR`
- `409 CONFLICT` — duplicate code (the handler also re-checks and throws `InvalidOperationException` even though the validator should already catch this — belt-and-braces)
- `403 Forbidden` — non Admin/HR caller
- `401 Unauthorized`

Note: the controller only documents `409` via `[ProducesResponseType]`, but `400` is also realistic given the validator — both are in play.

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/leave-types \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Sick Leave","code":"SL","isPaid":true,"requiresApproval":false,"annualEntitlementDays":12}'
```

**Best Practices**: `code` is optional but should be a stable short identifier if used for integrations/reports — duplicates are rejected at the company scope, not globally.

---

### PUT /api/v1/leave-types/{id}

**Purpose**: Update an existing leave type's configuration.

**URL**: `/api/v1/leave-types/{id}`

**Method**: PUT

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanManageLeaveTypes` → `Admin`, `HR`.

**Request** — path param `id`; body (`UpdateLeaveTypeCommand`); controller validates `id == cmd.Id` and returns 400 `ID_MISMATCH` otherwise:
```json
{
  "id": "guid (must match route id)",
  "name": "string (required, max 100)",
  "code": "string, optional, max 50, unique per company excluding this record",
  "isPaid": true,
  "requiresApproval": true,
  "annualEntitlementDays": 18
}
```

**Response**: `200 OK`, `ApiResponse<LeaveTypeDto>`.

**Validation** (`UpdateLeaveTypeCommandValidator`): same as create, but the uniqueness check excludes the record's own `Id`.

**Error Codes**:
- `400 ID_MISMATCH` — `{ "status": 400, "code": "ID_MISMATCH", "message": "Route id does not match body id." }` (raised directly by the controller, not the exception middleware)
- `400 VALIDATION_ERROR`
- `404 NOT_FOUND` — `"Leave type {id} not found."`
- `409 CONFLICT` — duplicate code (handler-level re-check)
- `403 Forbidden`
- `401 Unauthorized`

**Examples**:
```bash
curl -X PUT https://api.example.com/api/v1/leave-types/9c858901-8a57-4791-81fe-4c455b099bc9 \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"id":"9c858901-8a57-4791-81fe-4c455b099bc9","name":"Sick Leave","isPaid":true,"requiresApproval":false,"annualEntitlementDays":14}'
```

**Best Practices**: Always include `id` in the body matching the URL — omitting/mismatching it triggers the `ID_MISMATCH` 400 before any handler logic runs.

---

### DELETE /api/v1/leave-types/{id}

**Purpose**: Soft-delete a leave type (e.g., retiring an unused policy).

**URL**: `/api/v1/leave-types/{id}`

**Method**: DELETE

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Policy `CanManageLeaveTypes` → `Admin`, `HR`.

**Request** — path param `id`.

**Response**: `204 No Content`.

**Error Codes**: `404 NOT_FOUND` — `"Leave type {id} not found."`; `403 Forbidden`; `401 Unauthorized`.

**Examples**:
```bash
curl -X DELETE https://api.example.com/api/v1/leave-types/9c858901-8a57-4791-81fe-4c455b099bc9 \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**: Deleting a leave type does not appear to cascade-check for existing leave requests/balances referencing it — deleting a type in active use may orphan historical records from the type list view; confirm downstream impact before deleting a type with existing usage.

---

### POST /api/v1/leave-types/{id}/restore

**Purpose**: Undo a soft-delete on a leave type.

**URL**: `/api/v1/leave-types/{id}/restore`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Policy `CanManageLeaveTypes` → `Admin`, `HR`.

**Request** — path param `id`, no body.

**Response**: `204 No Content`.

**Validation**: N/A.

**Error Codes**:
- `404 NOT_FOUND` — `"Leave type {id} not found."` (looked up including soft-deleted rows)
- `409 CONFLICT` — `"Leave type is not deleted and cannot be restored."`
- `403 Forbidden`
- `401 Unauthorized`

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/leave-types/9c858901-8a57-4791-81fe-4c455b099bc9/restore \
  -H "Authorization: Bearer $TOKEN"
```

---

## Shifts API

Controller: `EMS.API.Controllers.ShiftController`, route prefix `api/v1/shifts` (plus two routes rooted at `~/api/v1/employees/{employeeId}/shifts` via `[HttpGet("~/...")]`-style absolute overrides), class-level `[Authorize]`.

### GET /api/v1/shifts

**Purpose**: List all shift definitions (e.g., "Morning 9–6", "Night 10pm–6am") configured for the caller's company.

**URL**: `/api/v1/shifts`

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Any authenticated user.

**Request**: none.

**Response**: `200 OK`
```json
{
  "data": [
    {
      "id": "guid",
      "name": "Morning Shift",
      "startTime": "09:00:00",
      "endTime": "18:00:00",
      "graceMinutes": 10,
      "isNightShift": false
    }
  ]
}
```
`startTime`/`endTime` serialize as `TimeSpan` (`"HH:mm:ss"` by default with System.Text.Json).

**Error Codes**: `401 Unauthorized`.

**Examples**:
```bash
curl https://api.example.com/api/v1/shifts -H "Authorization: Bearer $TOKEN"
```

---

### GET /api/v1/shifts/{id}

**Purpose**: Fetch a single shift's definition.

**URL**: `/api/v1/shifts/{id}`

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Any authenticated user.

**Request** — path param `id`.

**Response**: `200 OK`, `ApiResponse<ShiftDto>`.

**Error Codes**: `404 Not Found`; `401 Unauthorized`.

**Examples**:
```bash
curl https://api.example.com/api/v1/shifts/6b9a2e0e-6a2e-4b7e-8a1a-6f4c2a3e9c11 \
  -H "Authorization: Bearer $TOKEN"
```

---

### POST /api/v1/shifts

**Purpose**: Define a new shift template.

**URL**: `/api/v1/shifts`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanManageShifts` → roles `Admin`, `HR`.

**Request** (`CreateShiftCommand`):
```json
{
  "name": "string (required, max 150)",
  "startTime": "09:00:00 (TimeSpan)",
  "endTime": "18:00:00 (TimeSpan)",
  "graceMinutes": 10 (int, required, >= 0),
  "isNightShift": false
}
```

**Response**: `201 Created`, `Location` → `GET /api/v1/shifts/{id}`, `ApiResponse<ShiftDto>`, message `"Shift created successfully."`

**Validation** (`CreateShiftCommandValidator`): `Name` not empty, max 150; `GraceMinutes` ≥ 0. Note: no rule validates `EndTime` relative to `StartTime` — night shifts (end time "before" start time-of-day) are apparently expected and handled by the `IsNightShift` flag rather than by time-ordering validation.

**Error Codes**: `400 VALIDATION_ERROR`; `403 Forbidden`; `401 Unauthorized`.

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/shifts \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Morning Shift","startTime":"09:00:00","endTime":"18:00:00","graceMinutes":10,"isNightShift":false}'
```

**Best Practices**: Set `isNightShift: true` for shifts spanning midnight — be aware `AttendanceCalculator` compares only time-of-day for late/early detection, which can misfire across midnight for night shifts (a known limitation noted in code comments); do not rely on late/early flags being perfectly accurate for night-shift employees.

---

### PUT /api/v1/shifts/{id}

**Purpose**: Update a shift template's timing/grace configuration.

**URL**: `/api/v1/shifts/{id}`

**Method**: PUT

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanManageShifts` → `Admin`, `HR`.

**Request** — path param `id`; body (`UpdateShiftCommand`, `Id` set from route):
```json
{
  "name": "string (required, max 150)",
  "startTime": "09:00:00",
  "endTime": "18:00:00",
  "graceMinutes": 10,
  "isNightShift": false
}
```

**Response**: `200 OK`, `ApiResponse<ShiftDto>`.

**Validation** (`UpdateShiftCommandValidator`): `Id` not empty; `Name` not empty, max 150; `GraceMinutes` ≥ 0.

**Error Codes**: `400 VALIDATION_ERROR`; `404 NOT_FOUND` — `"Shift {id} not found."`; `403 Forbidden`; `401 Unauthorized`.

**Examples**:
```bash
curl -X PUT https://api.example.com/api/v1/shifts/6b9a2e0e-6a2e-4b7e-8a1a-6f4c2a3e9c11 \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Morning Shift","startTime":"09:30:00","endTime":"18:30:00","graceMinutes":15,"isNightShift":false}'
```

---

### DELETE /api/v1/shifts/{id}

**Purpose**: Soft-delete a shift template.

**URL**: `/api/v1/shifts/{id}`

**Method**: DELETE

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Policy `CanManageShifts` → `Admin`, `HR`.

**Request** — path param `id`.

**Response**: `204 No Content`.

**Error Codes**: `404 NOT_FOUND` — `"Shift {id} not found."`; `403 Forbidden`; `401 Unauthorized`.

**Examples**:
```bash
curl -X DELETE https://api.example.com/api/v1/shifts/6b9a2e0e-6a2e-4b7e-8a1a-6f4c2a3e9c11 \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**: No check exists here for active `EmployeeShift` assignments still referencing the shift — deleting a shift in use will not fail loudly; downstream attendance records store `ShiftId` as a snapshot foreign key, so verify no active assignments before deleting.

---

### GET /api/v1/employees/{employeeId}/shifts

**Purpose**: List an employee's shift assignment history (which shift template applies over which date ranges).

**URL**: `/api/v1/employees/{employeeId}/shifts` (registered on `ShiftController` via an absolute route override, not under `/api/v1/shifts`)

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Any authenticated user; visible to Admin/HR, the employee themselves, or their Manager (direct-report check). Others get `403 UnauthorizedAccessException`.

**Request** — path param `employeeId` (guid).

**Response**: `200 OK`
```json
{
  "data": [
    {
      "id": "guid",
      "employeeId": "guid",
      "shiftId": "guid",
      "effectiveFrom": "2026-01-01",
      "effectiveTo": "2026-12-31"
    }
  ]
}
```

**Validation**: N/A.

**Error Codes**: `403 FORBIDDEN` — `"You can only view your own or your team's shift assignments."`; `401 Unauthorized`.

**Examples**:
```bash
curl https://api.example.com/api/v1/employees/3fa85f64-5717-4562-b3fc-2c963f66afa6/shifts \
  -H "Authorization: Bearer $TOKEN"
```

---

### POST /api/v1/employees/{employeeId}/shifts

**Purpose**: Assign a shift template to an employee for a date range (e.g., onboarding, shift change).

**URL**: `/api/v1/employees/{employeeId}/shifts`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanManageShifts` → `Admin`, `HR`.

**Request** — path param `employeeId` (overwrites body's `EmployeeId`); body (`AssignEmployeeShiftCommand`):
```json
{
  "shiftId": "guid (required)",
  "effectiveFrom": "2026-08-01 (required, not default)",
  "effectiveTo": "2026-12-31, optional — must be >= effectiveFrom if supplied"
}
```

**Response**: `201 Created`, `Location` → `GET /api/v1/employees/{employeeId}/shifts`, `ApiResponse<EmployeeShiftDto>`, message `"Shift assigned successfully."`

**Validation** (`AssignEmployeeShiftCommandValidator`): `EmployeeId`, `ShiftId` not empty; `EffectiveFrom` not default; `EffectiveTo` ≥ `EffectiveFrom` when supplied.

**Error Codes**: `400 VALIDATION_ERROR`; `404/409` — `"Shift not found."` is thrown as a plain `InvalidOperationException` without the word "not found" matching case-insensitively... actually it does contain "not found" so it maps to `404 NOT_FOUND`; `403 Forbidden`; `401 Unauthorized`.

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/employees/3fa85f64-5717-4562-b3fc-2c963f66afa6/shifts \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"shiftId":"6b9a2e0e-6a2e-4b7e-8a1a-6f4c2a3e9c11","effectiveFrom":"2026-08-01"}'
```

**Best Practices**: No overlap validation exists between concurrent `EmployeeShift` assignments for the same employee — the API will happily create overlapping date ranges; client should check existing assignments (via GET) before creating a new one if overlap matters to your business process.

---

### PUT /api/v1/employees/{employeeId}/shifts/{assignmentId}

**Purpose**: Change an employee's existing shift assignment (different shift and/or date range).

**URL**: `/api/v1/employees/{employeeId}/shifts/{assignmentId}`

**Method**: PUT

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanManageShifts` → `Admin`, `HR`.

**Request** — path params `employeeId`, `assignmentId`; body (`UpdateEmployeeShiftCommand`, both ids overwritten from route):
```json
{
  "shiftId": "guid (required)",
  "effectiveFrom": "2026-08-01 (required, not default)",
  "effectiveTo": "2026-12-31, optional, >= effectiveFrom"
}
```

**Response**: `200 OK`, `ApiResponse<EmployeeShiftDto>`.

**Validation** (`UpdateEmployeeShiftCommandValidator`): `EmployeeId`, `AssignmentId`, `ShiftId` not empty; `EffectiveFrom` not default; `EffectiveTo` ≥ `EffectiveFrom` when supplied.

**Error Codes**: `400 VALIDATION_ERROR`; `404 NOT_FOUND` — `"Shift assignment {assignmentId} not found."` (also thrown if the assignment exists but doesn't belong to `employeeId`) or `"Shift not found."`; `403 Forbidden`; `401 Unauthorized`.

**Examples**:
```bash
curl -X PUT https://api.example.com/api/v1/employees/3fa85f64-5717-4562-b3fc-2c963f66afa6/shifts/7d2f1a3b-9e4c-4a2d-8b1e-2f3a4b5c6d7e \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"shiftId":"6b9a2e0e-6a2e-4b7e-8a1a-6f4c2a3e9c11","effectiveFrom":"2026-09-01"}'
```

---

### DELETE /api/v1/employees/{employeeId}/shifts/{assignmentId}

**Purpose**: End (soft-delete) an employee's shift assignment.

**URL**: `/api/v1/employees/{employeeId}/shifts/{assignmentId}`

**Method**: DELETE

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Policy `CanManageShifts` → `Admin`, `HR`.

**Request** — path params `employeeId`, `assignmentId`.

**Response**: `204 No Content`.

**Error Codes**: `404 NOT_FOUND` — `"Shift assignment {assignmentId} not found."`; `403 Forbidden`; `401 Unauthorized`.

**Examples**:
```bash
curl -X DELETE https://api.example.com/api/v1/employees/3fa85f64-5717-4562-b3fc-2c963f66afa6/shifts/7d2f1a3b-9e4c-4a2d-8b1e-2f3a4b5c6d7e \
  -H "Authorization: Bearer $TOKEN"
```

---

## Holidays API

Controller: `EMS.API.Controllers.HolidayController`, route prefix `api/v1/holidays`, class-level `[Authorize]`.

### GET /api/v1/holidays

**Purpose**: List the company holiday calendar, optionally filtered by office location, year, and whether a holiday is optional (floater).

**URL**: `/api/v1/holidays`

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Any authenticated user.

**Request** — query params (`GetHolidaysQuery`):
| Param | Type | Required | Notes |
|---|---|---|---|
| `officeLocationId` | guid | no | filters to holidays for that office (plus company-wide ones, depending on repository implementation — not verified here) |
| `year` | int | no | |
| `isOptional` | bool | no | |

**Response**: `200 OK`
```json
{
  "data": [
    {
      "id": "guid",
      "name": "Independence Day",
      "officeLocationId": "guid|null",
      "holidayDate": "2026-08-15",
      "isOptional": false,
      "createdAtUtc": "datetime",
      "updatedAtUtc": "datetime|null"
    }
  ]
}
```

**Validation**: No FluentValidation validator for `GetHolidaysQuery`.

**Error Codes**: `401 Unauthorized`.

**Examples**:
```bash
curl "https://api.example.com/api/v1/holidays?year=2026" -H "Authorization: Bearer $TOKEN"
```

---

### GET /api/v1/holidays/{id}

**Purpose**: Fetch a single holiday's detail.

**URL**: `/api/v1/holidays/{id}`

**Method**: GET

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Any authenticated user.

**Request** — path param `id`.

**Response**: `200 OK`, `ApiResponse<HolidayDto>`.

**Error Codes**: `404 Not Found`; `401 Unauthorized`.

**Examples**:
```bash
curl https://api.example.com/api/v1/holidays/5e6f7a8b-9c0d-4e1f-8a2b-3c4d5e6f7a8b \
  -H "Authorization: Bearer $TOKEN"
```

---

### POST /api/v1/holidays

**Purpose**: Add a new holiday to the calendar.

**URL**: `/api/v1/holidays`

**Method**: POST

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanManageHolidays` → roles `Admin`, `HR`.

**Request** (`CreateHolidayCommand`):
```json
{
  "name": "string (required, max 150)",
  "officeLocationId": "guid, optional — null means company-wide/all offices",
  "holidayDate": "2026-08-15 (required, not default)",
  "isOptional": false
}
```

**Response**: `201 Created`, `Location` → `GET /api/v1/holidays/{id}`, `ApiResponse<HolidayDto>`, message `"Holiday created successfully."`

**Validation** (`CreateHolidayCommandValidator`): `Name` not empty, max 150; `HolidayDate` not default. Note: no duplicate-date or office-scope uniqueness check — creating the same holiday twice for the same date/office is not blocked.

**Error Codes**: `400 VALIDATION_ERROR`; `403 Forbidden`; `401 Unauthorized`.

**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/holidays \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Independence Day","holidayDate":"2026-08-15","isOptional":false}'
```

**Best Practices**: Omit `officeLocationId` for holidays that apply company-wide; set it only for office/region-specific observances.

---

### PUT /api/v1/holidays/{id}

**Purpose**: Update an existing holiday's name/date/scope.

**URL**: `/api/v1/holidays/{id}`

**Method**: PUT

**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication**: Policy `CanManageHolidays` → `Admin`, `HR`.

**Request** — path param `id`; body (`UpdateHolidayCommand`); controller checks `id == cmd.Id`, returns `400 ID_MISMATCH` otherwise:
```json
{
  "id": "guid (must match route id)",
  "name": "string (required, max 150)",
  "officeLocationId": "guid, optional",
  "holidayDate": "2026-08-15 (required, not default)",
  "isOptional": false
}
```

**Response**: `200 OK`, `ApiResponse<HolidayDto>`.

**Validation** (`UpdateHolidayCommandValidator`): `Name` not empty, max 150; `HolidayDate` not default.

**Error Codes**: `400 ID_MISMATCH`; `400 VALIDATION_ERROR`; `404 NOT_FOUND` — `"Holiday {id} not found."`; `403 Forbidden`; `401 Unauthorized`.

**Examples**:
```bash
curl -X PUT https://api.example.com/api/v1/holidays/5e6f7a8b-9c0d-4e1f-8a2b-3c4d5e6f7a8b \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"id":"5e6f7a8b-9c0d-4e1f-8a2b-3c4d5e6f7a8b","name":"Independence Day (Observed)","holidayDate":"2026-08-17","isOptional":false}'
```

---

### DELETE /api/v1/holidays/{id}

**Purpose**: Remove a holiday from the calendar (soft delete).

**URL**: `/api/v1/holidays/{id}`

**Method**: DELETE

**Headers**: `Authorization: Bearer <token>`

**Authentication**: Policy `CanManageHolidays` → `Admin`, `HR`.

**Request** — path param `id`.

**Response**: `204 No Content`.

**Error Codes**: `404 NOT_FOUND` — `"Holiday {id} not found."`; `403 Forbidden`; `401 Unauthorized`.

**Examples**:
```bash
curl -X DELETE https://api.example.com/api/v1/holidays/5e6f7a8b-9c0d-4e1f-8a2b-3c4d5e6f7a8b \
  -H "Authorization: Bearer $TOKEN"
```

---

## Cross-cutting notes / uncertainties worth flagging to the team

1. **No 401/403 body contract confirmed.** All `401`/`403` outcomes above that originate from ASP.NET Core's `[Authorize]`/policy pipeline (i.e., failing *before* a MediatR handler runs) were not traced to a custom `IAuthorizationMiddlewareResultHandler` — only handler-thrown `UnauthorizedAccessException` is confirmed to produce the `ApiErrorResponse` 403 shape via `ExceptionHandlingMiddleware`. Framework-level 401/403 responses may have a different (or empty) body; verify against `Program.cs` JWT/authorization configuration if the client needs to parse these bodies.
2. **`AdjustLeaveBalanceCommand`** exists in `EMS.Application.Features.Leave.Commands` with a validator and handler but has **no controller endpoint** in any of the five controllers reviewed (confirmed via search — not referenced in `EMS.API/Controllers`). It is not documented here as an endpoint since it isn't reachable via HTTP today.
3. **Leave module's "privileged" Manager role is not team-scoped** the way Attendance's Manager role is — `LeaveController.IsPrivilegedLeaveRole()` grants a Manager full cross-employee visibility/action rights on `GET /leave/requests`, `POST /leave/requests` (apply on behalf), `PUT`, and `cancel`, with no direct-report check. This is inconsistent with `AttendanceController`, where Managers are always constrained to their own team. Flagged as worth confirming against `requirements.md`/`api-specification.md` rather than assuming it's a bug.
4. Several list/query endpoints (`GetAttendanceRecordsQuery`, `GetAttendanceCorrectionsQuery`, `GetLeavesQuery`, `GetHolidaysQuery`) have **no FluentValidation validator** — `page`/`pageSize` are defensively clamped in the handler instead of validated, and there's no length/format check on `status` filter strings (an invalid status string is simply treated as a non-matching filter rather than a 400).
# Payroll, Reimbursement & Assets API Reference

All responses are wrapped in a standard envelope unless noted:

- Success: `ApiResponse<T>` — `{ "data": T, "message": string, "correlationId": string }`
- Error: `ApiErrorResponse` — `{ "status": number, "code": string, "message": string, "errors": object|null, "correlationId": string }`

Global exception → status mapping (from `EMS.API/Middleware/ExceptionHandlingMiddleware.cs`):

| Exception | Status | Code |
|---|---|---|
| `FluentValidation.ValidationException` | 400 | `VALIDATION_ERROR` |
| `InvalidOperationException` with message containing "not found" | 404 | `NOT_FOUND` |
| `InvalidOperationException` (any other message) | 409 | `CONFLICT` |
| `UnauthorizedAccessException` | 403 | `FORBIDDEN` |
| Anything else | 500 | `INTERNAL_ERROR` |

All three controllers require a valid JWT (`Authorization: Bearer <token>`) — the `[Authorize]` attribute is applied at the controller level in every case.

---

## Payroll API

Controller: `EMS.API/Controllers/PayrollController.cs`. Base route: `[Route("api/v1/payroll")]`, class-level `[Authorize]`.

Authorization policies in effect (from `Program.cs`):
- `CanManagePayroll` → roles `Admin, HR`
- `CanApprovePayroll` → role `Admin` only

### Trigger Payroll Processing

**Purpose** — Kicks off asynchronous-style payroll processing for a pay period: computes basic + allowances + bonus + overtime − deductions for every active employee with an effective salary structure, folds in approved-and-unprocessed reimbursements, generates a payslip PDF per employee, and marks those reimbursements `Paid`.

**URL** — `/api/v1/payroll/process`

**Method** — POST

**Headers** — `Authorization: Bearer <token>` (required), `Content-Type: application/json`

**Authentication** — Policy `CanManagePayroll` (roles: `Admin`, `HR`)

**Request**
```json
{
  "periodStart": "2026-07-01T00:00:00Z",
  "periodEnd": "2026-07-31T23:59:59Z",
  "adjustments": [
    { "employeeId": "3f7b...guid", "bonusAmount": 500.00, "overtimeAmount": null }
  ]
}
```
- `periodStart` (DateTime, required)
- `periodEnd` (DateTime, required)
- `adjustments` (array, optional) — per-employee overrides. Employees not listed get `bonus = 0` and auto-calculated overtime (from Attendance vs. shift). `overtimeAmount`, if supplied, always overrides the auto-calculation for that employee.
  - `employeeId` (guid, required per entry)
  - `bonusAmount` (decimal, optional, ≥ 0)
  - `overtimeAmount` (decimal, optional, ≥ 0)

Note: `processedBy` is NOT client-supplied — the controller overwrites it from the caller's JWT identity (`ProcessPayrollCommand.ProcessedBy = GetCurrentUserId()`).

**Response** — `202 Accepted`
```json
{
  "data": { "payrollRunId": "b1e4...guid" },
  "message": "Payroll processing started.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```
Note: despite the 202 status code and "started" wording, the handler (`ProcessPayrollCommandHandler`) actually runs synchronously within the request — the run is `Completed` by the time the response is returned.

**Validation** (`ProcessPayrollCommandValidator`)
- `PeriodStart` required (not default DateTime)
- `PeriodEnd` required (not default DateTime)
- `PeriodStart` ≤ `PeriodEnd`
- `PeriodEnd` ≤ `DateTime.UtcNow` (cannot process a period that hasn't ended yet)
- Each `Adjustments[].EmployeeId` not empty
- Each `Adjustments[].BonusAmount` ≥ 0 when supplied
- Each `Adjustments[].OvertimeAmount` ≥ 0 when supplied

**Error Codes**
- `400 VALIDATION_ERROR` — e.g. `periodEnd` before `periodStart`, or `periodEnd` in the future
- `401` — missing/invalid JWT
- `403 FORBIDDEN` — caller lacks `Admin`/`HR` role
- No explicit duplicate-run guard exists in the handler — running `process` twice for the same period creates a second `PayrollRun` with its own payslip set (uncertain/likely gap; not enforced in code as of this read)

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/payroll/process \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"periodStart":"2026-07-01T00:00:00Z","periodEnd":"2026-07-31T23:59:59Z"}'
```
```json
{
  "data": { "payrollRunId": "b1e4c2d3-1234-4a5b-9c8d-abcdef123456" },
  "message": "Payroll processing started.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Best Practices**
- Run `POST /dry-run` first with the same period/adjustments to preview totals before committing — `process` persists payslips and marks reimbursements `Paid` immediately (not reversible via the API).
- Because there is no server-side idempotency/duplicate-run check, the client is responsible for not double-submitting the same period (e.g. disable the submit button after first click, confirm no existing run for the period via `GET /runs` first).
- Individual PDF generation failures are swallowed and logged as warnings, not surfaced to the caller — check `GET /runs/{id}` afterward and inspect `payslips[].hasDocument` to confirm PDFs were generated.

---

### Preview Payroll (Dry Run)

**Purpose** — Computes the same payslip figures `process` would produce, for a period and optional adjustments, without persisting a `PayrollRun`, any `Payslip`, or marking any reimbursement as processed. Used to preview before committing.

**URL** — `/api/v1/payroll/dry-run`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication** — Policy `CanManagePayroll` (roles: `Admin`, `HR`)

**Request** — same shape as `ProcessPayrollCommand` minus `processedBy`:
```json
{
  "periodStart": "2026-07-01T00:00:00Z",
  "periodEnd": "2026-07-31T23:59:59Z",
  "adjustments": [
    { "employeeId": "3f7b...guid", "bonusAmount": 500.00, "overtimeAmount": null }
  ]
}
```

**Response** — `200 OK`
```json
{
  "data": [
    {
      "employeeId": "3f7b...guid",
      "basic": 5000.00,
      "totalAllowances": 800.00,
      "totalDeductions": 300.00,
      "totalReimbursements": 120.50,
      "totalBonus": 500.00,
      "totalOvertime": 187.50,
      "overtimeHours": 5.00,
      "grossPay": 6487.50,
      "netPay": 6307.50
    }
  ],
  "message": "Request completed successfully.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** (`DryRunPayrollQueryValidator`)
- `PeriodStart` ≤ `PeriodEnd`
- Each `Adjustments[].EmployeeId` not empty
- Each `Adjustments[].BonusAmount` ≥ 0 when supplied
- Each `Adjustments[].OvertimeAmount` ≥ 0 when supplied
- Note: unlike `process`, there is no "PeriodEnd must not be in the future" rule here, and `PeriodStart`/`PeriodEnd` are not required to be non-default.

**Error Codes**
- `400 VALIDATION_ERROR` — `periodStart` after `periodEnd`, invalid adjustment entries
- `401` / `403 FORBIDDEN`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/payroll/dry-run \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"periodStart":"2026-07-01T00:00:00Z","periodEnd":"2026-07-31T23:59:59Z"}'
```

**Best Practices**
- Always dry-run before `process` for a new period — there's no undo for a real run.
- Reimbursement totals reflect currently-approved-and-unprocessed claims at the moment of the call; if approvals happen between dry-run and process, the real totals may differ.

---

### List Payroll Runs

**Purpose** — Returns all payroll runs (summary + nested payslips) for the run history/admin view.

**URL** — `/api/v1/payroll/runs`

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManagePayroll` (roles: `Admin`, `HR`)

**Request** — no parameters

**Response** — `200 OK`
```json
{
  "data": [
    {
      "id": "b1e4...guid",
      "periodStart": "2026-07-01T00:00:00Z",
      "periodEnd": "2026-07-31T23:59:59Z",
      "processedAtUtc": "2026-08-01T09:00:00Z",
      "processedBy": "u1...guid",
      "status": "Completed",
      "updatedAtUtc": null,
      "updatedBy": null,
      "payslipCount": 42,
      "totalNetPay": 265000.00,
      "payslips": [ /* PayslipDto[] — see Get Payroll Run by ID */ ]
    }
  ],
  "message": "Request completed successfully.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```
`status` is a plain string, one of `Processing`, `Completed`, `Approved` (set by the handlers — not a strongly-typed enum in the DTO).

**Validation** — none (no request body/params).

**Error Codes** — `401` / `403 FORBIDDEN`

**Examples**
```bash
curl https://api.example.com/api/v1/payroll/runs -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- This returns full payslip arrays per run — for large employee counts/many runs, prefer `GET /runs/{id}` when only one run's detail is needed to avoid an oversized payload.

---

### Get Payroll Run by ID

**Purpose** — Fetch one payroll run's detail, including its full payslip list, e.g. for a run detail/review screen.

**URL** — `/api/v1/payroll/runs/{id}`

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManagePayroll` (roles: `Admin`, `HR`)

**Request** — path param `id` (guid, required)

**Response** — `200 OK`
```json
{
  "data": {
    "id": "b1e4...guid",
    "periodStart": "2026-07-01T00:00:00Z",
    "periodEnd": "2026-07-31T23:59:59Z",
    "processedAtUtc": "2026-08-01T09:00:00Z",
    "processedBy": "u1...guid",
    "status": "Completed",
    "updatedAtUtc": null,
    "updatedBy": null,
    "payslipCount": 42,
    "totalNetPay": 265000.00,
    "payslips": [
      {
        "id": "c2d5...guid",
        "payrollRunId": "b1e4...guid",
        "employeeId": "3f7b...guid",
        "basic": 5000.00,
        "totalAllowances": 800.00,
        "totalDeductions": 300.00,
        "totalReimbursements": 120.50,
        "totalBonus": 500.00,
        "totalOvertime": 187.50,
        "overtimeHours": 5.00,
        "grossPay": 6487.50,
        "netPay": 6307.50,
        "generatedAtUtc": "2026-08-01T09:00:12Z",
        "hasDocument": true
      }
    ]
  },
  "message": "Request completed successfully.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** — route constraint `{id:guid}` only.

**Error Codes**
- `404` — no `PayrollRunDto` body (controller returns bare `NotFound()`, not `ApiErrorResponse`) when the run doesn't exist
- `401` / `403 FORBIDDEN`

**Examples**
```bash
curl https://api.example.com/api/v1/payroll/runs/b1e4c2d3-1234-4a5b-9c8d-abcdef123456 \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Poll this endpoint after `POST /process` to confirm final `status` and per-payslip `hasDocument` (PDF generation failures are silent at the process step).

---

### Approve Payroll Run

**Purpose** — Marks a `Completed` payroll run as `Approved`, recording the approver. This is a terminal, one-way state transition used as a sign-off step before/after disbursement.

**URL** — `/api/v1/payroll/runs/{id}/approve`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanApprovePayroll` (role: `Admin` only — stricter than the `CanManagePayroll` policy used elsewhere in this controller)

**Request** — path param `id` (guid, required). No body. `approvedBy` is set server-side from the caller's JWT identity.

**Response** — `204 No Content`

**Validation** (`ApprovePayrollRunCommandValidator`)
- `PayrollRunId` not empty
- `ApprovedBy` not empty
(Both are populated by the controller, not client input, so these rules effectively can't fail via the HTTP surface.)

**Error Codes**
- `404 NOT_FOUND` — "Payroll run not found." (run does not exist)
- `409 CONFLICT` — "Payroll run has already been approved." (status already `Approved`)
- `409 CONFLICT` — "Only completed payroll runs can be approved." (status is `Processing`, not yet `Completed`)
- `401` / `403 FORBIDDEN` — caller is not `Admin`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/payroll/runs/b1e4c2d3-1234-4a5b-9c8d-abcdef123456/approve \
  -H "Authorization: Bearer $TOKEN"
```
Response: `204 No Content` (empty body)

**Best Practices**
- Approval is idempotent-unsafe by design — a second call correctly 409s rather than silently succeeding; treat 409 on this endpoint as "already done," not a hard failure, when retrying after a timeout.
- Only `Admin` (not `HR`) can approve, even though `HR` can trigger processing — plan UI/role gating accordingly.

---

### List Salary Structures

**Purpose** — Returns all salary structures (basic + allowances + deductions + effective dates) across employees, for payroll configuration screens.

**URL** — `/api/v1/payroll/salary-structures`

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManagePayroll` (roles: `Admin`, `HR`)

**Request** — none

**Response** — `200 OK`
```json
{
  "data": [
    {
      "id": "d1e2...guid",
      "employeeId": "3f7b...guid",
      "basicSalary": 5000.00,
      "allowances": [ { "id": "a1...guid", "name": "HRA", "amount": 500.00 } ],
      "deductions": [ { "id": "d1...guid", "name": "Tax", "amount": 300.00 } ],
      "effectiveFrom": "2026-01-01T00:00:00Z",
      "effectiveTo": null,
      "isDeleted": false,
      "createdAtUtc": "2025-12-15T00:00:00Z",
      "updatedAtUtc": null
    }
  ],
  "message": "Request completed successfully.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** — none

**Error Codes** — `401` / `403 FORBIDDEN`

**Examples**
```bash
curl https://api.example.com/api/v1/payroll/salary-structures -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- This endpoint is not paginated — for large employee counts, expect a large response.

---

### Get Salary Structure by ID

**Purpose** — Fetch a single salary structure's detail, e.g. before editing it.

**URL** — `/api/v1/payroll/salary-structures/{id}`

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManagePayroll` (roles: `Admin`, `HR`)

**Request** — path param `id` (guid, required)

**Response** — `200 OK` — same `SalaryStructureDto` shape as the list endpoint's items.

**Validation** — route constraint `{id:guid}` only.

**Error Codes** — `404` (bare, no body) if not found; `401` / `403 FORBIDDEN`

**Examples**
```bash
curl https://api.example.com/api/v1/payroll/salary-structures/d1e2c3f4-1234-4a5b-9c8d-abcdef123456 \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Use `CreatedAtAction(nameof(GetSalaryStructure), ...)` `Location` header returned by `Create` to fetch the freshly created resource.

---

### Create Salary Structure

**Purpose** — Defines a new salary structure (basic salary + allowances + deductions + effective window) for an employee. Payroll processing looks up the structure effective as of the pay period start.

**URL** — `/api/v1/payroll/salary-structures`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication** — Policy `CanManagePayroll` (roles: `Admin`, `HR`)

**Request**
```json
{
  "employeeId": "3f7b...guid",
  "basicSalary": 5000.00,
  "allowances": [ { "name": "HRA", "amount": 500.00 } ],
  "deductions": [ { "name": "Tax", "amount": 300.00 } ],
  "effectiveFrom": "2026-01-01T00:00:00Z",
  "effectiveTo": null
}
```
- `employeeId` (guid, required)
- `basicSalary` (decimal, required, ≥ 0)
- `allowances` (array, optional; each: `name` required string, `amount` decimal ≥ 0)
- `deductions` (array, optional; each: `name` required string, `amount` decimal ≥ 0)
- `effectiveFrom` (DateTime, required)
- `effectiveTo` (DateTime, optional; if present, must be ≥ `effectiveFrom`)

**Response** — `201 Created`, `Location` header pointing to `GET /salary-structures/{id}`
```json
{
  "data": { "id": "d1e2...guid" },
  "message": "Salary structure created successfully.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** (`CreateSalaryStructureCommandValidator`)
- `EmployeeId` not empty, and must reference an existing employee (`employeeRepo.GetByIdAsync` check) — otherwise "Employee does not exist."
- `BasicSalary` ≥ 0
- `EffectiveFrom` ≤ `EffectiveTo` when `EffectiveTo` supplied
- Each `Allowances[].Name` not empty; `Allowances[].Amount` ≥ 0
- Each `Deductions[].Name` not empty; `Deductions[].Amount` ≥ 0

**Error Codes**
- `400 VALIDATION_ERROR` — e.g. `employeeId` doesn't exist, negative `basicSalary`, `effectiveTo` before `effectiveFrom`
- `401` / `403 FORBIDDEN`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/payroll/salary-structures \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"employeeId":"3f7b...","basicSalary":5000,"effectiveFrom":"2026-01-01T00:00:00Z"}'
```

**Best Practices**
- Multiple salary structures per employee are allowed (effective-dated) — set `effectiveTo` on the old structure and create a new one for a raise, rather than mutating the existing one, to preserve historical payslip accuracy.

---

### Update Salary Structure

**Purpose** — Replaces a salary structure's basic salary, allowances, deductions, and effective window.

**URL** — `/api/v1/payroll/salary-structures/{id}`

**Method** — PUT

**Headers** — `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication** — Policy `CanManagePayroll` (roles: `Admin`, `HR`)

**Request** — path param `id` (guid) plus body (route `id` is authoritative — controller overwrites `cmd.Id = id`):
```json
{
  "basicSalary": 5500.00,
  "allowances": [ { "id": "a1...guid", "name": "HRA", "amount": 550.00 } ],
  "deductions": [ { "id": null, "name": "Tax", "amount": 320.00 } ],
  "effectiveFrom": "2026-02-01T00:00:00Z",
  "effectiveTo": null
}
```
- `basicSalary` (decimal, required, ≥ 0)
- `allowances[].id` (guid, optional — omit/null for a new line item, supply to keep an existing one's identity)
- `allowances[].name` (string, required), `allowances[].amount` (decimal, ≥ 0)
- `deductions[]` — same shape as `allowances[]`
- `effectiveFrom` (DateTime, required), `effectiveTo` (DateTime, optional, ≥ `effectiveFrom`)

Note: the handler fully replaces the `Allowances`/`Deductions` collections (clears then re-adds) — it is not a partial patch of individual line items.

**Response** — `204 No Content`

**Validation** (`UpdateSalaryStructureCommandValidator`) — identical rule set to Create, minus the employee-existence check (`Id` not empty instead of `EmployeeId`).

**Error Codes**
- `400 VALIDATION_ERROR` — route/body `id` mismatch returns `{"status":400,"code":"ID_MISMATCH","message":"Route id does not match body id."}` (raised directly by the controller, not the middleware)
- `404 NOT_FOUND` — "Salary structure not found" if `id` doesn't exist
- `401` / `403 FORBIDDEN`

**Examples**
```bash
curl -X PUT https://api.example.com/api/v1/payroll/salary-structures/d1e2c3f4-1234-4a5b-9c8d-abcdef123456 \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"basicSalary":5500,"effectiveFrom":"2026-02-01T00:00:00Z"}'
```

**Best Practices**
- Always resend the full `allowances`/`deductions` arrays (including existing `id`s you want to keep) — omitted line items are deleted, not left untouched.

---

### Delete (Soft) Salary Structure

**Purpose** — Soft-deletes a salary structure, e.g. when it was created in error or superseded.

**URL** — `/api/v1/payroll/salary-structures/{id}`

**Method** — DELETE

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManagePayroll` (roles: `Admin`, `HR`)

**Request** — path param `id` (guid, required)

**Response** — `204 No Content`

**Validation** — none beyond route constraint; no validator class registered for `DeleteSalaryStructureCommand`.

**Error Codes** — `404 NOT_FOUND` — "Salary structure not found." if `id` doesn't exist; `401` / `403 FORBIDDEN`

**Examples**
```bash
curl -X DELETE https://api.example.com/api/v1/payroll/salary-structures/d1e2c3f4-1234-4a5b-9c8d-abcdef123456 \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Deleting a structure does not retroactively affect payslips already generated against it — it only stops future payroll runs from picking it up as the "effective" structure.

---

### Restore Salary Structure

**Purpose** — Un-deletes a previously soft-deleted salary structure.

**URL** — `/api/v1/payroll/salary-structures/{id}/restore`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManagePayroll` (roles: `Admin`, `HR`)

**Request** — path param `id` (guid, required). No body.

**Response** — `204 No Content`

**Validation** — none; no validator class registered for `RestoreSalaryStructureCommand`.

**Error Codes** — `404 NOT_FOUND` — `"Salary structure {id} not found."`; `401` / `403 FORBIDDEN`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/payroll/salary-structures/d1e2c3f4-1234-4a5b-9c8d-abcdef123456/restore \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices** — None beyond standard soft-delete restore semantics; this is a low-traffic admin recovery action.

---

### List Payslips for Employee

**Purpose** — Returns an employee's payslip history. Self-service for employees (always scoped to their own record) and full lookup for Admin/HR.

**URL** — `/api/v1/payroll/payslips`

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Any authenticated user (class-level `[Authorize]` only, no policy). Authorization is enforced in the handler, not via a policy attribute:
- Non-privileged callers (not `Admin`/`HR`) are always scoped to their own linked employee record, regardless of the `employeeId` query param.
- If a non-privileged caller supplies an `employeeId` that isn't their own, the request throws `UnauthorizedAccessException` → `403`.
- Privileged callers (`Admin`/`HR`) MUST supply `employeeId` (required per the validator when `IsPrivileged`).

**Request** — query param `employeeId` (guid, optional for self-service callers; required for `Admin`/`HR`)

**Response** — `200 OK`
```json
{
  "data": [
    {
      "id": "c2d5...guid",
      "payrollRunId": "b1e4...guid",
      "employeeId": "3f7b...guid",
      "basic": 5000.00,
      "totalAllowances": 800.00,
      "totalDeductions": 300.00,
      "totalReimbursements": 120.50,
      "totalBonus": 500.00,
      "totalOvertime": 187.50,
      "overtimeHours": 5.00,
      "grossPay": 6487.50,
      "netPay": 6307.50,
      "generatedAtUtc": "2026-08-01T09:00:12Z",
      "hasDocument": true
    }
  ],
  "message": "Request completed successfully.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** (`GetPayslipsForEmployeeQueryValidator`)
- `EmployeeId` required (not null) only `when IsPrivileged` — i.e. Admin/HR must pass `employeeId`; regular employees may omit it.

**Error Codes**
- `400 VALIDATION_ERROR` — privileged caller omitted `employeeId`
- `403 FORBIDDEN` — non-privileged caller passed an `employeeId` that isn't their own
- If the caller has no linked employee record, the handler returns an empty list (`200 OK`, `data: []`) rather than an error.
- `401` — missing/invalid JWT

**Examples**
```bash
# Self-service (Employee role)
curl https://api.example.com/api/v1/payroll/payslips -H "Authorization: Bearer $TOKEN"

# Admin/HR looking up a specific employee
curl "https://api.example.com/api/v1/payroll/payslips?employeeId=3f7b...guid" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

**Best Practices**
- Employee-role clients should simply omit `employeeId` rather than trying to pass their own ID — passing any other employee's ID always 403s.

---

### Download Payslip PDF

**Purpose** — Streams the generated payslip PDF for a specific payslip.

**URL** — `/api/v1/payroll/payslips/{payslipId}/download`

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Any authenticated user (class-level `[Authorize]` only). Handler-level scoping: non-privileged callers may only download their own payslip (`UnauthorizedAccessException` → `403` otherwise); `Admin`/`HR` may download any.

**Request** — path param `payslipId` (guid, required)

**Response** — `200 OK`, `Content-Type: application/pdf`, binary body, filename `payslip_{employeeId}_{payslipId}.pdf` (via `FileContentResult`, not the `ApiResponse<T>` envelope).

**Validation** — none (no FluentValidation validator registered for `DownloadPayslipQuery`).

**Error Codes**
- `404 NOT_FOUND` — "Payslip not found." if the `payslipId` doesn't exist, or "Payslip document not found." if the payslip has no stored PDF (e.g. generation failed during `process`) or the blob is missing from storage
- `403 FORBIDDEN` — non-privileged caller requesting someone else's payslip
- `401` — missing/invalid JWT

**Examples**
```bash
curl https://api.example.com/api/v1/payroll/payslips/c2d5.../download \
  -H "Authorization: Bearer $TOKEN" -o payslip.pdf
```

**Best Practices**
- Check `hasDocument` on the `PayslipDto` (from the list/run endpoints) before calling this — a `false` value means the PDF was never generated and this call will 404.

---

## Reimbursements API

Controller: `EMS.API/Controllers/ReimbursementController.cs`. Base route: `[Route("api/v1/reimbursements")]`, class-level `[Authorize]`, class-level `[EnableRateLimiting("WriteActionPolicy")]` (applies to every action in this controller, GET included, unless overridden).

Ownership model per the controller's own doc comment: create/edit/submit/delete/attach are strictly **owner-only** — there is no "Admin acts on behalf of employee" override anywhere in this controller (unlike Task Management). Review actions (start-review/approve/reject/request-changes) require the `CanManageReimbursements` policy (`Admin` only) and additionally block self-approval/self-rejection/self-changes-request at the handler level even for Admins who happen to also be the claimant.

Rate limiting (`Program.cs`): `WriteActionPolicy` = 100 requests/60s per client IP (fixed window, `QueueLimit 0` — over-limit requests are rejected outright, not queued); `AttachmentUploadPolicy` = 20 requests/60s per client IP, applied additionally to the attachment upload endpoint. On rejection: `429 Too Many Requests` with a `Retry-After` header (per `docs/api-specification.md` §3.1/§22 and `Program.cs`'s `options.OnRejected` handler).

`ReimbursementStatus` enum values: `Draft(0)`, `Submitted(1)`, `UnderReview(2)`, `Approved(3)`, `Rejected(4)`, `ChangesRequested(5)`, `Paid(6)`.

### List Reimbursements

**Purpose** — Paginated list of reimbursement claims. Employees see only their own; Admins can filter across everyone.

**URL** — `/api/v1/reimbursements`

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Any authenticated user. Non-Admin callers are always scoped to their own claims regardless of any `employeeId` filter supplied (silently overridden server-side, not rejected).

**Request** — query params (bound from `GetReimbursementsQuery`):
- `page` (int, optional, default 1)
- `pageSize` (int, optional, default 20; server clamps to 1–100, otherwise falls back to 20)
- `employeeId` (guid, optional — ignored for non-Admin callers)
- `status` (`ReimbursementStatus` string/int enum, optional)

**Response** — `200 OK`
```json
{
  "data": {
    "data": [
      {
        "id": "e1...guid",
        "reimbursementNumber": "REI-3F7BABCD1234",
        "employeeId": "3f7b...guid",
        "employeeName": "Jane Doe",
        "expenseTitle": "Client dinner",
        "expenseCategory": "Meals",
        "expenseDate": "2026-07-15T00:00:00Z",
        "amount": 85.40,
        "currency": "USD",
        "description": null,
        "notes": null,
        "distanceKm": null,
        "mileageRatePerKm": null,
        "status": "Draft",
        "submittedAtUtc": null,
        "approvedAtUtc": null,
        "approvedBy": null,
        "reviewRemarks": null,
        "payrollProcessed": false,
        "payrollRunId": null,
        "payrollDate": null,
        "createdAtUtc": "2026-07-15T10:00:00Z",
        "updatedAtUtc": null
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1
  },
  "message": "Request completed successfully.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** — no FluentValidation validator registered for `GetReimbursementsQuery`; `page`/`pageSize` are defensively clamped in the handler rather than rejected.

**Error Codes** — `401`; `429 Too Many Requests` if the IP's `WriteActionPolicy` budget (100/60s) is exhausted.

**Examples**
```bash
curl "https://api.example.com/api/v1/reimbursements?page=1&pageSize=20&status=Submitted" \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Use `status` filtering server-side rather than fetching all pages and filtering client-side.
- Respect the `429`/`Retry-After` — this endpoint shares the same 100-req/min-per-IP budget as every write action in this controller.

---

### Get Reimbursement by ID

**Purpose** — Fetch a single reimbursement's full detail.

**URL** — `/api/v1/reimbursements/{id}`

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Any authenticated user; non-Admin, non-owner callers get `404` (existence is deliberately not disclosed — this is intentional, per the controller's XML doc comment, not a bug).

**Request** — path param `id` (guid, required)

**Response** — `200 OK` — same `ReimbursementDto` shape as the list endpoint's items (unwrapped, not paginated).

**Validation** — none.

**Error Codes** — `404` (bare) — either the reimbursement doesn't exist, or it exists but the caller isn't the owner/Admin; `401`; `429`.

**Examples**
```bash
curl https://api.example.com/api/v1/reimbursements/e1a2b3c4-1234-4a5b-9c8d-abcdef123456 \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Treat 404 from this endpoint as "not visible to you," not necessarily "doesn't exist" — don't use it to probe for record existence.

---

### Create Reimbursement

**Purpose** — Creates a new reimbursement claim in `Draft` status for the calling employee. Supports two modes: flat `amount` claims, or mileage claims (`distanceKm` set, amount auto-computed).

**URL** — `/api/v1/reimbursements`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication** — Any authenticated user with a linked employee record (`RequestingUserId` is set server-side from the JWT; the caller cannot claim on behalf of another employee).

**Request**
```json
{
  "expenseTitle": "Client dinner",
  "expenseCategory": "Meals",
  "expenseDate": "2026-07-15T00:00:00Z",
  "amount": 85.40,
  "currency": "USD",
  "description": "Dinner with Acme Corp",
  "notes": null,
  "distanceKm": null
}
```
- `expenseTitle` (string, required, ≤200 chars)
- `expenseCategory` (string, required, ≤100 chars)
- `expenseDate` (DateTime, required, cannot be more than 1 day in the future)
- `amount` (decimal, required and must be > 0 **only when `distanceKm` is null**; ignored/recomputed when `distanceKm` is set)
- `currency` (string, required, ≤10 chars; defaults to `"USD"` if omitted)
- `description` (string, optional, ≤2000 chars)
- `notes` (string, optional, ≤1000 chars)
- `distanceKm` (decimal, optional; when set, must be > 0 — triggers mileage-claim mode: server computes `amount = distanceKm * configured mileage rate` (default `0.30`/km, from `Reimbursements:MileageRatePerKm` config), rounded to 2 decimals, and stores the rate used in `mileageRatePerKm`)

**Response** — `201 Created`, `Location` header → `GET /{id}`
```json
{
  "data": { "id": "e1...guid", "reimbursementNumber": "REI-3F7BABCD1234", "status": "Draft", "...": "full ReimbursementDto" },
  "message": "Reimbursement created successfully.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** (`CreateReimbursementCommandValidator`)
- `ExpenseTitle` not empty, ≤200 chars
- `ExpenseCategory` not empty, ≤100 chars
- `ExpenseDate` ≤ tomorrow (UTC) — "Expense date cannot be in the future."
- `Amount` > 0 when `DistanceKm` is null
- `DistanceKm` > 0 when supplied
- `Currency` not empty, ≤10 chars
- `Description` ≤2000 chars
- `Notes` ≤1000 chars

**Error Codes**
- `400 VALIDATION_ERROR` — any rule above
- `409 CONFLICT` — "The caller has no linked employee record and cannot submit reimbursements." (`InvalidOperationException`, non-"not found" message → 409 per the middleware mapping)
- `401`; `429 Too Many Requests`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/reimbursements \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"expenseTitle":"Client dinner","expenseCategory":"Meals","expenseDate":"2026-07-15T00:00:00Z","amount":85.40,"currency":"USD"}'
```

**Best Practices**
- For mileage claims, don't send `amount` at all (or send it knowing it will be ignored) — the server always recomputes it from `distanceKm * rate`.
- New claims start `Draft`; call `POST /{id}/submit` separately to move them into the approval workflow — creation alone does not submit.

---

### Update Reimbursement

**Purpose** — Edits an existing reimbursement's fields. Owner-only, and only while the claim is still `Draft` or `ChangesRequested`.

**URL** — `/api/v1/reimbursements/{id}`

**Method** — PUT

**Headers** — `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication** — Owner-only (no Admin override). `RequestingUserId` set server-side.

**Request** — path param `id` (guid) plus body; route/body `id` must match. Same fields as Create:
```json
{
  "id": "e1...guid",
  "expenseTitle": "Client dinner (updated)",
  "expenseCategory": "Meals",
  "expenseDate": "2026-07-15T00:00:00Z",
  "amount": 90.00,
  "currency": "USD",
  "description": null,
  "notes": null,
  "distanceKm": null
}
```

**Response** — `200 OK` with the updated `ReimbursementDto` wrapped in `ApiResponse<T>`.

**Validation** (`UpdateReimbursementCommandValidator`) — identical rules to `CreateReimbursementCommandValidator` (title/category/date/amount-or-distance/currency/description/notes).

**Error Codes**
- `400 VALIDATION_ERROR` — field rules, or `{"code":"ID_MISMATCH"}` if route `id` ≠ body `id`
- `404 NOT_FOUND` — `"Reimbursement {id} not found."`
- `403 FORBIDDEN` — caller is not the claim's owner ("You can only edit your own reimbursements.")
- `409 CONFLICT` — `"Reimbursement {number} can only be edited while Draft or ChangesRequested (currently {status})."`
- `401`; `429`

**Examples**
```bash
curl -X PUT https://api.example.com/api/v1/reimbursements/e1a2b3c4-1234-4a5b-9c8d-abcdef123456 \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"id":"e1a2b3c4-1234-4a5b-9c8d-abcdef123456","expenseTitle":"Client dinner (updated)","expenseCategory":"Meals","expenseDate":"2026-07-15T00:00:00Z","amount":90,"currency":"USD"}'
```

**Best Practices**
- Check the claim's current `status` before attempting an edit — anything past `ChangesRequested` (e.g. `Submitted`, `UnderReview`, `Approved`) will 409.

---

### Delete Reimbursement

**Purpose** — Soft-deletes a `Draft` reimbursement. Owner-only.

**URL** — `/api/v1/reimbursements/{id}`

**Method** — DELETE

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Owner-only (no Admin override).

**Request** — path param `id` (guid, required)

**Response** — `204 No Content`

**Validation** — none.

**Error Codes**
- `404 NOT_FOUND` — `"Reimbursement {id} not found."`
- `403 FORBIDDEN` — not the owner
- `409 CONFLICT` — `"Reimbursement {number} can only be deleted while Draft (currently {status})."`
- `401`; `429`

**Examples**
```bash
curl -X DELETE https://api.example.com/api/v1/reimbursements/e1a2b3c4-1234-4a5b-9c8d-abcdef123456 \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Once submitted, a claim can never be deleted via this API (only `Draft` qualifies) — use the review workflow (reject/request-changes) instead.

---

### Submit Reimbursement

**Purpose** — Moves a `Draft` or `ChangesRequested` claim to `Submitted`, entering the approval workflow. Owner-only.

**URL** — `/api/v1/reimbursements/{id}/submit`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Owner-only (no Admin override).

**Request** — path param `id` (guid, required). No body.

**Response** — `204 No Content`

**Validation** — none registered.

**Error Codes**
- `404 NOT_FOUND` — `"Reimbursement {id} not found."`
- `403 FORBIDDEN` — not the owner
- `409 CONFLICT` — `"Reimbursement {number} must be Draft or ChangesRequested to submit (currently {status})."`
- `401`; `429`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/reimbursements/e1a2b3c4-1234-4a5b-9c8d-abcdef123456/submit \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Ensure required attachments (receipts) are uploaded before submitting — once `Submitted`, the claim is no longer editable and attachments can still be added up until `Approved`/`Rejected`/`Paid`, but the expense fields themselves are locked.

---

### Start Review

**Purpose** — Moves a `Submitted` claim into `UnderReview`, signaling a reviewer has picked it up.

**URL** — `/api/v1/reimbursements/{id}/start-review`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManageReimbursements` (role: `Admin` only)

**Request** — path param `id` (guid, required). No body.

**Response** — `204 No Content`

**Validation** — none registered.

**Error Codes**
- `404 NOT_FOUND` — `"Reimbursement {id} not found."`
- `409 CONFLICT` — `"Reimbursement {number} must be Submitted to start review (currently {status})."`
- `403 FORBIDDEN` — caller is not `Admin`
- `401`; `429`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/reimbursements/e1a2b3c4-1234-4a5b-9c8d-abcdef123456/start-review \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

**Best Practices** — This step is optional in the sense that nothing else in this controller requires it directly, but `Approve`/`Reject`/`RequestChanges` all require the claim to be `UnderReview` first — always call this before those three actions.

---

### Approve Reimbursement

**Purpose** — Approves an `UnderReview` claim. Approved claims become eligible for the next payroll run to fold into `NetPay` and mark `Paid`.

**URL** — `/api/v1/reimbursements/{id}/approve`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManageReimbursements` (role: `Admin` only). Additionally blocked at the handler level if the caller is the claim's own employee, even if `Admin`.

**Request** — path param `id` (guid, required). No body.

**Response** — `204 No Content`

**Validation** — none registered.

**Error Codes**
- `404 NOT_FOUND` — `"Reimbursement {id} not found."`
- `409 CONFLICT` — `"You cannot approve your own reimbursement."` (self-approval block)
- `409 CONFLICT` — `"Reimbursement {number} must be UnderReview to approve (currently {status})."`
- `403 FORBIDDEN` — caller is not `Admin`
- `401`; `429`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/reimbursements/e1a2b3c4-1234-4a5b-9c8d-abcdef123456/approve \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

**Best Practices**
- Note the self-approval rule fires as a `409`, not `403` — it's a business-state conflict in this codebase's convention, not an authorization failure, even though conceptually it's about who the caller is. Handle both codes when building a generic error banner for this action.

---

### Reject Reimbursement

**Purpose** — Rejects an `UnderReview` claim with a mandatory remark explaining why.

**URL** — `/api/v1/reimbursements/{id}/reject`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication** — Policy `CanManageReimbursements` (role: `Admin` only). Self-rejection blocked at the handler level.

**Request** — path param `id` (guid, required); body:
```json
{ "remarks": "Missing itemized receipt." }
```
- `remarks` (string, required, ≤1000 chars)

**Response** — `204 No Content`

**Validation** (`RejectReimbursementCommandValidator`)
- `Remarks` not empty, ≤1000 chars — "A remark explaining the rejection is required."

**Error Codes**
- `400 VALIDATION_ERROR` — missing/oversized `remarks`
- `404 NOT_FOUND` — `"Reimbursement {id} not found."`
- `409 CONFLICT` — `"You cannot reject your own reimbursement."`
- `409 CONFLICT` — `"Reimbursement {number} must be UnderReview to reject (currently {status})."`
- `403 FORBIDDEN`; `401`; `429`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/reimbursements/e1a2b3c4-1234-4a5b-9c8d-abcdef123456/reject \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"remarks":"Missing itemized receipt."}'
```

**Best Practices** — `Rejected` is terminal in this handler set — there is no "reopen a rejected claim" action; the employee would need to create a new claim.

---

### Request Changes

**Purpose** — Sends an `UnderReview` claim back to the employee for edits, with a mandatory remark. The claim becomes `ChangesRequested`, which the owner can then edit (`PUT`) and resubmit (`submit`).

**URL** — `/api/v1/reimbursements/{id}/request-changes`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication** — Policy `CanManageReimbursements` (role: `Admin` only). Self-request-changes blocked at the handler level.

**Request** — path param `id` (guid, required); body:
```json
{ "remarks": "Please attach the original receipt, not a photo of the credit card statement." }
```
- `remarks` (string, required, ≤1000 chars)

**Response** — `204 No Content`

**Validation** (`RequestChangesReimbursementCommandValidator`)
- `Remarks` not empty, ≤1000 chars — "A remark explaining the requested changes is required."

**Error Codes**
- `400 VALIDATION_ERROR`
- `404 NOT_FOUND` — `"Reimbursement {id} not found."`
- `409 CONFLICT` — `"You cannot request changes on your own reimbursement."`
- `409 CONFLICT` — `"Reimbursement {number} must be UnderReview to request changes (currently {status})."`
- `403 FORBIDDEN`; `401`; `429`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/reimbursements/e1a2b3c4-1234-4a5b-9c8d-abcdef123456/request-changes \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"remarks":"Please attach the original receipt."}'
```

**Best Practices** — Surface `reviewRemarks` prominently to the employee on their claim detail view after this action, since it's the only channel communicating what needs fixing.

---

### List Attachments

**Purpose** — Lists a reimbursement's supporting documents (receipts).

**URL** — `/api/v1/reimbursements/{id}/attachments`

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Owner or `Admin`. Non-owner, non-Admin callers get `403 FORBIDDEN` (note: unlike `GetById`, this throws `UnauthorizedAccessException` rather than returning 404 — existence IS effectively disclosed here through the status code difference).

**Request** — path param `id` (guid, required)

**Response** — `200 OK`
```json
{
  "data": [
    {
      "id": "f1...guid",
      "reimbursementId": "e1...guid",
      "originalFileName": "receipt.pdf",
      "contentType": "application/pdf",
      "fileSizeBytes": 204800,
      "uploadedAtUtc": "2026-07-15T10:05:00Z",
      "uploadedBy": "u1...guid"
    }
  ],
  "message": "Request completed successfully.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** — none.

**Error Codes**
- `404 NOT_FOUND` — `"Reimbursement {id} not found."`
- `403 FORBIDDEN` — not owner/Admin
- `401`; `429`

**Examples**
```bash
curl https://api.example.com/api/v1/reimbursements/e1a2b3c4-1234-4a5b-9c8d-abcdef123456/attachments \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices** — None beyond standard read-scoping.

---

### Upload Attachment

**Purpose** — Uploads a supporting document (receipt) to a reimbursement claim. Owner-only; blocked once the claim is `Approved`/`Rejected`/`Paid`.

**URL** — `/api/v1/reimbursements/{id}/attachments`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`, `Content-Type: multipart/form-data`

**Authentication** — Owner-only (no Admin override). Additional rate limit: `AttachmentUploadPolicy` (20 requests/60s per IP), layered on top of the controller-wide `WriteActionPolicy`.

**Request** — path param `id` (guid, required); multipart form field `file` (binary, required):
- Allowed content types: `application/pdf` (`.pdf`), `image/jpeg` (`.jpg`/`.jpeg`), `image/png` (`.png`)
- Max size: 10 MB
- File extension must match the declared `Content-Type`
- Actual file bytes are checked against magic-number signatures (`%PDF-`, `FF D8 FF`, PNG signature) — a spoofed `Content-Type` header won't bypass validation
- `fileName` must not contain path separators (`/`, `\`) or `..`, and no invalid filename characters

**Response** — `200 OK` (not 201, despite `[ProducesResponseType(..., 201)]` annotation on the action — the actual code returns `Ok(...)`)
```json
{
  "data": "f1a2b3c4-1234-4a5b-9c8d-abcdef123456",
  "message": "Attachment uploaded.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** (`UploadReimbursementAttachmentCommandValidator`)
- `ReimbursementId` not empty
- `FileName` not empty, ≤255 chars, no invalid filename chars, no path separators/`..`
- `ContentType` must be one of the 3 allowed types
- File extension must match content type
- `Content` not empty, ≤10 MB (`MaxFileSizeBytes = 10 * 1024 * 1024`)
- File's magic-number signature must match the declared content type

Controller-level check before dispatch: `file == null || file.Length == 0` → `400 FILE_REQUIRED` ("A file is required.") — raised directly by the controller, not via FluentValidation.

**Error Codes**
- `400 FILE_REQUIRED` — no file/empty file (controller-level check)
- `400 VALIDATION_ERROR` — disallowed type, oversized, extension/content-type/signature mismatch, bad filename
- `404 NOT_FOUND` — `"Reimbursement {id} not found."`
- `403 FORBIDDEN` — not the owner
- `409 CONFLICT` — `"Reimbursement {number} is {status} and no longer accepts new attachments."` (status is `Approved`/`Rejected`/`Paid`)
- `401`; `429 Too Many Requests` (20/60s attachment-specific budget, tighter than the general 100/60s)

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/reimbursements/e1a2b3c4-1234-4a5b-9c8d-abcdef123456/attachments \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@receipt.pdf;type=application/pdf"
```
```json
{ "data": "f1a2b3c4-1234-4a5b-9c8d-abcdef123456", "message": "Attachment uploaded.", "correlationId": "a1b2c3d4e5f6a1b2" }
```

**Best Practices**
- Upload receipts while the claim is still `Draft`/`ChangesRequested`/`Submitted`/`UnderReview` — once a reviewer acts (`Approved`/`Rejected`), uploads are permanently blocked for that claim.
- Respect the 20-req/min attachment-specific rate limit separately from the 100-req/min general write budget — batch uploads (e.g. multiple receipts) should be paced accordingly.

---

### Download Attachment

**Purpose** — Streams a specific attachment's file content.

**URL** — `/api/v1/reimbursements/attachments/{attachmentId}/download`

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Owner or `Admin`. Unlike other endpoints in this controller, a non-owner here gets `404` (the handler returns `null` rather than throwing), matching the "don't disclose existence" pattern used by `GetById`.

**Request** — path param `attachmentId` (guid, required)

**Response** — `200 OK`, binary body with the original `Content-Type` and filename (via `FileContentResult`).

**Validation** — none.

**Error Codes** — `404` (bare `NotFound()`) — attachment doesn't exist, its parent reimbursement doesn't exist, caller isn't owner/Admin, or the stored blob is missing; `401`; `429`.

**Examples**
```bash
curl https://api.example.com/api/v1/reimbursements/attachments/f1a2b3c4-1234-4a5b-9c8d-abcdef123456/download \
  -H "Authorization: Bearer $TOKEN" -o receipt.pdf
```

**Best Practices** — None beyond standard read-scoping; note the inconsistency vs. `GetAttachments` (403) — don't assume a uniform "attachment access denied" status code across this controller.

---

## Assets API

Controller: `EMS.API/Controllers/AssetsController.cs`. Base route: `[Route("api/v1/assets")]`, class-level `[Authorize(Policy = "CanManageAssets")]` (roles: `Admin`, `HR`) — **every** action in this controller requires this policy; there is no self-service/employee-facing angle (per the controller's own doc comment, an employee doesn't manage their own asset assignments).

`AssetStatus` enum values: `Available`, `Assigned`, `UnderRepair`, `Retired`, `Lost`.

Two actions use `~/` absolute routing overrides that place them outside `/api/v1/assets`: the Return endpoint lives at `/api/v1/asset-assignments/{id}/return`, and the employee-assets lookup lives at `/api/v1/employees/{employeeId}/assets`.

### List Assets

**Purpose** — Paginated, filterable list of all assets (laptops, mobiles, etc.) in inventory.

**URL** — `/api/v1/assets`

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManageAssets` (roles: `Admin`, `HR`)

**Request** — query params (bound from `GetAssetsQuery`):
- `page` (int, optional, default 1)
- `pageSize` (int, optional, default 20; server clamps to 1–100)
- `status` (`AssetStatus` enum, optional)
- `category` (string, optional)
- `search` (string, optional)

**Response** — `200 OK`
```json
{
  "data": {
    "data": [
      {
        "id": "a1...guid",
        "assetTag": "AST-3F7BABCD1234",
        "category": "Laptop",
        "brand": "Dell",
        "model": "Latitude 7440",
        "serialNumber": "SN123456",
        "purchaseDate": "2025-06-01T00:00:00Z",
        "purchaseCost": 1500.00,
        "status": "Available",
        "notes": null,
        "isDeleted": false,
        "createdAtUtc": "2025-06-01T00:00:00Z",
        "updatedAtUtc": null
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1
  },
  "message": "Request completed successfully.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** — none registered for `GetAssetsQuery`; paging clamped defensively in-handler.

**Error Codes** — `401`; `403 FORBIDDEN` (non-Admin/HR caller)

**Examples**
```bash
curl "https://api.example.com/api/v1/assets?status=Available&category=Laptop" \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices** — Use `status`/`category`/`search` server-side filters rather than paging through the full inventory client-side.

---

### Get Asset by ID

**Purpose** — Fetch a single asset's detail.

**URL** — `/api/v1/assets/{id}`

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManageAssets`

**Request** — path param `id` (guid, required)

**Response** — `200 OK` — `AssetDto`, same shape as list items (unwrapped).

**Validation** — route constraint only.

**Error Codes** — `404` (bare) if not found; `401`; `403 FORBIDDEN`

**Examples**
```bash
curl https://api.example.com/api/v1/assets/a1b2c3d4-1234-4a5b-9c8d-abcdef123456 \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices** — None beyond standard lookup.

---

### Create Asset

**Purpose** — Registers a new asset into inventory. Always starts `Available`; `assetTag` is server-generated.

**URL** — `/api/v1/assets`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication** — Policy `CanManageAssets`

**Request**
```json
{
  "category": "Laptop",
  "brand": "Dell",
  "model": "Latitude 7440",
  "serialNumber": "SN123456",
  "purchaseDate": "2025-06-01T00:00:00Z",
  "purchaseCost": 1500.00,
  "notes": null
}
```
- `category` (string, required, ≤100 chars)
- `brand` (string, optional, ≤100 chars)
- `model` (string, optional, ≤100 chars)
- `serialNumber` (string, optional, ≤150 chars)
- `purchaseDate` (DateTime, optional)
- `purchaseCost` (decimal, optional, ≥ 0 when supplied)
- `notes` (string, optional, ≤1000 chars)

**Response** — `201 Created`, `Location` → `GET /{id}`
```json
{
  "data": { "id": "a1...guid" },
  "message": "Asset created successfully.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** (`CreateAssetCommandValidator`)
- `Category` not empty, ≤100 chars
- `Brand` ≤100 chars
- `Model` ≤100 chars
- `SerialNumber` ≤150 chars
- `PurchaseCost` ≥ 0 when supplied
- `Notes` ≤1000 chars

**Error Codes** — `400 VALIDATION_ERROR`; `401`; `403 FORBIDDEN`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/assets \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"category":"Laptop","brand":"Dell","model":"Latitude 7440"}'
```

**Best Practices** — `assetTag` is generated server-side (`AST-{guid prefix}`) — don't attempt to supply or predict it; read it back from the response of `GET /{id}` after creation.

---

### Update Asset

**Purpose** — Updates an asset's descriptive details (category, brand, model, serial, purchase info, notes). Does not change `status` — use `POST /{id}/status`, `/assign`, or the return endpoint for that.

**URL** — `/api/v1/assets/{id}`

**Method** — PUT

**Headers** — `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication** — Policy `CanManageAssets`

**Request** — path param `id` (guid) plus body (route `id` authoritative; body `id` must match):
```json
{
  "id": "a1...guid",
  "category": "Laptop",
  "brand": "Dell",
  "model": "Latitude 7450",
  "serialNumber": "SN123456",
  "purchaseDate": "2025-06-01T00:00:00Z",
  "purchaseCost": 1600.00,
  "notes": "Upgraded RAM"
}
```

**Response** — `204 No Content`

**Validation** (`UpdateAssetCommandValidator`) — same rules as Create plus `Id` not empty.

**Error Codes**
- `400 VALIDATION_ERROR`; `{"code":"ID_MISMATCH"}` (400) if route/body `id` differ
- `404 NOT_FOUND` — `"Asset {id} not found."`
- `401`; `403 FORBIDDEN`

**Examples**
```bash
curl -X PUT https://api.example.com/api/v1/assets/a1b2c3d4-1234-4a5b-9c8d-abcdef123456 \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"id":"a1b2c3d4-1234-4a5b-9c8d-abcdef123456","category":"Laptop","model":"Latitude 7450"}'
```

**Best Practices** — This endpoint silently ignores/does not accept a `status` field — attempting to change status via this endpoint has no effect; use the dedicated status/assign/return actions.

---

### Delete (Soft) Asset

**Purpose** — Soft-deletes an asset. Rejected while the asset is currently assigned.

**URL** — `/api/v1/assets/{id}`

**Method** — DELETE

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManageAssets`

**Request** — path param `id` (guid, required)

**Response** — `204 No Content`

**Validation** — none registered for `DeleteAssetCommand`.

**Error Codes**
- `404 NOT_FOUND` — `"Asset {id} not found."`
- `409 CONFLICT` — `"Asset {tag} is currently assigned and must be returned before it can be deleted."`
- `401`; `403 FORBIDDEN`

**Examples**
```bash
curl -X DELETE https://api.example.com/api/v1/assets/a1b2c3d4-1234-4a5b-9c8d-abcdef123456 \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices** — Return the asset first (`POST /api/v1/asset-assignments/{assignmentId}/return`) if it's currently `Assigned`, then delete.

---

### Restore Asset

**Purpose** — Un-deletes a previously soft-deleted asset.

**URL** — `/api/v1/assets/{id}/restore`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManageAssets`

**Request** — path param `id` (guid, required). No body.

**Response** — `204 No Content`

**Validation** — none registered for `RestoreAssetCommand`.

**Error Codes** — `404 NOT_FOUND` — `"Asset {id} not found."`; `401`; `403 FORBIDDEN`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/assets/a1b2c3d4-1234-4a5b-9c8d-abcdef123456/restore \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices** — None beyond standard restore semantics.

---

### Update Asset Status

**Purpose** — Changes an asset's status outside the assign/return flow — e.g. `Available` → `UnderRepair`, `UnderRepair` → `Available`, or → `Retired`/`Lost`. Cannot be used to set `Assigned` (use the Assign action) and is rejected while the asset is currently assigned (must be returned first).

**URL** — `/api/v1/assets/{id}/status`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication** — Policy `CanManageAssets`

**Request** — path param `id` (guid); body:
```json
{ "status": "UnderRepair" }
```
- `status` (`AssetStatus` enum — `Available`|`Assigned`|`UnderRepair`|`Retired`|`Lost` — required; `Assigned` is rejected by the handler)

**Response** — `204 No Content`

**Validation** — no FluentValidation validator registered for `UpdateAssetStatusCommand` (enum binding failure for an invalid string would itself produce a model-binding 400 from ASP.NET Core, separate from FluentValidation).

**Error Codes**
- `404 NOT_FOUND` — `"Asset {id} not found."`
- `409 CONFLICT` — `"Status cannot be set to Assigned directly — use the Assign action."`
- `409 CONFLICT` — `"Asset {tag} is currently assigned and must be returned before its status can change."`
- `401`; `403 FORBIDDEN`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/assets/a1b2c3d4-1234-4a5b-9c8d-abcdef123456/status \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"status":"UnderRepair"}'
```

**Best Practices** — Query `GET /{id}` first to confirm current status before attempting a transition, since both "already assigned" and "target=Assigned" are hard 409s.

---

### List Asset Assignment History

**Purpose** — Returns an asset's full assignment history (current + past), most recent first.

**URL** — `/api/v1/assets/{id}/assignments`

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManageAssets`

**Request** — path param `id` (guid, required) — this is the **asset** ID.

**Response** — `200 OK`
```json
{
  "data": [
    {
      "id": "as1...guid",
      "assetId": "a1...guid",
      "assetTag": "AST-3F7BABCD1234",
      "employeeId": "3f7b...guid",
      "employeeName": "Jane Doe",
      "assignedByUserId": "u1...guid",
      "assignedDate": "2026-01-10T00:00:00Z",
      "expectedReturnDate": "2027-01-10T00:00:00Z",
      "conditionAtAssignment": "New",
      "returnedDate": null,
      "conditionAtReturn": null,
      "notes": null,
      "createdAtUtc": "2026-01-10T00:00:00Z"
    }
  ],
  "message": "Request completed successfully.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** — none.

**Error Codes** — `401`; `403 FORBIDDEN`. Note: the handler does not appear to validate the asset exists before returning (an unknown `id` likely returns an empty array rather than 404 — confirm against `GetAssetAssignmentsQueryHandler`/repository if this matters for your client; not fully traced beyond the query class itself).

**Examples**
```bash
curl https://api.example.com/api/v1/assets/a1b2c3d4-1234-4a5b-9c8d-abcdef123456/assignments \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices** — None beyond standard read.

---

### Assign Asset

**Purpose** — Allocates an `Available` asset to an employee (laptop/mobile allocation), creating an assignment record and flipping the asset's status to `Assigned`.

**URL** — `/api/v1/assets/{id}/assign`

**Method** — POST

**Headers** — `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication** — Policy `CanManageAssets`

**Request** — path param `id` (guid) — this is the **asset** ID, set into `cmd.AssetId` by the controller; body:
```json
{
  "employeeId": "3f7b...guid",
  "expectedReturnDate": "2027-01-10T00:00:00Z",
  "conditionAtAssignment": "New",
  "notes": null
}
```
- `employeeId` (guid, required, must reference an existing employee)
- `expectedReturnDate` (DateTime, optional)
- `conditionAtAssignment` (string, optional, ≤500 chars)
- `notes` (string, optional, ≤1000 chars)

`assignedByUserId` is set server-side from the caller's JWT identity, not client-supplied.

**Response** — `200 OK` (not 201, despite the `[ProducesResponseType(..., 201)]` annotation — actual code path returns `Ok(...)`)
```json
{
  "data": "as1a2b3c-1234-4a5b-9c8d-abcdef123456",
  "message": "Asset assigned.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** (`AssignAssetCommandValidator`)
- `AssetId` not empty
- `EmployeeId` not empty, must reference an existing employee — otherwise "Employee does not exist."
- `ConditionAtAssignment` ≤500 chars
- `Notes` ≤1000 chars

**Error Codes**
- `400 VALIDATION_ERROR` — bad `employeeId`, oversized fields
- `404 NOT_FOUND` — `"Asset {id} not found."`
- `409 CONFLICT` — `"Asset {tag} is {status} and cannot be assigned — only an Available asset can be assigned."`
- `401`; `403 FORBIDDEN`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/assets/a1b2c3d4-1234-4a5b-9c8d-abcdef123456/assign \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"employeeId":"3f7b...guid","expectedReturnDate":"2027-01-10T00:00:00Z","conditionAtAssignment":"New"}'
```
```json
{ "data": "as1a2b3c-1234-4a5b-9c8d-abcdef123456", "message": "Asset assigned.", "correlationId": "a1b2c3d4e5f6a1b2" }
```

**Best Practices** — Only `Available` assets can be assigned — check `status` via `GET /{id}` first if unsure; a `409` here almost always means someone else assigned it first (race condition) or it needs to be returned/repaired.

---

### Return Asset

**Purpose** — Closes out an open assignment (asset return tracking) and sets the asset's resulting status (defaults to `Available`; can instead route it to `UnderRepair`/`Retired`/`Lost` based on returned condition).

**URL** — `/api/v1/asset-assignments/{id}/return` (note: this is an absolute route override — NOT under `/api/v1/assets`, despite living in `AssetsController`)

**Method** — POST

**Headers** — `Authorization: Bearer <token>`, `Content-Type: application/json`

**Authentication** — Policy `CanManageAssets`

**Request** — path param `id` (guid) — this is the **assignment** ID, not the asset ID; body:
```json
{
  "conditionAtReturn": "Minor scratches",
  "notes": "Returned on schedule",
  "resultingAssetStatus": "Available"
}
```
- `conditionAtReturn` (string, optional, ≤500 chars)
- `notes` (string, optional, ≤1000 chars) — if omitted/blank, the assignment's existing `notes` are left unchanged rather than cleared
- `resultingAssetStatus` (`AssetStatus` enum, optional, defaults to `Available` if omitted; must not be `Assigned`)

**Response** — `204 No Content`

**Validation** (`ReturnAssetCommandValidator`)
- `Id` not empty
- `ConditionAtReturn` ≤500 chars
- `Notes` ≤1000 chars
- `ResultingAssetStatus` must be a valid enum value

**Error Codes**
- `400 VALIDATION_ERROR`
- `404 NOT_FOUND` — `"Asset assignment {id} not found."` or, less likely, `"Asset {assetId} not found."` if the linked asset record is missing
- `409 CONFLICT` — `"This assignment has already been returned."`
- `409 CONFLICT` — `"ResultingAssetStatus cannot be Assigned — return a specific condition (Available, UnderRepair, Retired, or Lost)."`
- `401`; `403 FORBIDDEN`

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/asset-assignments/as1a2b3c-1234-4a5b-9c8d-abcdef123456/return \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"conditionAtReturn":"Minor scratches","resultingAssetStatus":"Available"}'
```

**Best Practices** — Use the **assignment** ID (from `GetAssignments`/`Assign`'s response), not the asset ID, in the URL — a common integration mistake given this endpoint's non-standard route placement outside `/assets`.

---

### List Employee's Asset History

**Purpose** — Returns one employee's full asset assignment history (current + past), across all assets.

**URL** — `/api/v1/employees/{employeeId}/assets` (absolute route override — outside `/api/v1/assets`)

**Method** — GET

**Headers** — `Authorization: Bearer <token>`

**Authentication** — Policy `CanManageAssets`

**Request** — path param `employeeId` (guid, required)

**Response** — `200 OK` — array of `AssetAssignmentDto` (same shape as `GetAssignments`), wrapped in `ApiResponse<T>`.

**Validation** — none.

**Error Codes** — `401`; `403 FORBIDDEN`. No explicit "employee not found" check traced in the handler signature — an unknown `employeeId` likely returns an empty array rather than 404.

**Examples**
```bash
curl https://api.example.com/api/v1/employees/3f7b1234-1234-4a5b-9c8d-abcdef123456/assets \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices** — Useful for an employee's "my equipment" admin view (looked up by HR/Admin, not self-service — this endpoint still requires `CanManageAssets`, an employee cannot call it for themselves).
# Recruitment, Performance & Task Management API Reference

Derived directly from the current source: `backend/EMS.API/Controllers/CandidatesController.cs`,
`backend/EMS.API/Controllers/PerformanceController.cs`, `backend/EMS.API/Controllers/TaskController.cs`,
and their corresponding `EMS.Application/Features/{Recruitment,Performance,Tasks}` Commands/Queries/Validators/DTOs.

## Conventions used across all endpoints below

- **Base URL prefix**: `/api/v1` (all three controllers).
- **Auth header**: every endpoint requires `Authorization: Bearer <access_token>` except where noted — all three controllers carry a class-level `[Authorize]`; there is no `[AllowAnonymous]` endpoint in this scope.
- **Success envelope** (`ApiResponse<T>`, from `EMS.API.Controllers.ApiResponse<T>`):
  ```json
  { "data": { }, "message": "Request completed successfully.", "correlationId": "a1b2c3d4e5f6a7b8" }
  ```
- **Paged envelope** (`PagedResult<T>`, wraps `data` above):
  ```json
  { "data": [ ], "page": 1, "pageSize": 20, "totalCount": 57, "totalPages": 3 }
  ```
- **Error envelope** (`ApiErrorResponse`), produced by `EMS.API.Middleware.ExceptionHandlingMiddleware`:
  ```json
  { "status": 400, "code": "VALIDATION_ERROR", "message": "...", "errors": [ { "propertyName": "Email", "errorMessage": "..." } ], "correlationId": "..." }
  ```
  Exception → status mapping (global, applies to every endpoint in this doc):
  | Exception | Status | Code |
  |---|---|---|
  | `FluentValidation.ValidationException` | 400 | `VALIDATION_ERROR` |
  | `InvalidOperationException` whose message contains "not found" | 404 | `NOT_FOUND` |
  | `InvalidOperationException` (any other message — illegal state transition, business rule) | 409 | `CONFLICT` |
  | `UnauthorizedAccessException` | 403 | `FORBIDDEN` |
  | anything else | 500 | `INTERNAL_ERROR` |
- **Rate limiting**: `TaskController` carries `[EnableRateLimiting("WriteActionPolicy")]` at the class level (100 requests/60s per client IP, configurable via `RateLimiting:WriteAction:*`), and the attachment-upload action additionally carries `[EnableRateLimiting("AttachmentUploadPolicy")]` (20/60s per IP). `CandidatesController` and `PerformanceController` have no rate-limit attributes. Exceeding a limit returns `429` with `{ "status": 429, "code": "RATE_LIMIT_EXCEEDED", "message": "Too many requests. Please try again later." }` and a `Retry-After` header.
- File uploads (candidate attachments, task attachments) are `multipart/form-data`, field name `file`, PDF/JPEG/PNG only, 10 MB max, with a magic-byte signature check against the declared content type (not just extension/MIME).

---

## Candidates API

Controller route: `[Route("api/v1/candidates")]`. Several actions use `~/` absolute overrides so their real path is `/api/v1/interviews/...`, `/api/v1/offers/...`, or `/api/v1/checklist/...` — noted per endpoint. All actions require the `CanManageRecruitment` policy (`Admin`, `HR` roles) **except** `POST /api/v1/interviews/{id}/feedback`, which is open to any authenticated user but scoped in the handler to the interview's assigned interviewer (Admin/HR can act on anyone's behalf).

### GET /api/v1/candidates — List candidates

**Purpose**: Paginated candidate list for the recruitment pipeline board, with status/designation/search filters.
**URL**: `/api/v1/candidates`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment` (Admin, HR).
**Request**: Query params (`GetCandidatesQuery`): `page` (int, default 1), `pageSize` (int, default 20), `status` (`CandidateStatus` enum: `Applied|Screening|Interviewing|Offered|Hired|Rejected|Withdrawn`, optional), `designationId` (guid, optional), `search` (string, optional).
**Response**: 200, `ApiResponse<PagedResult<CandidateDto>>`. `CandidateDto`: `id, candidateNumber, firstName, lastName, email, phoneNumber, designationId, designationName, departmentId, departmentName, source, appliedDate, status, notes, convertedEmployeeId, isDeleted, createdAtUtc, updatedAtUtc`.
**Validation**: None (query object has no FluentValidation validator).
**Error Codes**: 401 (no/invalid token), 403 (caller lacks Admin/HR role).
**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" "https://api.example.com/api/v1/candidates?status=Interviewing&page=1&pageSize=20"
```
```json
{ "data": { "data": [ { "id": "...", "candidateNumber": "CAN-3F2A1B9C0D", "firstName": "Asha", "lastName": "Rao", "email": "asha@example.com", "designationId": "...", "status": "Interviewing", "appliedDate": "2026-06-01T00:00:00Z", "isDeleted": false, "createdAtUtc": "2026-06-01T10:00:00Z" } ], "page": 1, "pageSize": 20, "totalCount": 1, "totalPages": 1 }, "message": "Request completed successfully." }
```
**Best Practices**:
- Use `status` + `search` together to power the pipeline Kanban view instead of fetching all candidates client-side.
- `pageSize` is silently clamped server-side to a max where similar handlers cap at 100 — do not assume very large pages are honored.

### GET /api/v1/candidates/{id} — Get candidate by ID

**Purpose**: Fetch full detail for a single candidate record.
**URL**: `/api/v1/candidates/{id}`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path param `id` (guid).
**Response**: 200 `ApiResponse<CandidateDto>` (same shape as above); 404 if not found.
**Validation**: None.
**Error Codes**: 401, 403, 404.
**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/candidates/3fae1c2d-.../
```
**Best Practices**: Treat 404 as "not found or soft-deleted" — deleted candidates are excluded by the default repository query.

### POST /api/v1/candidates — Create candidate

**Purpose**: Register a new candidate application (start of the recruitment pipeline).
**URL**: `/api/v1/candidates`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageRecruitment`.
**Request** (`CreateCandidateCommand`):
```json
{
  "firstName": "string (required)",
  "lastName": "string (required)",
  "email": "string (required, email format)",
  "phoneNumber": "string (optional)",
  "designationId": "guid (required)",
  "departmentId": "guid (optional)",
  "source": "string (optional)",
  "appliedDate": "2026-06-01T00:00:00Z (required, non-default)",
  "notes": "string (optional)"
}
```
**Response**: 201, `ApiResponse<object>` with `data: { id }`, `Location` header to `GetById`. Server generates `candidateNumber` as `CAN-XXXXXXXXXX`.
**Validation** (`CreateCandidateCommandValidator`): `FirstName`/`LastName` required, max 100; `Email` required, valid email, max 256; `PhoneNumber` max 30; `Source` max 100; `Notes` max 1000; `AppliedDate` must not be default; `DesignationId` required and must exist (scoped to caller's company); `DepartmentId`, if supplied, must exist (scoped to caller's company).
**Error Codes**: 400 `VALIDATION_ERROR` (e.g. missing email, non-existent designation), 401, 403.
**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/candidates \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"firstName":"Asha","lastName":"Rao","email":"asha@example.com","designationId":"...","appliedDate":"2026-06-01T00:00:00Z"}'
```
```json
{ "data": { "id": "3fae1c2d-..." }, "message": "Candidate created successfully." }
```
**Best Practices**:
- Resolve `designationId`/`departmentId` from the Designations/Departments lookup endpoints first — the validator rejects unknown IDs with a 400, not a 404.
- Candidate starts in the (implicit) `Applied` status; there is no `status` field on create.

### PUT /api/v1/candidates/{id} — Update candidate

**Purpose**: Edit a candidate's contact/designation/notes details (not their pipeline status — use the dedicated status-transition actions for that).
**URL**: `/api/v1/candidates/{id}`
**Method**: PUT
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id` must match body `Id`. Body (`UpdateCandidateCommand`): `id, firstName, lastName, email, phoneNumber, designationId, departmentId, source, notes` (no `appliedDate` — immutable after creation).
**Response**: 204 No Content.
**Validation** (`UpdateCandidateCommandValidator`): same field rules as create minus `AppliedDate`.
**Error Codes**: 400 `ID_MISMATCH` (route/body id differ) or `VALIDATION_ERROR`, 401, 403, 404 `NOT_FOUND`.
**Examples**:
```bash
curl -X PUT https://api.example.com/api/v1/candidates/3fae1c2d-... \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"id":"3fae1c2d-...","firstName":"Asha","lastName":"Rao","email":"asha@example.com","designationId":"...","notes":"Strong React background"}'
```
**Best Practices**: Update does not touch `Status` — use `reject`/`withdraw`/interview/offer flows instead.

### DELETE /api/v1/candidates/{id} — Soft-delete candidate

**Purpose**: Remove a candidate from active views (soft delete; recoverable).
**URL**: `/api/v1/candidates/{id}`
**Method**: DELETE
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`.
**Response**: 204 No Content.
**Validation**: None; no state-machine guard (any status can be deleted).
**Error Codes**: 401, 403, 404.
**Examples**: `curl -X DELETE -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/candidates/3fae1c2d-...`
**Best Practices**: Pair with `POST /{id}/restore` for undo flows in the UI.

### POST /api/v1/candidates/{id}/restore — Restore soft-deleted candidate

**Purpose**: Undo a soft delete.
**URL**: `/api/v1/candidates/{id}/restore`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`. No body.
**Response**: 204 No Content.
**Validation**: None.
**Error Codes**: 401, 403, 404 (id not found even including deleted rows).
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/candidates/3fae1c2d-.../restore`
**Best Practices**: Idempotency is not guaranteed — restoring an already-active candidate is not explicitly guarded against in the handler, it simply re-clears the delete flags.

### POST /api/v1/candidates/{id}/reject — Reject candidate

**Purpose**: Company decision to not proceed with the candidate. Terminal state.
**URL**: `/api/v1/candidates/{id}/reject`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`. Body (optional, `CandidateReasonBody`): `{ "reason": "string" }`.
**Response**: 204 No Content. Reason (if given) is appended to the candidate's `notes`.
**Validation**: None on the body itself.
**Error Codes**: 401, 403, 404 `NOT_FOUND`, 409 `CONFLICT` if candidate is already `Hired`, `Rejected`, or `Withdrawn` ("is already in a terminal state").
**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/candidates/3fae1c2d-.../reject \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"reason":"Not enough experience"}'
```
**Best Practices**: Check current `status` before calling — this is a one-way terminal transition and returns 409 if already terminal.

### POST /api/v1/candidates/{id}/withdraw — Withdraw candidate

**Purpose**: Candidate's own decision to withdraw from the process. Terminal state.
**URL**: `/api/v1/candidates/{id}/withdraw`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`. Body (optional): `{ "reason": "string" }`.
**Response**: 204 No Content.
**Validation**: None.
**Error Codes**: 401, 403, 404, 409 (already terminal — same rule as Reject).
**Examples**: same shape as Reject, path `.../withdraw`.
**Best Practices**: Semantically distinct from `reject` only in who initiated it — both land on a terminal status and both are audit-logged separately (`Withdrawn` vs `Rejected`).

### GET /api/v1/candidates/{id}/attachments — List candidate attachments

**Purpose**: List uploaded files (resume, ID proof, etc.) for a candidate.
**URL**: `/api/v1/candidates/{id}/attachments`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`.
**Response**: 200 `ApiResponse<IEnumerable<CandidateAttachmentDto>>`: `{ id, originalFileName, contentType, fileSizeBytes, uploadedAtUtc }`.
**Validation**: None.
**Error Codes**: 401, 403. (Returns empty list rather than 404 if candidate has no attachments; handler does not appear to 404 on a missing candidate — it queries attachments directly by `CandidateId`.)
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/candidates/3fae1c2d-.../attachments`
**Best Practices**: Use `contentType`/`fileSizeBytes` to render file-type icons and size without a second request.

### POST /api/v1/candidates/{id}/attachments — Upload candidate attachment

**Purpose**: Upload a resume or supporting document for a candidate.
**URL**: `/api/v1/candidates/{id}/attachments`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: multipart/form-data`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`. Multipart field `file` (required). Server builds `UploadCandidateAttachmentCommand` with `CandidateId`, `FileName`, `ContentType`, `Content` (bytes), `RequestingUserId` (from JWT).
**Response**: 200 `ApiResponse<Guid>` — the new attachment id. (Decorated `[ProducesResponseType(201)]` in code but the action returns `Ok(...)`, so the real runtime status is 200.)
**Validation** (`UploadCandidateAttachmentCommandValidator`): `FileName` required, max 255, no invalid filesystem chars, no `..`/`/`/`\`; `ContentType` must be one of `application/pdf`, `image/jpeg`, `image/png`; file extension must match declared content type; `Content` non-empty and ≤ 10 MB; file's magic bytes must match the declared content type (PDF `%PDF-`, JPEG `FFD8FF`, PNG `89504E47...`).
**Error Codes**: 400 `FILE_REQUIRED` (controller-level guard for null/empty file) or `VALIDATION_ERROR` (validator failures — bad type, oversized, spoofed signature), 401, 403, 404 `NOT_FOUND` (candidate doesn't exist).
**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/candidates/3fae1c2d-.../attachments \
  -H "Authorization: Bearer $TOKEN" -F "file=@resume.pdf;type=application/pdf"
```
```json
{ "data": "9c1e2f3a-...", "message": "Attachment uploaded." }
```
**Best Practices**:
- Send the correct `Content-Type` on the multipart part — the signature check will reject a PDF sent as `image/png` even if the bytes are a valid PDF.
- Client-side size-check before upload (10 MB) to avoid a wasted round trip.

### GET /api/v1/candidates/attachments/{attachmentId}/download — Download candidate attachment

**Purpose**: Download the raw file bytes of a candidate attachment.
**URL**: `/api/v1/candidates/attachments/{attachmentId}/download`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `attachmentId`.
**Response**: 200, raw file bytes with the original `Content-Type` and filename (`FileStreamResult`/`File()`); 404 if not found.
**Validation**: None.
**Error Codes**: 401, 403, 404.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" -OJ https://api.example.com/api/v1/candidates/attachments/9c1e2f3a-.../download`
**Best Practices**: This is a binary response, not the `ApiResponse<T>` JSON envelope — handle it separately from other endpoints in your HTTP client.

### GET /api/v1/candidates/{id}/interviews — List candidate's interviews

**Purpose**: Show all interview rounds scheduled for a candidate.
**URL**: `/api/v1/candidates/{id}/interviews`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`.
**Response**: 200 `ApiResponse<IEnumerable<InterviewDto>>`: `{ id, candidateId, interviewerEmployeeId, interviewerName, round, mode, scheduledAtUtc, durationMinutes, status, feedback, rating, outcome, createdAtUtc }`. `mode` ∈ `Onsite|Phone|VideoCall`; `status` ∈ `Scheduled|Completed|Cancelled|NoShow`; `outcome` ∈ `Pending|Passed|Failed|OnHold`.
**Validation**: None.
**Error Codes**: 401, 403.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/candidates/3fae1c2d-.../interviews`
**Best Practices**: Render rounds ordered by `scheduledAtUtc`; the API does not guarantee ordering itself — sort client-side if the backend doesn't (not verified from the query, so sort defensively).

### POST /api/v1/candidates/{id}/interviews — Schedule interview

**Purpose**: Schedule an interview round for a candidate; first interview scheduled auto-advances the candidate from `Applied`/`Screening` to `Interviewing`.
**URL**: `/api/v1/candidates/{id}/interviews`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageRecruitment`.
**Request** (`ScheduleInterviewCommand`, `CandidateId` set from route):
```json
{
  "interviewerEmployeeId": "guid (required)",
  "round": "string (required, e.g. 'Technical Round 1')",
  "mode": "Onsite | Phone | VideoCall",
  "scheduledAtUtc": "2026-06-10T09:00:00Z (required)",
  "durationMinutes": 60
}
```
**Response**: 200 `ApiResponse<Guid>` — new interview id.
**Validation** (`ScheduleInterviewCommandValidator`): `CandidateId` required; `Round` required, max 150; `ScheduledAtUtc` not default; `DurationMinutes` > 0 when supplied; `InterviewerEmployeeId` required and must reference an existing employee.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403, 404 `NOT_FOUND` (candidate not found), 409 `CONFLICT` if the candidate is already `Hired`, `Rejected`, or `Withdrawn`.
**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/candidates/3fae1c2d-.../interviews \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"interviewerEmployeeId":"...","round":"Technical Round 1","mode":"VideoCall","scheduledAtUtc":"2026-06-10T09:00:00Z","durationMinutes":45}'
```
**Best Practices**: Schedule against a candidate not already in a terminal state — check `status` first to avoid the 409.

### POST /api/v1/interviews/{id}/reschedule — Reschedule interview

**Purpose**: Change the date/time (and optionally duration) of a Scheduled interview.
**URL**: `/api/v1/interviews/{id}/reschedule` (absolute route via `~/`, not nested under `/candidates`)
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageRecruitment`.
**Request** (`RescheduleInterviewCommand`, `Id` set from route): `{ "scheduledAtUtc": "...", "durationMinutes": 30 }`.
**Response**: 204 No Content.
**Validation** (`RescheduleInterviewCommandValidator`): `Id` required; `ScheduledAtUtc` not default; `DurationMinutes` > 0 when supplied.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403, 404 `NOT_FOUND`, 409 `CONFLICT` ("Only a Scheduled interview can be rescheduled").
**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/interviews/7ab1.../reschedule \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"scheduledAtUtc":"2026-06-12T09:00:00Z"}'
```
**Best Practices**: Only valid while the interview is `Scheduled` — `Completed`/`Cancelled`/`NoShow` interviews reject with 409.

### POST /api/v1/interviews/{id}/cancel — Cancel interview

**Purpose**: Cancel a Scheduled interview round.
**URL**: `/api/v1/interviews/{id}/cancel`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`. No body.
**Response**: 204 No Content.
**Validation**: None beyond the state check.
**Error Codes**: 401, 403, 404, 409 ("Only a Scheduled interview can be cancelled").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/interviews/7ab1.../cancel`
**Best Practices**: n/a beyond the state guard above.

### POST /api/v1/interviews/{id}/no-show — Mark interview as no-show

**Purpose**: Record that the candidate (or interviewer) didn't show up for a Scheduled interview.
**URL**: `/api/v1/interviews/{id}/no-show`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`. No body.
**Response**: 204 No Content.
**Validation**: None beyond state check.
**Error Codes**: 401, 403, 404, 409 ("Only a Scheduled interview can be marked NoShow").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/interviews/7ab1.../no-show`
**Best Practices**: n/a.

### POST /api/v1/interviews/{id}/feedback — Submit interview feedback

**Purpose**: Record the interviewer's rating/outcome after conducting a Scheduled interview; moves the interview to `Completed`.
**URL**: `/api/v1/interviews/{id}/feedback`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Any authenticated user (**no** `CanManageRecruitment` policy on this action) — but the handler restricts it to the interview's assigned interviewer unless the caller has an `Admin` or `HR` role.
**Request** (`SubmitInterviewFeedbackCommand`, `Id`/`RequestingUserId`/`IsPrivileged` set by controller):
```json
{ "feedback": "string (required)", "rating": 4, "outcome": "Passed | Failed | OnHold" }
```
**Response**: 204 No Content.
**Validation** (`SubmitInterviewFeedbackCommandValidator`): `Id` required; `Feedback` required, max 2000; `Rating` between 1 and 5 inclusive; `Outcome` must be a valid enum value and cannot be `Pending`.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403 `FORBIDDEN` (not the assigned interviewer and not Admin/HR), 404 `NOT_FOUND`, 409 `CONFLICT` ("Only a Scheduled interview can receive feedback").
**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/interviews/7ab1.../feedback \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"feedback":"Strong on system design, weak on algorithms.","rating":4,"outcome":"Passed"}'
```
**Best Practices**: This is the one recruitment endpoint reachable by a non-Admin/HR employee — build the UI so the "Submit Feedback" action only appears for the assigned interviewer or a privileged role.

### GET /api/v1/candidates/{id}/offers — List candidate's offers

**Purpose**: Show all offers issued to a candidate (a candidate can accumulate multiple over time if withdrawn/re-offered).
**URL**: `/api/v1/candidates/{id}/offers`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`.
**Response**: 200 `ApiResponse<IEnumerable<OfferDto>>`: `{ id, offerNumber, candidateId, designationId, designationName, departmentId, departmentName, offeredSalary, joiningDate, status, issuedAtUtc, respondedAtUtc, expiresAtUtc, notes, hasDocument, createdAtUtc }`. `status` ∈ `Draft|Sent|Accepted|Rejected|Withdrawn|Expired`.
**Validation**: None.
**Error Codes**: 401, 403.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/candidates/3fae1c2d-.../offers`
**Best Practices**: `hasDocument` tells you whether `GET /api/v1/offers/{id}/download` will return a PDF (only true once the offer has been `Sent`).

### POST /api/v1/candidates/{id}/offers — Create offer

**Purpose**: Draft a new job offer for a candidate. Does not send it yet — a separate `send` call generates and issues the PDF.
**URL**: `/api/v1/candidates/{id}/offers`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageRecruitment`.
**Request** (`CreateOfferCommand`, `CandidateId` from route):
```json
{
  "designationId": "guid (required)",
  "departmentId": "guid (optional)",
  "offeredSalary": 1200000.00,
  "joiningDate": "2026-07-01T00:00:00Z (required)",
  "expiresAtUtc": "2026-06-20T00:00:00Z (optional, must be future)",
  "notes": "string (optional)"
}
```
**Response**: 200 `ApiResponse<Guid>` — new offer id, generated `offerNumber` (`OFR-XXXXXXXXXX`), status starts `Draft`.
**Validation** (`CreateOfferCommandValidator`): `CandidateId` required; `OfferedSalary` > 0; `JoiningDate` not default; `ExpiresAtUtc`, if given, must be in the future (relative to `DateTime.UtcNow`); `Notes` max 1000; `DesignationId` required and must exist; `DepartmentId`, if given, must exist.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403, 404 `NOT_FOUND` (candidate), 409 `CONFLICT` (candidate in a terminal state).
**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/candidates/3fae1c2d-.../offers \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"designationId":"...","offeredSalary":1200000,"joiningDate":"2026-07-01T00:00:00Z"}'
```
**Best Practices**: Multiple `Draft` offers can theoretically exist per candidate — only one should be progressed via `send` at a time; the domain does not enforce "one active offer" for you.

### POST /api/v1/offers/{id}/send — Send offer

**Purpose**: Issue a Draft offer — generates the offer letter PDF, stores it, and moves both the offer to `Sent` and the candidate to `Offered`.
**URL**: `/api/v1/offers/{id}/send` (absolute route)
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`. No body.
**Response**: 204 No Content.
**Validation**: None beyond state check.
**Error Codes**: 401, 403, 404 `NOT_FOUND`, 409 `CONFLICT` ("Only a Draft offer can be sent").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/offers/9f1a.../send`
**Best Practices**: This is a side-effecting, non-idempotent action (PDF generation + blob storage write) — guard the "Send" button against double-clicks client-side; a second call 409s cleanly but still costs a round trip.

### POST /api/v1/offers/{id}/accept — Accept offer

**Purpose**: Record the candidate's acceptance of a Sent offer; seeds the default onboarding checklist (5 fixed items: Offer Letter Signed, ID Proof Submitted, Bank Details Collected, Laptop/Asset Allocated, Induction Completed).
**URL**: `/api/v1/offers/{id}/accept`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`. No body.
**Response**: 204 No Content.
**Validation**: None beyond state check.
**Error Codes**: 401, 403, 404, 409 ("Only a Sent offer can be accepted").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/offers/9f1a.../accept`
**Best Practices**: After calling this, immediately `GET /api/v1/candidates/{id}/checklist` to show the seeded items — they exist server-side as soon as this call succeeds.

### POST /api/v1/offers/{id}/reject — Reject offer

**Purpose**: Record the candidate's rejection of a Sent offer.
**URL**: `/api/v1/offers/{id}/reject`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`. Body (optional): `{ "reason": "string, max 500" }`.
**Response**: 204 No Content.
**Validation** (`RejectOfferCommandValidator`): `Id` required; `Reason` max 500.
**Error Codes**: 400 `VALIDATION_ERROR` (reason too long), 401, 403, 404, 409 ("Only a Sent offer can be rejected").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"reason":"Accepted a competing offer"}' https://api.example.com/api/v1/offers/9f1a.../reject`
**Best Practices**: This does not change the candidate's own status — only the offer's. Follow up with `POST /candidates/{id}/reject` or a new offer as appropriate.

### POST /api/v1/offers/{id}/withdraw — Withdraw offer

**Purpose**: Company pulls back a Draft or Sent offer before the candidate responds.
**URL**: `/api/v1/offers/{id}/withdraw`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`. No body.
**Response**: 204 No Content.
**Validation**: None beyond state check.
**Error Codes**: 401, 403, 404, 409 ("Only a Draft or Sent offer can be withdrawn").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/offers/9f1a.../withdraw`
**Best Practices**: n/a.

### GET /api/v1/offers/{id}/download — Download offer letter PDF

**Purpose**: Download the generated offer letter PDF for a Sent (or later-state) offer.
**URL**: `/api/v1/offers/{id}/download`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`.
**Response**: 200, raw PDF bytes; 404 if no document exists (e.g. offer still `Draft`).
**Validation**: None.
**Error Codes**: 401, 403, 404.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" -OJ https://api.example.com/api/v1/offers/9f1a.../download`
**Best Practices**: Check `OfferDto.hasDocument` before showing a download link.

### GET /api/v1/candidates/{id}/checklist — List onboarding checklist items

**Purpose**: Show onboarding checklist progress for a candidate (seeded when their offer is accepted; can be augmented).
**URL**: `/api/v1/candidates/{id}/checklist`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `id`.
**Response**: 200 `ApiResponse<IEnumerable<OnboardingChecklistItemDto>>`: `{ id, candidateId, itemName, isCompleted, completedAtUtc, notes, createdAtUtc }`.
**Validation**: None.
**Error Codes**: 401, 403.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/candidates/3fae1c2d-.../checklist`
**Best Practices**: n/a.

### POST /api/v1/candidates/{id}/checklist — Add checklist item

**Purpose**: Add a custom onboarding checklist item on top of the default seeded set.
**URL**: `/api/v1/candidates/{id}/checklist`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageRecruitment`.
**Request** (`AddChecklistItemCommand`, `CandidateId` from route): `{ "itemName": "string (required, max 200)", "notes": "string (optional, max 500)" }`.
**Response**: 200 `ApiResponse<Guid>` — new item id.
**Validation** (`AddChecklistItemCommandValidator`): `CandidateId` required; `ItemName` required, max 200; `Notes` max 500.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403, 404 `NOT_FOUND` (candidate).
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"itemName":"NDA Signed"}' https://api.example.com/api/v1/candidates/3fae1c2d-.../checklist`
**Best Practices**: Not gated on offer-acceptance status — items can technically be added at any candidate status.

### POST /api/v1/checklist/{itemId}/complete — Complete checklist item

**Purpose**: Mark an onboarding checklist item done.
**URL**: `/api/v1/checklist/{itemId}/complete` (absolute route)
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageRecruitment`.
**Request**: Path `itemId`. Body (optional, `CandidateReasonBody`): `{ "reason": "string" }` — mapped to the item's `Notes` if supplied (overwrites, does not append).
**Response**: 204 No Content.
**Validation**: None (`CompleteChecklistItemCommand` has no registered validator).
**Error Codes**: 401, 403, 404 `NOT_FOUND`.
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"reason":"Verified by HR on 2026-06-15"}' https://api.example.com/api/v1/checklist/aa11.../complete`
**Best Practices**: Idempotent-ish — completing an already-completed item just re-stamps `completedAtUtc`/`completedBy`, no error.

### POST /api/v1/candidates/{id}/convert-to-employee — Convert candidate to employee

**Purpose**: Terminal recruitment action — creates the real `Employee` record from a candidate with an `Accepted` offer, and marks the candidate `Hired`.
**URL**: `/api/v1/candidates/{id}/convert-to-employee`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageRecruitment`.
**Request** (`ConvertCandidateToEmployeeCommand`, `CandidateId` from route):
```json
{
  "employeeCode": "string (required, max 50, unique per company)",
  "officeLocationId": "guid (required)",
  "teamId": "guid (optional)",
  "managerId": "guid (optional)",
  "joinDate": "2026-07-01T00:00:00Z (optional — defaults to the accepted offer's JoiningDate)"
}
```
**Response**: 200, `ApiResponse<Guid>` — new employee id. (Decorated `[ProducesResponseType(201)]` in code but the action returns `Ok(...)`, so the real runtime status is 200.)
**Validation** (`ConvertCandidateToEmployeeCommandValidator`): `CandidateId` required; `EmployeeCode` required, max 50; `OfficeLocationId` required and must exist (scoped to caller's company).
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403, 404 `NOT_FOUND` (candidate), 409 `CONFLICT` — three distinct business rules can trigger this: candidate already converted/`Hired`; candidate has no `Accepted` offer; `EmployeeCode` or the candidate's email already exists on an Employee record.
**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/candidates/3fae1c2d-.../convert-to-employee \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"employeeCode":"EMP-2045","officeLocationId":"..."}'
```
```json
{ "data": "b7c8d9e0-...", "message": "Candidate converted to employee." }
```
**Best Practices**: Confirm the candidate has an `Accepted` offer (`GET .../offers`, filter `status == "Accepted"`) before calling — the handler picks the first accepted offer it finds and derives `DepartmentId`/`DesignationId`/default `JoinDate` from it, so an inconsistent candidate/offer state produces a 409 rather than a partial employee.

---

## Performance API

Controller route: `[Route("api/v1")]` (so paths are `/api/v1/goals`, `/api/v1/reviews`, `/api/v1/promotions` — **not** prefixed with `/performance`). All list/get/self-service endpoints carry only the class-level `[Authorize]` and are scoped inside the handler (an Employee sees their own records; a Manager additionally sees direct reports'; Admin/HR see everything). Create/update/delete-class actions require `CanManagePerformance` (`Admin`, `HR`, `Manager`), further scoped in the handler so a Manager can only act on their own direct reports. Promotion approve/reject/delete/restore require the stricter `CanApprovePromotions` (`Admin`, `HR` only).

### Goals

#### GET /api/v1/goals — List goals

**Purpose**: Paginated goal list, filterable by employee/status/category.
**URL**: `/api/v1/goals`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user; scoped in-handler — non-privileged callers see only their own goals plus (if `Manager`) their direct reports'.
**Request** (`GetGoalsQuery`): `page`, `pageSize` (default 20, capped at 100), `employeeId` (guid, optional), `status` (`NotStarted|InProgress|Completed|Cancelled`, optional), `category` (string, optional).
**Response**: 200 `ApiResponse<PagedResult<PerformanceGoalDto>>`. `PerformanceGoalDto`: `id, goalNumber, employeeId, employeeName, title, description, category, startDate, targetDate, weight, status, progressPercent, kpis: PerformanceGoalKpiDto[], isDeleted, createdAtUtc, updatedAtUtc`. `PerformanceGoalKpiDto`: `id, goalId, name, targetValue, currentValue, unit, notes, createdAtUtc, updatedAtUtc`.
**Validation**: None.
**Error Codes**: 401, 403 `FORBIDDEN` if a non-privileged caller explicitly filters by an `employeeId` outside their own scope ("You can only view goals for your own team.").
**Examples**: `curl -H "Authorization: Bearer $TOKEN" "https://api.example.com/api/v1/goals?status=InProgress"`
**Best Practices**: Omit `employeeId` to get "everything I'm allowed to see" rather than guessing scope client-side; the server auto-scopes when it's left off.

#### GET /api/v1/goals/{id} — Get goal by ID

**Purpose**: Fetch a single goal with its KPIs.
**URL**: `/api/v1/goals/{id}`
**Method**: GET
**Authentication**: Any authenticated user; non-privileged callers get `null` (→ controller returns 404) unless they are the goal's own employee or that employee's manager — existence is not disclosed to outsiders, matching the Task "hide existence" pattern.
**Request**: Path `id`.
**Response**: 200 `ApiResponse<PerformanceGoalDto>`; 404 if not found or not in the caller's scope.
**Validation**: None.
**Error Codes**: 401, 404.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/goals/aa11.../`

#### POST /api/v1/goals — Create goal

**Purpose**: Set a new performance goal for an employee.
**URL**: `/api/v1/goals`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManagePerformance` (Admin, HR, Manager); handler additionally requires a Manager caller to be the target employee's actual manager.
**Request** (`CreateGoalCommand`):
```json
{
  "employeeId": "guid (required, must exist)",
  "title": "string (required, max 200)",
  "description": "string (optional, max 2000)",
  "category": "string (optional, max 100)",
  "startDate": "2026-06-01T00:00:00Z (required)",
  "targetDate": "2026-09-30T00:00:00Z (required, >= startDate)",
  "weight": 25.0
}
```
**Response**: 201 `ApiResponse<object>` `{ id }`, `Location` header. New goal starts `NotStarted`, `progressPercent: 0`, `goalNumber` = `GOL-XXXXXXXXXX`.
**Validation** (`CreateGoalCommandValidator`): `Title` required, max 200; `Description` max 2000; `Category` max 100; `StartDate` not default; `TargetDate` not default and ≥ `StartDate`; `Weight` (if given) between 0 and 100; `EmployeeId` required and must reference an existing employee.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403 `FORBIDDEN` ("You can only create goals for your own direct reports." — thrown for a non-privileged Manager targeting someone who isn't their report).
**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/goals \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"employeeId":"...","title":"Ship v2 API","startDate":"2026-06-01T00:00:00Z","targetDate":"2026-09-30T00:00:00Z","weight":25}'
```
**Best Practices**: A Manager creating a goal for a non-report gets a 403, not a 404 — don't rely on the error code alone to detect "employee doesn't exist" vs. "not your report" (both surface differently: unknown employee is a 400 from the validator; wrong-report is a 403 from the handler).

#### PUT /api/v1/goals/{id} — Update goal

**Purpose**: Edit a goal's title/description/category/target date/weight/status.
**URL**: `/api/v1/goals/{id}`
**Method**: PUT
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManagePerformance`; handler requires privileged role or being the target employee's manager.
**Request**: Path `id` must equal body `Id`. Body (`UpdateGoalCommand`): `id, title, description, category, targetDate, weight, status` (`NotStarted|InProgress|Completed|Cancelled`).
**Response**: 204 No Content.
**Validation** (`UpdateGoalCommandValidator`): `Id` required; `Title` required, max 200; `Description` max 2000; `Category` max 100; `TargetDate` not default; `Weight` 0–100 if given; `Status` must be a valid enum value.
**Error Codes**: 400 `ID_MISMATCH` or `VALIDATION_ERROR`, 401, 403 `FORBIDDEN`, 404 `NOT_FOUND`.
**Examples**: `curl -X PUT -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"id":"aa11...","title":"Ship v2 API","targetDate":"2026-10-15T00:00:00Z","status":"InProgress"}' https://api.example.com/api/v1/goals/aa11.../`
**Best Practices**: This endpoint can also directly set `Status`, unlike `progress` (percent-only) — use it for the manager-driven "mark Completed/Cancelled" action, and `/progress` for the employee-driven percent slider.

#### POST /api/v1/goals/{id}/progress — Update goal progress

**Purpose**: Employee-facing progress-percent update on their own goal (manager/Admin/HR can also do it).
**URL**: `/api/v1/goals/{id}/progress`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Any authenticated user; handler restricts to the goal's own employee, their manager, or Admin/HR.
**Request** (`UpdateGoalProgressCommand`, `Id` from route): `{ "progressPercent": 60 }`.
**Response**: 204 No Content.
**Validation** (`UpdateGoalProgressCommandValidator`): `Id` required; `ProgressPercent` between 0 and 100 inclusive.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403 `FORBIDDEN` ("You can only update progress on your own goals."), 404 `NOT_FOUND`.
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"progressPercent":60}' https://api.example.com/api/v1/goals/aa11.../progress`
**Best Practices**: Does not auto-transition `Status` (e.g. reaching 100% does not flip status to `Completed`) — the client/manager must still call `PUT /goals/{id}` to set `status: "Completed"`.

#### DELETE /api/v1/goals/{id} — Soft-delete goal

**Purpose**: Remove a goal from active views.
**URL**: `/api/v1/goals/{id}`
**Method**: DELETE
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManagePerformance`; handler requires privileged role or being the employee's manager.
**Request**: Path `id`.
**Response**: 204 No Content.
**Validation**: None.
**Error Codes**: 401, 403, 404.
**Examples**: `curl -X DELETE -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/goals/aa11.../`

#### POST /api/v1/goals/{id}/restore — Restore soft-deleted goal

**Purpose**: Undo a goal soft delete.
**URL**: `/api/v1/goals/{id}/restore`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManagePerformance`; handler requires privileged role or being the employee's manager.
**Request**: Path `id`. No body.
**Response**: 204 No Content.
**Validation**: None.
**Error Codes**: 401, 403, 404 `NOT_FOUND`, 409 `CONFLICT` ("Goal ... is not deleted" — restore called on a non-deleted goal).
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/goals/aa11.../restore`

#### POST /api/v1/goals/{id}/kpis — Add KPI to goal

**Purpose**: Attach a measurable KPI to a goal ("KPI Tracking").
**URL**: `/api/v1/goals/{id}/kpis`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManagePerformance`; handler requires privileged role or being the goal owner's manager.
**Request** (`AddGoalKpiCommand`, `GoalId` from route): `{ "name": "string (required, max 200)", "targetValue": 100.0, "unit": "string (optional, max 30)", "notes": "string (optional, max 1000)" }`.
**Response**: 200 `ApiResponse<Guid>` — new KPI id. `currentValue` starts at 0. (Decorated `[ProducesResponseType(201)]` in code but the action returns `Ok(...)`, so the real runtime status is 200.)
**Validation** (`AddGoalKpiCommandValidator`): `GoalId` required; `Name` required, max 200; `TargetValue` ≥ 0; `Unit` max 30; `Notes` max 1000.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403 `FORBIDDEN`, 404 `NOT_FOUND` (goal).
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"name":"API endpoints shipped","targetValue":20,"unit":"endpoints"}' https://api.example.com/api/v1/goals/aa11.../kpis`

#### POST /api/v1/kpis/{kpiId}/progress — Update KPI progress

**Purpose**: Update a KPI's current value toward its target.
**URL**: `/api/v1/kpis/{kpiId}/progress`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Any authenticated user; handler restricts to the parent goal's owner, their manager, or Admin/HR.
**Request** (`UpdateGoalKpiProgressCommand`, `Id` from route): `{ "currentValue": 14.0, "notes": "string (optional, max 1000)" }`.
**Response**: 204 No Content.
**Validation** (`UpdateGoalKpiProgressCommandValidator`): `Id` required; `CurrentValue` ≥ 0; `Notes` max 1000.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403 `FORBIDDEN`, 404 `NOT_FOUND` (KPI, or its parent goal).
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"currentValue":14}' https://api.example.com/api/v1/kpis/cc22.../progress`

### Performance Reviews

#### GET /api/v1/reviews — List reviews

**Purpose**: Paginated review list, filterable by employee/reviewer/status.
**URL**: `/api/v1/reviews`
**Method**: GET
**Authentication**: Any authenticated user; scoped in-handler — sees reviews where they're the subject or reviewer, plus (if Manager) their reports', plus (if Admin/HR) everything.
**Request** (`GetReviewsQuery`): `page`, `pageSize`, `employeeId` (optional), `reviewerEmployeeId` (optional), `status` (`Draft|SelfAssessmentSubmitted|Completed|Cancelled`, optional).
**Response**: 200 `ApiResponse<PagedResult<PerformanceReviewDto>>`. `PerformanceReviewDto`: `id, reviewNumber, employeeId, employeeName, reviewerEmployeeId, reviewerName, reviewPeriodStart, reviewPeriodEnd, status, selfAssessment, managerAssessment, overallRating, selfSubmittedAtUtc, completedAtUtc, notes, isDeleted, createdAtUtc, updatedAtUtc`.
**Validation**: None.
**Error Codes**: 401.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" "https://api.example.com/api/v1/reviews?status=Draft"`

#### GET /api/v1/reviews/{id} — Get review by ID

**URL**: `/api/v1/reviews/{id}` **Method**: GET **Authentication**: Any authenticated user, scoped as above.
**Purpose**: Fetch a single review's full detail including self/manager assessment text.
**Request**: Path `id`. **Response**: 200 `ApiResponse<PerformanceReviewDto>`; 404 if not found. **Validation**: None. **Error Codes**: 401, 404.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/reviews/dd33.../`

#### POST /api/v1/reviews — Start review cycle

**Purpose**: Start a new review cycle (`Draft` status) for an employee.
**URL**: `/api/v1/reviews`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManagePerformance`; a non-privileged Manager must be the target's manager **and** must name themselves as `reviewerEmployeeId` (cannot delegate to a third party — only Admin/HR can set an arbitrary reviewer).
**Request** (`CreateReviewCommand`):
```json
{
  "employeeId": "guid (required, must exist)",
  "reviewerEmployeeId": "guid (required, must exist, must differ from employeeId)",
  "reviewPeriodStart": "2026-01-01T00:00:00Z (required)",
  "reviewPeriodEnd": "2026-06-30T00:00:00Z (required, >= start)",
  "notes": "string (optional, max 1000)"
}
```
**Response**: 201 `ApiResponse<object>` `{ id }`. `reviewNumber` = `REV-XXXXXXXXXX`, starts `Draft`.
**Validation** (`CreateReviewCommandValidator`): `ReviewPeriodStart`/`ReviewPeriodEnd` not default, end ≥ start; `Notes` max 1000; `EmployeeId`/`ReviewerEmployeeId` required and must exist; employee cannot review themselves.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403 `FORBIDDEN` (Manager not the target's manager, or trying to name someone else as reviewer).
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"employeeId":"...","reviewerEmployeeId":"...","reviewPeriodStart":"2026-01-01T00:00:00Z","reviewPeriodEnd":"2026-06-30T00:00:00Z"}' https://api.example.com/api/v1/reviews`
**Best Practices**: When building a Manager-facing "start review" form, hardcode/hide the reviewer field to the current user's own employee id — the API will 403 any other value from a non-privileged caller.

#### POST /api/v1/reviews/{id}/self-assessment — Submit self-assessment

**Purpose**: Employee submits their self-assessment text. `Draft` → `SelfAssessmentSubmitted`.
**URL**: `/api/v1/reviews/{id}/self-assessment`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Any authenticated user; handler restricts to the reviewed employee themselves, or Admin/HR.
**Request** (`SubmitSelfAssessmentCommand`, `Id` from route): `{ "selfAssessment": "string (required, max 4000)" }`.
**Response**: 204 No Content.
**Validation** (`SubmitSelfAssessmentCommandValidator`): `Id` required; `SelfAssessment` required, max 4000.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403 `FORBIDDEN` ("Only the reviewed employee can submit their self-assessment."), 404 `NOT_FOUND`, 409 `CONFLICT` ("Review ... must be Draft to submit a self-assessment").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"selfAssessment":"Delivered X, Y, Z this half..."}' https://api.example.com/api/v1/reviews/dd33.../self-assessment`
**Best Practices**: Only callable once — the review must be exactly `Draft`; a second call after submission 409s.

#### POST /api/v1/reviews/{id}/manager-review — Submit manager review

**Purpose**: Reviewer submits their assessment and overall rating, completing the cycle. `Draft`/`SelfAssessmentSubmitted` → `Completed`.
**URL**: `/api/v1/reviews/{id}/manager-review`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Any authenticated user; handler restricts to the assigned reviewer, or Admin/HR.
**Request** (`SubmitManagerReviewCommand`, `Id` from route): `{ "managerAssessment": "string (required, max 4000)", "overallRating": 4.5 }`.
**Response**: 204 No Content.
**Validation** (`SubmitManagerReviewCommandValidator`): `Id` required; `ManagerAssessment` required, max 4000; `OverallRating` between 1 and 5 inclusive.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403 `FORBIDDEN` ("Only the assigned reviewer can submit the manager review."), 404 `NOT_FOUND`, 409 `CONFLICT` ("Review ... must be Draft or SelfAssessmentSubmitted").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"managerAssessment":"Strong half overall.","overallRating":4.5}' https://api.example.com/api/v1/reviews/dd33.../manager-review`
**Best Practices**: Manager review can be submitted even if the employee never completed their self-assessment (`Draft` is a valid starting state, not just `SelfAssessmentSubmitted`) — don't hard-block the UI on self-assessment being present.

#### POST /api/v1/reviews/{id}/cancel — Cancel review

**Purpose**: Cancel a review that hasn't completed yet.
**URL**: `/api/v1/reviews/{id}/cancel`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManagePerformance` at the controller; handler further restricts to the assigned reviewer or Admin/HR.
**Request**: Path `id`. Body (optional): `{ "reason": "string, max 500" }`, appended to `notes`.
**Response**: 204 No Content.
**Validation** (`CancelReviewCommandValidator`): `Id` required; `Reason` max 500.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403 `FORBIDDEN`, 404 `NOT_FOUND`, 409 `CONFLICT` ("Review ... is already Completed/Cancelled and cannot be cancelled").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"reason":"Employee moved teams mid-cycle"}' https://api.example.com/api/v1/reviews/dd33.../cancel`

#### DELETE /api/v1/reviews/{id} — Soft-delete review

**Purpose**: Remove a review from active views.
**URL**: `/api/v1/reviews/{id}`
**Method**: DELETE
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManagePerformance`; handler restricts to the assigned reviewer or Admin/HR.
**Request**: Path `id`. **Response**: 204 No Content. **Validation**: None. **Error Codes**: 401, 403 `FORBIDDEN`, 404.
**Examples**: `curl -X DELETE -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/reviews/dd33.../`

#### POST /api/v1/reviews/{id}/restore — Restore soft-deleted review

**URL**: `/api/v1/reviews/{id}/restore` **Method**: POST
**Purpose**: Undo a review soft delete.
**Authentication**: Policy `CanManagePerformance`; handler restricts to the assigned reviewer or Admin/HR.
**Request**: Path `id`. No body. **Response**: 204 No Content. **Validation**: None.
**Error Codes**: 401, 403 `FORBIDDEN`, 404 `NOT_FOUND`, 409 `CONFLICT` ("Review ... is not deleted").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/reviews/dd33.../restore`

### Promotions

#### GET /api/v1/promotions — List promotions

**Purpose**: Paginated promotion proposal list.
**URL**: `/api/v1/promotions` **Method**: GET
**Authentication**: Any authenticated user; scoped in-handler (own, + reports if Manager, + all if Admin/HR).
**Request** (`GetPromotionsQuery`): `page`, `pageSize`, `employeeId` (optional), `status` (`Proposed|Approved|Rejected|Withdrawn`, optional).
**Response**: 200 `ApiResponse<PagedResult<PromotionDto>>`. `PromotionDto`: `id, promotionNumber, employeeId, employeeName, fromDesignationId, fromDesignationName, toDesignationId, toDesignationName, fromDepartmentId, fromDepartmentName, toDepartmentId, toDepartmentName, effectiveDate, reason, status, proposedByUserId, decidedByUserId, decidedAtUtc, decisionNotes, appliedAtUtc, isDeleted, createdAtUtc, updatedAtUtc`.
**Validation**: None. **Error Codes**: 401.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" "https://api.example.com/api/v1/promotions?status=Proposed"`

#### GET /api/v1/promotions/{id} — Get promotion by ID

**URL**: `/api/v1/promotions/{id}` **Method**: GET
**Purpose**: Fetch a single promotion proposal/decision record.
**Authentication**: Any authenticated user, scoped as above.
**Request**: Path `id`. **Response**: 200 `ApiResponse<PromotionDto>`; 404 if not found. **Error Codes**: 401, 404.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/promotions/ee44.../`

#### POST /api/v1/promotions — Propose promotion

**Purpose**: Propose a designation/department change for an employee, pending approval.
**URL**: `/api/v1/promotions`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManagePerformance`; handler requires privileged role or being the target employee's manager.
**Request** (`ProposePromotionCommand`):
```json
{
  "employeeId": "guid (required, must exist)",
  "toDesignationId": "guid (required, must exist)",
  "toDepartmentId": "guid (optional, must exist if given)",
  "effectiveDate": "2026-08-01T00:00:00Z (required)",
  "reason": "string (required, max 1000)"
}
```
`fromDesignationId`/`fromDepartmentId` are captured server-side from the employee's current record — do not send them.
**Response**: 201 `ApiResponse<object>` `{ id }`. `promotionNumber` = `PRO-XXXXXXXXXX`, status starts `Proposed`.
**Validation** (`ProposePromotionCommandValidator`): `EffectiveDate` not default; `Reason` required, max 1000; `EmployeeId` required and must exist; `ToDesignationId` required and must exist; `ToDepartmentId`, if given, must exist.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403 `FORBIDDEN` (non-privileged caller not the employee's manager), 404 `NOT_FOUND` (employee), 409 `CONFLICT` ("The target designation/department must differ from the employee's current one").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"employeeId":"...","toDesignationId":"...","effectiveDate":"2026-08-01T00:00:00Z","reason":"Consistently exceeding goals"}' https://api.example.com/api/v1/promotions`

#### POST /api/v1/promotions/{id}/approve — Approve promotion

**Purpose**: Approve a Proposed promotion. If `effectiveDate` has already arrived, the designation/department change is applied to the Employee record immediately; otherwise it's deferred to a daily sweep job.
**URL**: `/api/v1/promotions/{id}/approve`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanApprovePromotions` (Admin, HR only — a Manager cannot approve their own proposal).
**Request**: Path `id`. Body (optional): `{ "decisionNotes": "string, max 1000" }`.
**Response**: 204 No Content.
**Validation** (`ApprovePromotionCommandValidator`): `Id` required; `DecisionNotes` max 1000.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403, 404 `NOT_FOUND`, 409 `CONFLICT` ("Promotion ... must be Proposed to approve").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"decisionNotes":"Approved effective next cycle"}' https://api.example.com/api/v1/promotions/ee44.../approve`
**Best Practices**: Check `appliedAtUtc` on the resulting `PromotionDto` (via a follow-up GET) to know whether the employee's designation changed immediately or is still pending the effective date.

#### POST /api/v1/promotions/{id}/reject — Reject promotion

**Purpose**: Reject a Proposed promotion.
**URL**: `/api/v1/promotions/{id}/reject`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanApprovePromotions` (Admin, HR only).
**Request**: Path `id`. Body (optional): `{ "decisionNotes": "string, max 1000" }`.
**Response**: 204 No Content.
**Validation** (`RejectPromotionCommandValidator`): `Id` required; `DecisionNotes` max 1000.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403, 404, 409 `CONFLICT` ("Promotion ... must be Proposed to reject").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"decisionNotes":"Not yet, revisit next cycle"}' https://api.example.com/api/v1/promotions/ee44.../reject`

#### POST /api/v1/promotions/{id}/withdraw — Withdraw promotion

**Purpose**: The proposer pulls back their own proposed promotion before a decision is made.
**URL**: `/api/v1/promotions/{id}/withdraw`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManagePerformance` at the controller; handler further restricts to the original proposer or Admin/HR.
**Request**: Path `id`. No body.
**Response**: 204 No Content.
**Validation**: None.
**Error Codes**: 401, 403 `FORBIDDEN` ("Only the user who proposed this promotion can withdraw it."), 404, 409 `CONFLICT` ("Promotion ... must be Proposed to withdraw").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/promotions/ee44.../withdraw`

#### DELETE /api/v1/promotions/{id} — Soft-delete promotion

**Purpose**: Remove a promotion record from active views.
**URL**: `/api/v1/promotions/{id}` **Method**: DELETE
**Authentication**: Policy `CanApprovePromotions` (Admin, HR only).
**Request**: Path `id`. **Response**: 204 No Content. **Validation**: None. **Error Codes**: 401, 403, 404.
**Examples**: `curl -X DELETE -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/promotions/ee44.../`

#### POST /api/v1/promotions/{id}/restore — Restore soft-deleted promotion

**URL**: `/api/v1/promotions/{id}/restore` **Method**: POST
**Authentication**: Policy `CanApprovePromotions` (Admin, HR only).
**Request**: Path `id`. No body. **Response**: 204 No Content.
**Error Codes**: 401, 403, 404 `NOT_FOUND`. Unlike Goal/Review restore, `RestorePromotionCommandHandler` has no `IsDeleted` guard — calling this on an already-active promotion does not 409, it just re-clears (already-null) delete fields.
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/promotions/ee44.../restore`

---

## Tasks API

Controller route: `[Route("api/v1/tasks")]`. Class-level `[Authorize]` plus `[EnableRateLimiting("WriteActionPolicy")]` (100 req/60s/IP). Admin-only for create/edit/reassign/cancel (`CanManageTasks` policy = `Admin` role only). Everything else (accept/reject/start/progress/complete/comments/attachments) is open to any authenticated user but scoped in the handler to the task's assignee, with Admin able to act on behalf of anyone.

### GET /api/v1/tasks — List tasks

**Purpose**: Paginated task list. Non-Admin callers only ever see tasks assigned to them, regardless of any `assignedEmployeeId` filter they pass.
**URL**: `/api/v1/tasks`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user; server overrides `assignedEmployeeId` to the caller's own employee id unless they're `Admin`.
**Request** (`GetTasksQuery`): `page`, `pageSize` (default 20, capped 100), `assignedEmployeeId` (guid, optional — ignored/overridden for non-Admins), `clientId` (guid, optional), `status` (`Assigned|Accepted|Rejected|InProgress|OnHold|Completed|Cancelled`, optional), `priority` (`Low|Medium|High|Critical`, optional).
**Response**: 200 `ApiResponse<PagedResult<TaskItemDto>>`. `TaskItemDto`: `id, taskNumber, title, description, clientId, clientName, clientAddress, clientLatitude, clientLongitude, assignedEmployeeId, assignedEmployeeName, assignedByUserId, assignedDate, dueDate, priority, status, notes, completedAtUtc, createdAtUtc, updatedAtUtc`.
**Validation**: None.
**Error Codes**: 401.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" "https://api.example.com/api/v1/tasks?status=InProgress"`
**Best Practices**: A non-Admin employee never needs to pass `assignedEmployeeId` — it's forced server-side; passing someone else's id silently has no effect rather than erroring.

### GET /api/v1/tasks/{id} — Get task by ID

**Purpose**: Fetch a single task's full detail (including client location for field visits).
**URL**: `/api/v1/tasks/{id}`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user; non-Admin callers get 404 (not 403) if the task isn't theirs — existence is not disclosed to non-assignees.
**Request**: Path `id`.
**Response**: 200 `ApiResponse<TaskItemDto>`; 404 if not found or not the caller's task.
**Validation**: None.
**Error Codes**: 401, 404.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/tasks/ff55.../`
**Best Practices**: Don't distinguish "doesn't exist" from "not yours" in the UI — the API deliberately doesn't either.

### POST /api/v1/tasks — Create task

**Purpose**: Create and assign a new task to an employee.
**URL**: `/api/v1/tasks`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageTasks` (Admin only).
**Request** (`CreateTaskCommand`):
```json
{
  "title": "string (required, max 200)",
  "description": "string (optional, max 2000)",
  "clientId": "guid (optional, must be an active client if given)",
  "assignedEmployeeId": "guid (required, must exist)",
  "dueDate": "2026-06-20T00:00:00Z (optional, cannot be in the past)",
  "priority": "Low | Medium | High | Critical (default Medium)",
  "notes": "string (optional, max 1000)"
}
```
**Response**: 201 `ApiResponse<TaskItemDto>` (full DTO, not just an id), `Location` header. `taskNumber` = `TSK-XXXXXXXXXX`, status starts `Assigned`, `assignedDate` = now, `assignedByUserId` = caller.
**Validation** (`CreateTaskCommandValidator`): `Title` required, max 200; `Description` max 2000; `Notes` max 1000; `AssignedEmployeeId` required and must exist; `ClientId`, if given, must reference an active client (inactive clients rejected); `DueDate`, if given, cannot be before today (UTC date).
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403, 429 `RATE_LIMIT_EXCEEDED`.
**Examples**:
```bash
curl -X POST https://api.example.com/api/v1/tasks \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"title":"Site visit — AC servicing","assignedEmployeeId":"...","clientId":"...","priority":"High","dueDate":"2026-06-20T00:00:00Z"}'
```
**Best Practices**: Look up the client's `isActive` flag client-side before letting a user pick it, to pre-empt the 400.

### PUT /api/v1/tasks/{id} — Edit task

**Purpose**: Edit task details (title/description/client/due date/priority/notes). Rejected once the task is `Completed` or `Cancelled`.
**URL**: `/api/v1/tasks/{id}`
**Method**: PUT
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageTasks` (Admin only).
**Request**: Path `id` must equal body `Id`. Body (`UpdateTaskCommand`): `id, title, description, clientId, dueDate, priority, notes` (no `assignedEmployeeId` — use `reassign` for that).
**Response**: 200 `ApiResponse<TaskItemDto>` (full updated DTO — note: not 204, unlike most other Update endpoints in this codebase).
**Validation** (`UpdateTaskCommandValidator`): `Title` required, max 200; `Description` max 2000; `Notes` max 1000; `ClientId`, if given, must be an active client.
**Error Codes**: 400 `ID_MISMATCH` or `VALIDATION_ERROR`, 401, 403, 404 `NOT_FOUND`, 409 `CONFLICT` ("Task ... is {Status} and is read-only").
**Examples**: `curl -X PUT -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"id":"ff55...","title":"Site visit — AC servicing (urgent)","priority":"Critical"}' https://api.example.com/api/v1/tasks/ff55.../`
**Best Practices**: This endpoint returns 200 with the body, not 204 — don't assume no-content parsing here like you would for most other "update" actions in this API.

### POST /api/v1/tasks/{id}/reassign — Reassign task

**Purpose**: Reassign a task to a different employee; resets status to `Assigned` regardless of where the previous assignee had progressed to.
**URL**: `/api/v1/tasks/{id}/reassign`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Policy `CanManageTasks` (Admin only).
**Request** (`ReassignTaskCommand`, `Id` from route): `{ "assignedEmployeeId": "guid (required, must exist)" }`.
**Response**: 200 `ApiResponse<TaskItemDto>`.
**Validation** (`ReassignTaskCommandValidator`): `AssignedEmployeeId` required and must exist.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403, 404 `NOT_FOUND`, 409 `CONFLICT` ("Task ... is {Status} and is read-only" — cannot reassign a Completed/Cancelled task).
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"assignedEmployeeId":"..."}' https://api.example.com/api/v1/tasks/ff55.../reassign`
**Best Practices**: The new assignee starts fresh at `Assigned` — any progress/acceptance the old assignee had is discarded from the status machine (comments/attachments history is preserved, just the status resets).

### POST /api/v1/tasks/{id}/cancel — Cancel task

**Purpose**: Cancel a task (admin-only, not self-scoped).
**URL**: `/api/v1/tasks/{id}/cancel`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Policy `CanManageTasks` (Admin only).
**Request**: Path `id`. No body.
**Response**: 204 No Content.
**Validation**: None.
**Error Codes**: 401, 403, 404, 409 `CONFLICT` ("Task ... is already completed and cannot be cancelled").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/tasks/ff55.../cancel`
**Best Practices**: Unlike Update/Reassign, Cancel is allowed from any non-`Completed` status (including already-`Cancelled`, which the handler does not appear to explicitly guard against — verify idempotency before relying on it).

### POST /api/v1/tasks/{id}/accept — Accept task

**Purpose**: Assignee accepts an `Assigned` task. `Assigned` → `Accepted`.
**URL**: `/api/v1/tasks/{id}/accept`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user; handler restricts to the task's assignee unless the caller is `Admin`.
**Request**: Path `id`. No body.
**Response**: 204 No Content.
**Validation**: None.
**Error Codes**: 401, 403 `FORBIDDEN` ("You can only accept tasks assigned to you."), 404, 409 `CONFLICT` ("Task ... must be in Assigned status to accept").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/tasks/ff55.../accept`

### POST /api/v1/tasks/{id}/reject — Reject task

**Purpose**: Assignee declines an `Assigned` task. `Assigned` → `Rejected`.
**URL**: `/api/v1/tasks/{id}/reject`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Any authenticated user; handler restricts to the assignee unless `Admin`.
**Request**: Path `id`. Body (optional, `RejectTaskBody`): `{ "reason": "string" }`, appended to `notes`.
**Response**: 204 No Content.
**Validation**: None (no FluentValidation validator registered for `RejectTaskCommand`).
**Error Codes**: 401, 403 `FORBIDDEN`, 404, 409 `CONFLICT` ("Task ... must be in Assigned status to reject").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"reason":"Outside my region"}' https://api.example.com/api/v1/tasks/ff55.../reject`
**Best Practices**: `Rejected` is a dead-end state reachable only from `Assigned` — an Admin must `reassign` (which resets to `Assigned`) to route it to someone else; there's no direct un-reject action.

### POST /api/v1/tasks/{id}/start — Start task

**Purpose**: Assignee starts work on an `Accepted` task. `Accepted` → `InProgress`.
**URL**: `/api/v1/tasks/{id}/start`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user; handler restricts to the assignee unless `Admin`.
**Request**: Path `id`. No body.
**Response**: 204 No Content.
**Validation**: None.
**Error Codes**: 401, 403 `FORBIDDEN`, 404, 409 `CONFLICT` ("Task ... must be Accepted before it can be started").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/tasks/ff55.../start`

### POST /api/v1/tasks/{id}/progress — Update task progress

**Purpose**: Toggle an in-flight task between `InProgress` and `OnHold`.
**URL**: `/api/v1/tasks/{id}/progress`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Any authenticated user; handler restricts to the assignee unless `Admin`.
**Request** (`UpdateTaskProgressCommand`, `Id` from route): `{ "status": "InProgress | OnHold" }`.
**Response**: 204 No Content.
**Validation** (`UpdateTaskProgressCommandValidator`): `Status` must be exactly `InProgress` or `OnHold` — no other enum value accepted here.
**Error Codes**: 400 `VALIDATION_ERROR` (e.g. `status: "Completed"` sent here), 401, 403 `FORBIDDEN`, 404, 409 `CONFLICT` ("Task ... must be InProgress or OnHold to update progress").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"status":"OnHold"}' https://api.example.com/api/v1/tasks/ff55.../progress`
**Best Practices**: Use `complete` (not this endpoint) to finish a task — sending `status: "Completed"` here is a 400.

### POST /api/v1/tasks/{id}/complete — Complete task

**Purpose**: Mark an `InProgress`/`OnHold` task `Completed`. The task becomes read-only afterward (blocks Update/Reassign/Comment/Attachment-upload).
**URL**: `/api/v1/tasks/{id}/complete`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user; handler restricts to the assignee unless `Admin`.
**Request**: Path `id`. No body.
**Response**: 204 No Content.
**Validation**: None.
**Error Codes**: 401, 403 `FORBIDDEN` ("You can only complete tasks assigned to you."), 404, 409 `CONFLICT` ("Task ... must be InProgress or OnHold to complete").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/tasks/ff55.../complete`
**Best Practices**: This is terminal for editing purposes — capture any final notes/attachments *before* calling complete.

### GET /api/v1/tasks/{id}/comments — List task comments

**Purpose**: View a task's progress/notes log.
**URL**: `/api/v1/tasks/{id}/comments`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user; handler restricts to the assignee unless `Admin`.
**Request**: Path `id`.
**Response**: 200 `ApiResponse<IEnumerable<TaskCommentDto>>`: `{ id, taskId, authorUserId, comment, createdAtUtc }`.
**Validation**: None.
**Error Codes**: 401, 403 `FORBIDDEN`, 404 `NOT_FOUND` (task).
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/tasks/ff55.../comments`

### POST /api/v1/tasks/{id}/comments — Add task comment

**Purpose**: Add a note to a task's progress log. Rejected once the task is `Completed` or `Cancelled`.
**URL**: `/api/v1/tasks/{id}/comments`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
**Authentication**: Any authenticated user; handler restricts to the assignee unless `Admin`.
**Request** (`AddTaskCommentCommand`, `TaskId` from route): `{ "comment": "string (required, max 2000)" }`.
**Response**: Actual runtime status is **200** — the action returns `Ok(...)` in code even though it's decorated `[ProducesResponseType(201)]` (a Swagger/doc mismatch in the source, not a documentation choice here). Body: `ApiResponse<TaskCommentDto>`: `{ id, taskId, authorUserId, comment, createdAtUtc }`.
**Validation** (`AddTaskCommentCommandValidator`): `Comment` required, max 2000.
**Error Codes**: 400 `VALIDATION_ERROR`, 401, 403 `FORBIDDEN`, 404 `NOT_FOUND`, 409 `CONFLICT` ("Task ... is {Status} and is read-only").
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"comment":"Arrived on site, starting servicing."}' https://api.example.com/api/v1/tasks/ff55.../comments`

### GET /api/v1/tasks/{id}/attachments — List task attachments

**Purpose**: View a task's uploaded photos/documents.
**URL**: `/api/v1/tasks/{id}/attachments`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user; handler restricts to the assignee unless `Admin`.
**Request**: Path `id`.
**Response**: 200 `ApiResponse<IEnumerable<TaskAttachmentDto>>`: `{ id, taskId, originalFileName, contentType, fileSizeBytes, uploadedAtUtc, uploadedBy }`.
**Validation**: None.
**Error Codes**: 401, 403 `FORBIDDEN`, 404 `NOT_FOUND`.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/tasks/ff55.../attachments`

### POST /api/v1/tasks/{id}/attachments — Upload task attachment

**Purpose**: Upload a photo/document evidencing task work ("Upload photos"). Rejected once the task is `Completed` or `Cancelled`.
**URL**: `/api/v1/tasks/{id}/attachments`
**Method**: POST
**Headers**: `Authorization: Bearer <token>`, `Content-Type: multipart/form-data`
**Authentication**: Any authenticated user; handler restricts to the assignee unless `Admin`. Additionally rate-limited at `[EnableRateLimiting("AttachmentUploadPolicy")]` (20 req/60s/IP, tighter than the class-level 100/60s).
**Request**: Path `id`. Multipart field `file` (required). Server builds `UploadTaskAttachmentCommand`.
**Response**: Actual runtime status is **200** (action returns `Ok(...)` despite `[ProducesResponseType(201)]`). Body: `ApiResponse<Guid>` — new attachment id.
**Validation** (`UploadTaskAttachmentCommandValidator`): identical rules to the candidate-attachment validator — `FileName` required/safe, `ContentType` ∈ `application/pdf|image/jpeg|image/png`, extension matches content type, ≤ 10 MB, magic-byte signature check.
**Error Codes**: 400 `FILE_REQUIRED` or `VALIDATION_ERROR`, 401, 403 `FORBIDDEN`, 404 `NOT_FOUND`, 409 `CONFLICT` (task read-only), 429 `RATE_LIMIT_EXCEEDED`.
**Examples**: `curl -X POST -H "Authorization: Bearer $TOKEN" -F "file=@site-photo.jpg;type=image/jpeg" https://api.example.com/api/v1/tasks/ff55.../attachments`
**Best Practices**: Budget for the tighter 20/60s upload limit specifically — batch/throttle client-side if uploading multiple site photos in quick succession.

### GET /api/v1/tasks/attachments/{attachmentId}/download — Download task attachment

**Purpose**: Download the raw bytes of a task attachment.
**URL**: `/api/v1/tasks/attachments/{attachmentId}/download`
**Method**: GET
**Headers**: `Authorization: Bearer <token>`
**Authentication**: Any authenticated user; handler restricts to the assignee of the attachment's parent task unless `Admin`.
**Request**: Path `attachmentId`.
**Response**: 200, raw file bytes; 404 if not found or not authorized (existence not disclosed).
**Validation**: None.
**Error Codes**: 401, 404.
**Examples**: `curl -H "Authorization: Bearer $TOKEN" -OJ https://api.example.com/api/v1/tasks/attachments/9c1e.../download`
# Messaging, Notifications & Announcements API Reference

Source: `backend/EMS.API/Controllers/MessagingController.cs`, `NotificationsController.cs`, `AnnouncementsController.cs` and their corresponding `EMS.Application/Features/*` commands, queries, validators, and handlers. Verified against the current code, not just `docs/api-specification.md` (the doc is consistent with the code as of this writing; where it adds useful framing that isn't visible in the code alone — e.g. "no SignalR" — that's noted).

**Transport note:** None of these three controllers use SignalR/WebSockets. `MessagingController` was checked specifically for hub usage (`grep -r "Hub|SignalR|MapHub"` across `backend/`) and there is none anywhere in the backend. All delivery is plain REST; clients must poll (e.g. `GET /api/v1/conversations/unread-count`, `GET /api/notifications/user/{userId}`, `GET /api/announcements`) for new data.

**Response envelope note:** `MessagingController` wraps every response in the shared `ApiResponse<T>` / `ApiErrorResponse` envelope (see `EMS.API/Controllers/AuthController.cs` for the envelope definitions). `NotificationsController` and `AnnouncementsController` do **not** — they return bare JSON (raw arrays, raw ids, raw DTOs) directly from `Ok(...)`/`CreatedAtAction(...)`, with no `data`/`message`/`correlationId` wrapper. This is called out per-endpoint below.

**Error handling note:** All three controllers rely on the global `ExceptionHandlingMiddleware` (`backend/EMS.API/Middleware/ExceptionHandlingMiddleware.cs`) to translate thrown exceptions into HTTP responses:

| Thrown exception | HTTP status | `code` |
|---|---|---|
| `FluentValidation.ValidationException` | 400 | `VALIDATION_ERROR` |
| `InvalidOperationException` whose message contains "not found" | 404 | `NOT_FOUND` |
| `InvalidOperationException` (any other message) | 409 | `CONFLICT` |
| `UnauthorizedAccessException` | 403 | `FORBIDDEN` |
| Any other exception | 500 | `INTERNAL_ERROR` |

Error body shape (when the middleware produces it): `{ "status": number, "code": string, "message": string, "errors": object|null, "correlationId": string }`. Note this is a differently-cased/shaped object than `ApiErrorResponse`'s C# properties (`Status`, `Code`, ...) because the middleware serializes with `JsonNamingPolicy.CamelCase`.

---

## Messaging API

Controller route prefix: `/api/v1` (`[Route("api/v1")]`). Controller-level `[Authorize]` — every action requires a valid JWT; specific actions add role/policy restrictions on top. Every response is wrapped in `ApiResponse<T>` on success.

Access model (from the controller's XML doc comment): messaging is **open** — any authenticated employee can message any other employee or their manager. Every action except Delete/Restore is scoped inside the handler to "caller is an active participant of the conversation," not by role. Delete/Restore are Admin/HR-only moderation actions gated by the `CanManageMessaging` policy (`policy.RequireRole("Admin", "HR")`, defined in `EMS.API/Program.cs`).

### List Conversations

**Purpose** — Fetch the caller's own conversation list (1:1 and group), most-recently-active first, for the messaging inbox view.

**URL** — `/api/v1/conversations`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — Any authenticated user (self-scoped — only returns the caller's own conversations; no role requirement).

**Request** — Query params (bound to `GetConversationsQuery`):
- `page` (int, optional, default `1`)
- `pageSize` (int, optional, default `20`; server clamps to 20 if outside `1..100`)
- `search` (string, optional) — matches conversation title or another participant's name

`RequestingUserId` is set server-side from the JWT and cannot be supplied by the client.

**Response** — `200 OK`
```json
{
  "data": {
    "data": [
      {
        "id": "b3f1...guid",
        "title": null,
        "isGroup": false,
        "participants": [
          { "userId": "...", "name": "Jane Doe", "joinedAtUtc": "2026-07-01T10:00:00Z", "leftAtUtc": null }
        ],
        "lastMessageAtUtc": "2026-07-31T09:15:00Z",
        "lastMessagePreview": "See you tomorrow",
        "unreadCount": 2,
        "isDeleted": false,
        "createdAtUtc": "2026-06-01T08:00:00Z",
        "updatedAtUtc": "2026-07-31T09:15:00Z"
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 5,
    "totalPages": 1
  },
  "message": "Request completed successfully.",
  "correlationId": "a1b2c3d4e5f6a1b2"
}
```

**Validation** — No FluentValidation validator exists for `GetConversationsQuery`. Page/pageSize are clamped defensively in the handler (`GetConversationsQueryHandler`), not rejected.

**Error Codes** — `401` (missing/invalid token, via JWT middleware, not the exception middleware).

**Examples**
```bash
curl -H "Authorization: Bearer $TOKEN" \
  "https://api.example.com/api/v1/conversations?page=1&pageSize=20&search=jane"
```

**Best Practices**
- Use `search` server-side rather than filtering a full list client-side.
- Pair with `GET /conversations/unread-count` for a lightweight nav badge instead of re-fetching and summing this list.

---

### Get Unread Conversation Count

**Purpose** — Get the number of conversations that have at least one unread message, for a navbar/badge indicator.

**URL** — `/api/v1/conversations/unread-count`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — Any authenticated user (self-scoped).

**Request** — No parameters.

**Response** — `200 OK`
```json
{ "data": 3, "message": "Request completed successfully.", "correlationId": "..." }
```

**Validation** — None (no request body/params).

**Error Codes** — `401`.

**Examples**
```bash
curl -H "Authorization: Bearer $TOKEN" \
  https://api.example.com/api/v1/conversations/unread-count
```

**Best Practices**
- This counts **conversations with any unread message**, not total unread messages — don't sum it against per-conversation `unreadCount` expecting the two to reconcile at the message level.
- Poll this on an interval (or on window focus) rather than on every keystroke/route change.

---

### Get Conversation By Id

**Purpose** — Fetch a single conversation with its active participant list, e.g. when opening a chat thread.

**URL** — `/api/v1/conversations/{id}`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — Any authenticated user, but the handler enforces the caller must be an active participant of the conversation, unless the caller is Admin/HR (`IsPrivileged` computed from `User.IsInRole("Admin") || User.IsInRole("HR")`).

**Request** — Path param: `id` (guid, required).

**Response** — `200 OK`, body shape identical to one item of the `ConversationDto` array above, wrapped in `ApiResponse<ConversationDto>`.

**Validation** — None (no body).

**Error Codes**
- `404` — conversation does not exist (controller checks for `null` from the query handler and calls `NotFound()` directly — this is a plain 404 with no body, not the `ApiErrorResponse` envelope).
- `403 FORBIDDEN` — caller is not an active participant and not Admin/HR (`UnauthorizedAccessException` → middleware → 403).
- `401` — missing/invalid token.

**Examples**
```bash
curl -H "Authorization: Bearer $TOKEN" \
  https://api.example.com/api/v1/conversations/b3f1c2d4-...
```

**Best Practices**
- Treat 403 and 404 identically in the UI (both mean "you can't see this conversation") to avoid leaking existence of conversations the caller isn't part of.

---

### Create Conversation

**Purpose** — Start a new conversation (1:1 or group) and send its first message in one call. If the request targets exactly one other user and no `title` is given, the handler reuses an existing untitled 1:1 conversation between the same two users instead of creating a duplicate thread.

**URL** — `/api/v1/conversations`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required), `Content-Type: application/json`

**Authentication** — Any authenticated user.

**Request** — Body (`CreateConversationCommand`):
```json
{
  "participantUserIds": ["11111111-1111-1111-1111-111111111111"],
  "title": null,
  "initialMessageBody": "Hi, can we sync on the sprint plan?"
}
```
- `participantUserIds` (`Guid[]`, required) — at least one other user id.
- `title` (`string?`, optional, max 250 chars) — omit/null for a 1:1 direct conversation; a group is implied when there's more than one other participant.
- `initialMessageBody` (`string`, required, max 4000 chars).

`RequestingUserId` is set server-side from the JWT.

**Response** — `201 Created`
```json
{ "data": { "id": "b3f1c2d4-...guid" }, "message": "Conversation created successfully.", "correlationId": "..." }
```
`Location` header points at `GET /api/v1/conversations/{id}`. Note: if an existing 1:1 conversation is reused, the returned `id` is the **existing** conversation's id, not a newly created one — still `201`.

**Validation** (`CreateConversationCommandValidator`)
- `initialMessageBody`: not empty, max 4000 chars.
- `title`: max 250 chars (no `NotEmpty`, so null/omitted is fine).
- `participantUserIds`: must be non-null and non-empty ("At least one other participant is required.").
- Each id in `participantUserIds`: validated asynchronously against `IUserRepository.GetByIdAsync` — must exist and be `IsActive`, else "Participant user does not exist or is inactive."

**Error Codes**
- `400 VALIDATION_ERROR` — e.g. empty `initialMessageBody`, empty `participantUserIds`, or a participant id that doesn't exist/is inactive.
- `401` — missing/invalid token.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/conversations \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"participantUserIds":["11111111-1111-1111-1111-111111111111"],"initialMessageBody":"Hi there"}'
```
```json
{ "data": { "id": "b3f1c2d4-5678-90ab-cdef-1234567890ab" }, "message": "Conversation created successfully.", "correlationId": "9f8e7d6c5b4a3210" }
```

**Best Practices**
- Don't pre-check for an existing 1:1 conversation on the client before calling this — the server already dedupes untitled 1:1 threads.
- Always pass a `title` when intentionally creating a group with a single other participant, otherwise it will be treated as (and potentially merged into) a direct conversation semantics-wise once a third person is added later via `AddParticipants`.

---

### Add Participants

**Purpose** — Add one or more users to an existing conversation. Adding anyone to a previously-1:1 conversation promotes it to a group (`isGroup = true`). Re-adding a user who previously left resets their read watermark to "now" so they don't see the full backlog as unread.

**URL** — `/api/v1/conversations/{id}/participants`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required), `Content-Type: application/json`

**Authentication** — Any authenticated user, but must be an active participant of the conversation (handler check — no privileged bypass here, unlike `GetConversationById`/`GetMessages`).

**Request** — Path param `id` (guid). Body (`AddParticipantsCommand`):
```json
{ "userIds": ["22222222-2222-2222-2222-222222222222"] }
```
- `userIds` (`Guid[]`, required) — the `conversationId` and `requestingUserId` fields on the command are overwritten by the controller from the route/JWT, so they need not (and should not) be supplied in the body.

**Response** — `204 No Content`.

**Validation** (`AddParticipantsCommandValidator`)
- `conversationId`: not empty (always true — set from route).
- `userIds`: must be non-null and non-empty ("At least one user id is required.").
- Each id in `userIds`: must exist and be `IsActive` via `IUserRepository`, else "User does not exist or is inactive."

**Error Codes**
- `400 VALIDATION_ERROR` — empty `userIds`, or a user id that doesn't exist/is inactive.
- `409 CONFLICT` — none directly thrown by this handler for a "not found" conversation (see next line) — conflict path isn't used here.
- `404`-shaped error via `InvalidOperationException("Conversation not found.")` → middleware maps to `404 NOT_FOUND` (contains "not found").
- `403 FORBIDDEN` — caller is not an active participant (`UnauthorizedAccessException`).
- `401` — missing/invalid token.

Note: users already an active participant are silently skipped (no error) — the endpoint is idempotent for already-present users.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/conversations/b3f1c2d4-.../participants \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"userIds":["22222222-2222-2222-2222-222222222222"]}'
```

**Best Practices**
- Batch multiple new participants into a single call rather than one call per user — the handler already loops and does one `SaveChanges`.
- Expect `isGroup` to flip to `true` after this call even if the conversation started as 1:1 and the resulting active-participant count is only 2 due to someone having left — check `participants` in the follow-up `GET` rather than assuming.

---

### Leave Conversation

**Purpose** — Let the caller leave a group conversation. Not available on direct (1:1) conversations.

**URL** — `/api/v1/conversations/{id}/leave`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — Any authenticated user; must currently be an active participant.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — No FluentValidation validator for `LeaveConversationCommand`; business rules enforced directly in the handler (see Error Codes).

**Error Codes**
- `404 NOT_FOUND` — conversation doesn't exist (`InvalidOperationException("Conversation not found.")`).
- `409 CONFLICT` — conversation is not a group (`InvalidOperationException("Cannot leave a direct conversation.")` — message doesn't contain "not found" so it falls into the generic `InvalidOperationException` → 409 branch).
- `403 FORBIDDEN` — caller is not an active participant, or already left (`UnauthorizedAccessException("You are not a participant of this conversation.")`).
- `401` — missing/invalid token.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/conversations/b3f1c2d4-.../leave \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Check `isGroup` on the conversation DTO before showing a "Leave" action in the UI — calling this on a 1:1 thread always 409s.

---

### Delete Conversation (Admin/HR moderation)

**Purpose** — Soft-delete a conversation. Explicitly a moderation action, not a "delete for me" / leave action — gated to Admin/HR.

**URL** — `/api/v1/conversations/{id}`

**Method** — DELETE

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — `[Authorize(Policy = "CanManageMessaging")]` → roles `Admin` or `HR` only.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — No validator; handler-enforced.

**Error Codes**
- `404 NOT_FOUND` — conversation doesn't exist (`InvalidOperationException($"Conversation {id} not found.")`).
- `403` — caller lacks `Admin`/`HR` role (enforced by the `[Authorize(Policy)]` attribute before the handler runs, via ASP.NET Core authorization — standard `403 Forbidden`, not the `ApiErrorResponse` envelope).
- `401` — missing/invalid token.

**Examples**
```bash
curl -X DELETE https://api.example.com/api/v1/conversations/b3f1c2d4-... \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Soft-delete only — participants can still be found via `GetConversationByIdIncludingDeletedAsync` for restore; don't assume message content is purged.

---

### Restore Conversation (Admin/HR moderation)

**Purpose** — Undo a soft-delete on a conversation.

**URL** — `/api/v1/conversations/{id}/restore`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — `[Authorize(Policy = "CanManageMessaging")]` → `Admin`/`HR` only.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — No validator; handler-enforced.

**Error Codes**
- `404 NOT_FOUND` — conversation doesn't exist at all, even soft-deleted (`InvalidOperationException($"Conversation {id} not found.")`).
- `409 CONFLICT` — conversation exists but is not currently deleted (`InvalidOperationException($"Conversation {id} is not deleted.")`).
- `403` — caller lacks `Admin`/`HR` role.
- `401` — missing/invalid token.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/conversations/b3f1c2d4-.../restore \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Call `GetConversationById` (as Admin/HR) first if unsure whether the conversation is currently deleted, to avoid the 409.

---

### List Messages

**Purpose** — Page through a conversation's messages, newest first, e.g. to render the chat thread.

**URL** — `/api/v1/conversations/{id}/messages`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — Any authenticated user; must be an active participant, or Admin/HR (`IsPrivileged`).

**Request** — Path param `id` (guid). Query params (`GetMessagesQuery`): `page` (int, default 1), `pageSize` (int, default 20, clamped to `1..100` server-side).

**Response** — `200 OK`
```json
{
  "data": {
    "data": [
      {
        "id": "...",
        "conversationId": "b3f1c2d4-...",
        "senderUserId": "...",
        "senderName": "Jane Doe",
        "body": "Hi there",
        "sentAtUtc": "2026-07-31T09:15:00Z"
      }
    ],
    "page": 1, "pageSize": 20, "totalCount": 12, "totalPages": 1
  },
  "message": "Request completed successfully.",
  "correlationId": "..."
}
```

**Validation** — No FluentValidation validator for `GetMessagesQuery`; page/pageSize defensively clamped in the handler.

**Error Codes**
- `404 NOT_FOUND` — conversation doesn't exist (`InvalidOperationException("Conversation not found.")`).
- `403 FORBIDDEN` — not an active participant and not privileged.
- `401` — missing/invalid token.

**Examples**
```bash
curl -H "Authorization: Bearer $TOKEN" \
  "https://api.example.com/api/v1/conversations/b3f1c2d4-.../messages?page=1&pageSize=20"
```

**Best Practices**
- The doc comment states "newest first" — confirm sort order matches your rendering assumption (don't re-sort ascending client-side without checking, since `GetMessagesAsync`'s exact ordering lives in the repository, not shown here).
- Combine with `POST /conversations/{id}/read` after the user views the page, to advance their read watermark.

---

### Send Message

**Purpose** — Post a new message into an existing conversation.

**URL** — `/api/v1/conversations/{id}/messages`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required), `Content-Type: application/json`

**Authentication** — Any authenticated user; must be an active participant (no Admin/HR bypass on send).

**Request** — Path param `id` (guid). Body (`SendMessageCommand`):
```json
{ "body": "Sounds good, see you then." }
```
- `body` (`string`, required, max 4000 chars). `conversationId`/`requestingUserId` are set by the controller.

**Response** — `201 Created`
```json
{ "data": { "id": "message-guid" }, "message": "Message sent.", "correlationId": "..." }
```
`Location` header points at `GET /api/v1/conversations/{id}/messages`.

**Validation** (`SendMessageCommandValidator`)
- `conversationId`: not empty.
- `body`: not empty, max 4000 chars.

**Error Codes**
- `400 VALIDATION_ERROR` — empty/too-long `body`.
- `404 NOT_FOUND` — conversation doesn't exist.
- `403 FORBIDDEN` — caller not an active participant (or has left).
- `401` — missing/invalid token.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/conversations/b3f1c2d4-.../messages \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"body":"Sounds good, see you then."}'
```
```json
{ "data": { "id": "9c8b7a6f-..." }, "message": "Message sent.", "correlationId": "1a2b3c4d5e6f7081" }
```

**Best Practices**
- Sending a message auto-advances the sender's own read watermark server-side — no need to call `mark read` immediately after sending your own message.
- Since there's no push channel, poll `GET /conversations/{id}/messages` (or the unread-count endpoint) after sending from another device/tab.

---

### Mark Conversation Read

**Purpose** — Advance the caller's read watermark on a conversation to "now," clearing its unread count.

**URL** — `/api/v1/conversations/{id}/read`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — Any authenticated user; must be an active participant.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — No FluentValidation validator; handler-enforced.

**Error Codes**
- `404 NOT_FOUND` — conversation doesn't exist.
- `403 FORBIDDEN` — caller has no participant record, or has left (`participant == null || participant.LeftAtUtc != null`).
- `401` — missing/invalid token.

**Examples**
```bash
curl -X POST https://api.example.com/api/v1/conversations/b3f1c2d4-.../read \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Call this when the conversation view is opened/focused, not on every message render, to avoid excessive write traffic.

---

## Notifications API

Controller route prefix: `/api/[controller]` → `/api/notifications` (no `v1` segment — differs from Messaging's `/api/v1/...` prefix; confirm with `docs/api-specification.md` §19.1, which also uses `/notifications` without a version segment). **Responses are bare JSON — no `ApiResponse<T>` envelope.**

### Create Notification

**Purpose** — Create a personal (per-user) notification, e.g. for a leave-decision or attendance alert. Optionally also sends an email if `channel` is `"Email"`.

**URL** — `/api/notifications`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required), `Content-Type: application/json`

**Authentication** — `[Authorize(Roles = "Admin,HR")]`.

**Request** — Body (`CreateNotificationCommand`):
```json
{
  "userId": "11111111-1111-1111-1111-111111111111",
  "title": "Leave Approved",
  "message": "Your leave request for Aug 5-7 has been approved.",
  "channel": "InApp",
  "expiresAtUtc": null
}
```
- `userId` (`Guid?`, optional — nullable in the command; not validated as required by any validator).
- `title` (`string`, required by domain convention but **not enforced by any validator** — see Validation below).
- `message` (`string`, same caveat).
- `channel` (`string`, optional, defaults to `"InApp"`; only `"InApp"` and `"Email"` are meaningfully handled — `"Email"` triggers `IEmailSender.SendEmailAsync` when `userId` is set).
- `expiresAtUtc` (`DateTime?`, optional).

**Response** — `201 Created`, bare id (not wrapped):
```json
"b3f1c2d4-5678-90ab-cdef-1234567890ab"
```
`Location` header points at `GET /api/notifications/user/{userId}`. Note: if `cmd.UserId` is null, the generated `Location` route value `userId=null` will not resolve to a valid `GetForUser` route (that action requires a `Guid` route param) — this is a latent inconsistency in the code, not a documented behavior.

**Validation** — **No FluentValidation validator exists for `CreateNotificationCommand`** (confirmed: no `Validators/` folder under `Features/Notifications/`, and no class implementing `AbstractValidator<CreateNotificationCommand>` anywhere in the backend). `[ApiController]` model binding will still reject a structurally malformed JSON body (400), but empty/missing `title`/`message` strings, an invalid `channel` value, or a past `expiresAtUtc` are **not rejected** — they pass straight to the handler and get persisted as-is.

**Error Codes**
- `400` — malformed JSON / model-binding failure only (no field-level validation).
- `401` — missing/invalid token.
- `403` — caller is not `Admin` or `HR`.
- If email sending fails (channel `Email`), the handler catches the exception, logs a warning, and still returns `201` — email delivery failure is silent to the API caller.

**Examples**
```bash
curl -X POST https://api.example.com/api/notifications \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"userId":"11111111-1111-1111-1111-111111111111","title":"Leave Approved","message":"Your leave request for Aug 5-7 has been approved.","channel":"InApp"}'
```
```json
"b3f1c2d4-5678-90ab-cdef-1234567890ab"
```

**Best Practices**
- Client-side, validate `title`/`message` are non-empty and `channel` is one of the two recognized values before submitting — the server will not catch these.
- Don't rely on `201`'s `Location` header for notifications created without a `userId`; fetch by other means if you need to confirm creation for a system/broadcast-style notification with `userId: null`.

---

### Get Notifications For User

**Purpose** — List a user's notifications, paginated, optionally filtered to unread only — the primary feed for a notification bell/dropdown.

**URL** — `/api/notifications/user/{userId}`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — `[Authorize]` (any authenticated user), plus a handler-level ownership check: the caller must either be `Admin`, or `userId` must equal the caller's own id (parsed from `ClaimTypes.NameIdentifier`). Non-`Admin` HR users cannot fetch another user's notifications (only `Admin` is exempted — HR is **not** listed here, unlike Announcements' create/delete gate).

**Request** — Path param `userId` (guid, required). Query params: `page` (int, default 1), `pageSize` (int, default 20), `onlyUnread` (bool, default `false`).

**Response** — `200 OK`, bare array (not wrapped):
```json
[
  {
    "id": "...",
    "userId": "11111111-1111-1111-1111-111111111111",
    "title": "Leave Approved",
    "message": "Your leave request for Aug 5-7 has been approved.",
    "channel": "InApp",
    "isRead": false,
    "createdAtUtc": "2026-07-31T09:00:00Z",
    "expiresAtUtc": null
  }
]
```

**Validation** — No validator for `GetNotificationsQuery`; `page`/`pageSize` are passed straight through to `INotificationRepository.GetForUserAsync` with no clamping in the handler (unlike Messaging's queries) — behavior for a non-positive or huge `pageSize` depends entirely on the repository implementation (not reviewed as part of this controller-focused pass).

**Error Codes**
- `403` — `current == null` (unresolvable identity) or caller is neither `Admin` nor the `userId` owner. Returned via `return Forbid()` directly in the controller — this is ASP.NET Core's standard 403 challenge response, **not** the `ApiErrorResponse` JSON envelope.
- `401` — missing/invalid token.
- `400` — `userId` path segment isn't a valid GUID (model binding failure).

**Examples**
```bash
curl -H "Authorization: Bearer $TOKEN" \
  "https://api.example.com/api/notifications/user/11111111-1111-1111-1111-111111111111?page=1&pageSize=20&onlyUnread=true"
```

**Best Practices**
- Poll on an interval or on app focus for the notification badge; there is no push/SignalR channel.
- Use `onlyUnread=true` for the dropdown badge count instead of fetching everything and filtering client-side.

---

### Mark Notification Read

**Purpose** — Mark a single notification as read.

**URL** — `/api/notifications/{id}/mark-read`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — `[Authorize]` (any authenticated user) — **no ownership check**: the handler does not verify the caller owns the notification (`MarkAsReadCommand` doesn't even carry a `RequestingUserId`/`ReadBy` value from the controller — the controller constructs `new MarkAsReadCommand { NotificationId = id }` only, leaving `ReadBy` at its default `null`). Any authenticated user who knows/guesses a notification id can mark it read.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — No validator for `MarkAsReadCommand`.

**Error Codes**
- `500 INTERNAL_ERROR` if the notification doesn't exist — **not** `404`. The handler throws a plain `System.Exception("Notification not found")` rather than `InvalidOperationException`, so it does not match the middleware's "not found" `InvalidOperationException` branch and falls through to the generic 500 case. This is a real inconsistency with the rest of the API (worth flagging to the backend team; documented here as-is per actual current behavior, not the intended behavior).
- `401` — missing/invalid token.

**Examples**
```bash
curl -X POST https://api.example.com/api/notifications/9c8b7a6f-.../mark-read \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Treat a `500` from this endpoint as "notification may not exist" until the backend is fixed to throw `InvalidOperationException`/return 404 — don't surface it to end users as a generic server error.
- Because there's no ownership check, do not expose raw notification ids to a lower-trust context (e.g. a public share link) — any authenticated user could mark another user's notification read.

---

## Announcements API

Controller route prefix: `/api/[controller]` → `/api/announcements` (no `v1` segment). Controller-level `[Authorize]`. **Responses are bare JSON — no `ApiResponse<T>` envelope.**

### Create Announcement

**Purpose** — Broadcast a new company-wide (or department-/role-scoped) announcement.

**URL** — `/api/announcements`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required), `Content-Type: application/json`

**Authentication** — `[Authorize(Roles = "Admin,HR")]` (matches the `CanManageAnnouncements` policy description in `docs/api-specification.md`, though the controller uses the role list directly rather than a named policy).

**Request** — Body (`CreateAnnouncementCommand`):
```json
{
  "title": "Office closed Monday",
  "message": "The office will be closed for the public holiday on Monday.",
  "priority": "Normal",
  "audienceType": "All",
  "departmentId": null,
  "targetRole": null,
  "expiresAtUtc": "2026-08-10T00:00:00Z"
}
```
- `title` (`string`, required, max 250 chars).
- `message` (`string`, required, max 2000 chars).
- `priority` (`string`, optional, default `"Normal"`) — must be one of `Normal`, `Important`, `Critical`.
- `audienceType` (`string`, optional, default `"All"`) — must be one of `All`, `Department`, `Role`.
- `departmentId` (`Guid?`, required **only when** `audienceType == "Department"`) — must reference an existing department in the caller's company (`IDepartmentRepository.GetByIdAsync(id, currentUser.CompanyId)`).
- `targetRole` (`string?`, required **only when** `audienceType == "Role"`).
- `expiresAtUtc` (`DateTime?`, optional) — if provided, must be in the future.
- `createdByUserId` is set server-side from the JWT (`CurrentUserId()`), not client-suppliable.

**Response** — `201 Created`, bare id:
```json
"c1d2e3f4-5678-90ab-cdef-1234567890ab"
```
`Location` header points at `GET /api/announcements/{id}`.

**Validation** (`CreateAnnouncementCommandValidator`)
- `title`: not empty, max 250 chars.
- `message`: not empty, max 2000 chars.
- `priority`: must be in `{Normal, Important, Critical}`.
- `audienceType`: must be in `{All, Department, Role}`.
- `departmentId`: not null when `audienceType == "Department"`; must resolve to an existing department (scoped by `currentUser.CompanyId`) via async check.
- `targetRole`: not empty when `audienceType == "Role"`.
- `expiresAtUtc`: must be `> DateTime.UtcNow` when provided.

**Error Codes**
- `400 VALIDATION_ERROR` — e.g. missing `title`, invalid `priority`/`audienceType`, missing `departmentId` for a `Department`-audience announcement, non-existent `departmentId`, missing `targetRole` for a `Role`-audience announcement, or a past `expiresAtUtc`.
- `401` — missing/invalid token.
- `403` — caller lacks `Admin`/`HR` role.

**Examples**
```bash
curl -X POST https://api.example.com/api/announcements \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"title":"Office closed Monday","message":"The office will be closed for the public holiday on Monday.","priority":"Normal","audienceType":"All"}'
```
```json
"c1d2e3f4-5678-90ab-cdef-1234567890ab"
```

**Best Practices**
- Always pair `audienceType: "Department"` with a valid `departmentId` and `audienceType: "Role"` with a `targetRole` — the validator rejects the mismatch but only at submit time, so validate the pairing client-side too for a better UX.
- Set `expiresAtUtc` for time-bound announcements (e.g. one-off holiday notices) so they stop appearing in `GET /announcements` automatically rather than requiring a manual `DELETE`.

---

### List Announcements

**Purpose** — List announcements visible to the caller (audience-filtered), most recent first — the main announcements feed.

**URL** — `/api/announcements`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — Any authenticated user. Visibility is filtered server-side using the caller's id and role claim (`ClaimTypes.Role`, defaulting to `"Employee"` if absent) — there is no client-suppliable `userId`/`departmentId` filter.

**Request** — Query params: `page` (int, default 1), `pageSize` (int, default 20), `onlyUnread` (bool, default `false`).

**Response** — `200 OK`, bare array:
```json
[
  {
    "id": "c1d2e3f4-...",
    "title": "Office closed Monday",
    "message": "The office will be closed for the public holiday on Monday.",
    "priority": "Normal",
    "audienceType": "All",
    "departmentId": null,
    "targetRole": null,
    "createdByUserId": "...",
    "createdAtUtc": "2026-07-30T12:00:00Z",
    "expiresAtUtc": "2026-08-10T00:00:00Z",
    "isReadByMe": false
  }
]
```

**Visibility rule** (from `AnnouncementRepository.GetVisibleForUserAsync`): an announcement is included only if it is not soft-deleted, not expired (`expiresAtUtc == null || expiresAtUtc > now`), and one of: `audienceType == "All"`; `audienceType == "Department"` and the caller's employee record's `departmentId` matches; or `audienceType == "Role"` and `targetRole` matches the caller's role claim. `onlyUnread=true` filters out announcements the caller has already read.

**Validation** — No validator for `GetAnnouncementsQuery`; no page/pageSize clamping observed in the handler (paging is applied via `.Skip()/.Take()` in the repository after loading and filtering in memory — worth noting for large datasets, though outside this doc's controller-level scope).

**Error Codes** — `401` (missing/invalid token) only; no role restriction on read.

**Examples**
```bash
curl -H "Authorization: Bearer $TOKEN" \
  "https://api.example.com/api/announcements?page=1&pageSize=20&onlyUnread=true"
```

**Best Practices**
- Poll on an interval or on app load (per `docs/api-specification.md` §19: "Delivery is poll-based, not real-time... There is no SignalR or push infrastructure in this system today").
- Use `onlyUnread=true` server-side filtering rather than computing "unread" client-side from `isReadByMe`, to keep pagination counts consistent.

---

### Get Announcement By Id

**Purpose** — Fetch a single announcement (e.g. for a detail/notification-click view), respecting the same audience-visibility rule as the list endpoint.

**URL** — `/api/announcements/{id}`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — Any authenticated user; visibility filtered by the caller's id/role as above.

**Request** — Path param `id` (guid).

**Response** — `200 OK`, bare `AnnouncementDto` (same shape as one list item above).

**Validation** — None (no body).

**Error Codes**
- `404` — announcement does not exist, is soft-deleted, is expired, or is not visible to the caller's department/role (all four cases collapse to the same plain `NotFound()` — no distinguishing error body, since the controller does `if (item == null) return NotFound();`).
- `401` — missing/invalid token.

**Examples**
```bash
curl -H "Authorization: Bearer $TOKEN" \
  https://api.example.com/api/announcements/c1d2e3f4-...
```

**Best Practices**
- Don't assume a `404` here means "deleted" — it's also returned for announcements the caller simply isn't in the audience for, or that have expired. Don't leak audience-restriction details to the end user beyond a generic "not found."

---

### Mark Announcement Read

**Purpose** — Record that the caller has read an announcement (drives `isReadByMe` / `onlyUnread` filtering).

**URL** — `/api/announcements/{id}/mark-read`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — Any authenticated user (`[Authorize]` at controller level; no extra role check on this action). No visibility/audience check is performed here — the handler only checks the announcement exists and is not soft-deleted (via `_repo.GetByIdAsync`), not whether the caller is actually in its audience.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — No validator for `MarkAnnouncementReadCommand`. Handler is idempotent — calling it twice does not error (`MarkReadAsync` checks `alreadyRead` and returns early).

**Error Codes**
- `404 NOT_FOUND` — announcement doesn't exist or is soft-deleted (`InvalidOperationException($"Announcement {id} not found.")`).
- `401` — missing/invalid token.

**Examples**
```bash
curl -X POST https://api.example.com/api/announcements/c1d2e3f4-.../mark-read \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Safe to call more than once (idempotent) — no need to guard client-side against double-firing.
- Call this when the announcement is actually viewed/expanded, not merely listed, to keep unread counts meaningful.

---

### Delete (Retract) Announcement

**Purpose** — Soft-delete ("retract") an announcement so it stops appearing in feeds.

**URL** — `/api/announcements/{id}`

**Method** — DELETE

**Headers** — `Authorization: Bearer <access_token>` (required)

**Authentication** — `[Authorize(Roles = "Admin,HR")]`.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — No validator; handler-enforced existence check only.

**Error Codes**
- `404 NOT_FOUND` — announcement doesn't exist (`InvalidOperationException($"Announcement {id} not found.")`); also returned if it was already retracted, since `GetByIdAsync` filters out soft-deleted rows.
- `401` — missing/invalid token.
- `403` — caller lacks `Admin`/`HR` role.

**Examples**
```bash
curl -X DELETE https://api.example.com/api/announcements/c1d2e3f4-... \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

**Best Practices**
- There is no corresponding "restore" endpoint for Announcements (unlike Messaging's `POST /conversations/{id}/restore`) — retraction is effectively final from the API surface, even though the underlying delete is a soft-delete in the database.
# Platform / Multi-Tenant / Client Master API Reference

Covers: `ClientController`, `CompanyRegistrationController`, `PlatformAuditLogsController`, `PlatformCompaniesController`, `PlatformDashboardController`, `PlatformSettingsController`.

## Cross-cutting notes

- **Tenant identification.** There is no `X-Tenant-Id` header. Tenant scoping is embedded in the JWT: `JwtTokenService` stamps a `company_id` claim on every user token except Super Admin's (Super Admin has no company). `ICurrentUserService`/`CurrentUserService` reads this claim server-side; `TenantStatusMiddleware` (registered between `UseAuthentication()` and `UseAuthorization()` in `Program.cs`) rejects every request from a non-Active/Trial company with `403 COMPANY_SUSPENDED` before it reaches any controller, and is a no-op for requests with no `company_id` claim (Super Admin, or public/anonymous endpoints).
- **Error envelope.** All errors are `ApiErrorResponse { status, code, message, errors?, correlationId }` (`backend/EMS.API/Controllers/AuthController.cs`), written by `ExceptionHandlingMiddleware` (`backend/EMS.API/Middleware/ExceptionHandlingMiddleware.cs`):
  - `FluentValidation.ValidationException` → `400 VALIDATION_ERROR`, `errors: [{ propertyName, errorMessage }]` — thrown by the MediatR `ValidationBehavior` pipeline before the handler runs.
  - `InvalidOperationException` whose message contains "not found" → `404 NOT_FOUND`.
  - Any other `InvalidOperationException` (duplicate name, invalid state transition, etc.) → `409 CONFLICT`.
  - `UnauthorizedAccessException` → `403 FORBIDDEN`.
  - Anything else → `500 INTERNAL_ERROR`.
- **Success envelope.** All success responses are `ApiResponse<T> { data, message, correlationId }`.
- **All routes below are prefixed `/api/v1`** per each controller's `[Route]` attribute.

---

## Clients API

Controller: `backend/EMS.API/Controllers/ClientController.cs`. Route: `api/v1/clients`. Class-level `[Authorize]` plus class-level `[EnableRateLimiting("WriteActionPolicy")]` (100 req/60s per client IP by default, configurable via `RateLimiting:WriteAction`, shared with Task Management / Reimbursement modules per the controller's doc comment). `GET` endpoints require only authentication; every mutating endpoint additionally requires the `CanManageClients` policy, which is `RequireRole("Admin")` only (`Program.cs` line 326) — deliberately not delegated to HR.

No FluentValidation validators exist for `GetClientsQuery`, `GetClientByIdQuery`, `ActivateClientCommand`, `DeactivateClientCommand`, `ArchiveClientCommand`, `DeleteClientCommand`, or `RestoreClientCommand` — only `CreateClientCommand` and `UpdateClientCommand` have validators (`backend/EMS.Application/Features/Clients/Validators/CreateClientCommandValidator.cs`).

### List Clients

**Purpose** — Retrieve a paginated, searchable, filterable list of clients. Any authenticated user can call this (read access is intentionally open pending Task Management scoping).

**URL** — `/api/v1/clients`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (required).

**Authentication** — Any authenticated user (`[Authorize]`, no specific policy/role).

**Request** — Query parameters, bound to `GetClientsQuery`:
| Param | Type | Required | Default |
|---|---|---|---|
| `page` | int | No | 1 |
| `pageSize` | int | No | 20 (clamped server-side to 1–100; out-of-range falls back to 20) |
| `search` | string | No | — |
| `isActive` | bool | No | — (omit to return both active and inactive) |

**Response** — `200 OK`
```json
{
  "data": {
    "data": [ { "id": "...", "clientName": "Acme Retail", "...": "..." } ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1
  },
  "message": "Request completed successfully.",
  "correlationId": "..."
}
```

**Validation** — None (no validator registered for `GetClientsQuery`). `page`/`pageSize` are silently normalized in `GetClientsQueryHandler`, not rejected.

**Error Codes** — `401` (missing/invalid token).

**Examples**
```bash
curl -X GET "https://api.example.com/api/v1/clients?page=1&pageSize=20&search=acme&isActive=true" \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Always pass `pageSize` explicitly if you need a specific page size; values outside 1–100 silently fall back to 20 rather than erroring.
- Use `search` for a combined match against `clientName`, `companyName`, `contactPerson`, or `email` (per `docs/api-specification.md` §20; verify exact matched columns against `IClientRepository.GetAllAsync` if precision matters).

---

### Get Client By Id

**Purpose** — Fetch a single client's full detail, e.g. to populate an edit form.

**URL** — `/api/v1/clients/{id}`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (required).

**Authentication** — Any authenticated user.

**Request** — Path param `id` (guid, required).

**Response** — `200 OK`
```json
{
  "data": {
    "id": "00000000-0000-0000-0000-000000001201",
    "clientName": "Acme Retail",
    "companyName": "Acme Corp",
    "contactPerson": "Jane Doe",
    "mobileNumber": "+1-555-0100",
    "alternateMobile": null,
    "email": "jane@acme.example",
    "gstNumber": null,
    "addressLine1": "1 Market Street",
    "addressLine2": null,
    "city": "San Francisco",
    "state": "CA",
    "country": "USA",
    "postalCode": "94105",
    "latitude": 37.7936,
    "longitude": -122.3965,
    "notes": null,
    "isActive": true,
    "isArchived": false,
    "isDeleted": false,
    "createdAtUtc": "2026-01-01T00:00:00Z",
    "updatedAtUtc": null
  },
  "message": "Request completed successfully.",
  "correlationId": "..."
}
```

**Validation** — None (`Guid` route-constraint `{id:guid}` rejects malformed GUIDs at routing level with a framework 404, before reaching the action).

**Error Codes** — `401`; `404` if no client with that id exists (controller returns bare `NotFound()`, not the standard `ApiErrorResponse` envelope — note this inconsistency).

**Examples**
```bash
curl -X GET "https://api.example.com/api/v1/clients/00000000-0000-0000-0000-000000001201" \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Treat a plain `404` (no JSON body guaranteed) differently from the `ApiErrorResponse`-shaped 404s the mutating endpoints below produce — this endpoint's not-found path bypasses the audit trail's `InvalidOperationException` convention.

---

### Create Client

**Purpose** — Register a new client in the Client Master. Called by Admin-facing client-onboarding UI.

**URL** — `/api/v1/clients`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required); `Content-Type: application/json`.

**Authentication** — `CanManageClients` policy (`Admin` role only).

**Request** — Body (`CreateClientCommand`):
```json
{
  "clientName": "Acme Retail",
  "companyName": "Acme Corp",
  "contactPerson": "Jane Doe",
  "mobileNumber": "+1-555-0100",
  "alternateMobile": null,
  "email": "jane@acme.example",
  "gstNumber": null,
  "addressLine1": "1 Market Street",
  "addressLine2": null,
  "city": "San Francisco",
  "state": "CA",
  "country": "USA",
  "postalCode": "94105",
  "latitude": 37.7936,
  "longitude": -122.3965,
  "notes": null
}
```
Required: `clientName`, `companyName`, `contactPerson`, `mobileNumber`, `email`, `addressLine1`, `city`, `country`, `postalCode`. Optional: `alternateMobile`, `gstNumber`, `addressLine2`, `state`, `latitude`, `longitude`, `notes`.

**Response** — `201 Created`, `Location` header from `CreatedAtAction(GetById)`. Body is `ApiResponse<ClientDto>` (same shape as Get Client By Id), message `"Client created successfully."`.

**Validation** (`CreateClientCommandValidator`):
| Field | Rules |
|---|---|
| `clientName` | NotEmpty, MaxLength(150), must not already exist among clients (async check via `IClientRepository.NameExistsAsync`) |
| `companyName` | NotEmpty, MaxLength(150) |
| `contactPerson` | NotEmpty, MaxLength(150) |
| `mobileNumber` | NotEmpty, MaxLength(20) |
| `alternateMobile` | MaxLength(20) |
| `email` | NotEmpty, valid email format, MaxLength(255) |
| `gstNumber` | MaxLength(20) |
| `addressLine1` | NotEmpty, MaxLength(250) |
| `addressLine2` | MaxLength(250) |
| `city` | NotEmpty, MaxLength(100) |
| `state` | MaxLength(100) |
| `country` | NotEmpty, MaxLength(100) |
| `postalCode` | NotEmpty, MaxLength(20) |
| `latitude` | InclusiveBetween(-90, 90), only when supplied |
| `longitude` | InclusiveBetween(-180, 180), only when supplied |
| `notes` | MaxLength(1000) |

**Error Codes**
- `400 VALIDATION_ERROR` — any rule above fails, including duplicate `clientName` (caught by the validator's async uniqueness check before the handler runs).
- `401` — not authenticated.
- `403 FORBIDDEN` — authenticated but not Admin.
- `409 CONFLICT` — theoretically possible if `CreateClientCommandHandler`'s own `NameExistsAsync` re-check (a race-condition guard) trips after the validator passed; in practice the validator catches this first.
- `429 RATE_LIMIT_EXCEEDED` — more than 100 write requests/60s from the same IP across Clients/Tasks/Reimbursements combined.

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/clients" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"clientName":"Acme Retail","companyName":"Acme Corp","contactPerson":"Jane Doe","mobileNumber":"+1-555-0100","email":"jane@acme.example","addressLine1":"1 Market Street","city":"San Francisco","country":"USA","postalCode":"94105"}'
```
Response:
```json
{ "data": { "id": "...", "clientName": "Acme Retail", "...": "..." }, "message": "Client created successfully.", "correlationId": "..." }
```

**Best Practices**
- Check `clientName` uniqueness client-side before submit to avoid a round trip, but always handle `400 VALIDATION_ERROR` server-side as the source of truth (uniqueness is enforced at the application layer, not a DB constraint).
- `latitude`/`longitude` should be supplied together or not at all for meaningful map data; the API does not enforce that pairing.

---

### Update Client

**Purpose** — Edit an existing client's details.

**URL** — `/api/v1/clients/{id}`

**Method** — PUT

**Headers** — `Authorization: Bearer <access_token>` (required); `Content-Type: application/json`.

**Authentication** — `CanManageClients` policy (`Admin` only).

**Request** — Path param `id` (guid). Body (`UpdateClientCommand`) — same fields as Create plus `id` (must match route `id`, or the controller returns `400 ID_MISMATCH` before calling the handler).

**Response** — `200 OK`, `ApiResponse<ClientDto>`.

**Validation** (`UpdateClientCommandValidator`) — identical field rules to Create; `clientName` uniqueness check excludes the client's own `id`.

**Error Codes**
- `400 ID_MISMATCH` — route `id` != body `id` (checked in the controller, own custom code, not from `ExceptionHandlingMiddleware`).
- `400 VALIDATION_ERROR` — field rule violation or duplicate name.
- `401`, `403`.
- `404 NOT_FOUND` — no client with that id (`UpdateClientCommandHandler` throws `InvalidOperationException("Client {id} not found.")`).
- `429 RATE_LIMIT_EXCEEDED`.

**Examples**
```bash
curl -X PUT "https://api.example.com/api/v1/clients/00000000-0000-0000-0000-000000001201" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"id":"00000000-0000-0000-0000-000000001201","clientName":"Acme Retail Inc","companyName":"Acme Corp","contactPerson":"Jane Doe","mobileNumber":"+1-555-0100","email":"jane@acme.example","addressLine1":"1 Market Street","city":"San Francisco","country":"USA","postalCode":"94105"}'
```

**Best Practices**
- Always send the full object — this is a full replace (PUT), not a partial patch; omitted optional fields are set to `null`/default.
- Set `id` in the body identically to the path segment to avoid the `ID_MISMATCH` short-circuit.

---

### Delete Client

**Purpose** — Soft-delete a client (e.g. onboarded in error, or client relationship ended entirely).

**URL** — `/api/v1/clients/{id}`

**Method** — DELETE

**Headers** — `Authorization: Bearer <access_token>` (required).

**Authentication** — `CanManageClients` policy.

**Request** — Path param `id` (guid).

**Response** — `204 No Content`.

**Validation** — None.

**Error Codes** — `401`, `403`, `404 NOT_FOUND` (`DeleteClientCommandHandler` throws if not found), `429`.

**Examples**
```bash
curl -X DELETE "https://api.example.com/api/v1/clients/00000000-0000-0000-0000-000000001201" \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Prefer `deactivate` over `delete` if the client relationship might resume — `delete` sets `IsDeleted`, only reversible via `restore`, whereas `deactivate` is the lighter, purely-status toggle.

---

### Activate Client

**Purpose** — Mark a client eligible to receive new tasks (e.g. after re-engaging a previously deactivated client).

**URL** — `/api/v1/clients/{id}/activate`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required).

**Authentication** — `CanManageClients` policy.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — None. No state-machine guard — can be called on an already-active client as a no-op.

**Error Codes** — `401`, `403`, `404 NOT_FOUND`, `429`.

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/clients/00000000-0000-0000-0000-000000001201/activate" \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Idempotent — safe to call repeatedly.

---

### Deactivate Client

**Purpose** — Block a client from receiving new tasks while retaining its history (e.g. temporary pause in engagement).

**URL** — `/api/v1/clients/{id}/deactivate`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required).

**Authentication** — `CanManageClients` policy.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — None.

**Error Codes** — `401`, `403`, `404 NOT_FOUND`, `429`.

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/clients/00000000-0000-0000-0000-000000001201/deactivate" \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Use instead of `delete` when you expect to `activate` again later — clean, single-field toggle.

---

### Archive Client

**Purpose** — Retire a client from active workflows entirely (distinct from soft delete) while keeping its history queryable. Also flips `isActive` to false as a side effect.

**URL** — `/api/v1/clients/{id}/archive`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required).

**Authentication** — `CanManageClients` policy.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — None.

**Error Codes** — `401`, `403`, `404 NOT_FOUND`, `429`.

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/clients/00000000-0000-0000-0000-000000001201/archive" \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- Archiving also deactivates (`IsActive` set to `false`) in the same operation — no need to call `deactivate` separately first.
- Use `restore` (not `activate`) to reverse an archive — `activate` only flips `IsActive` and leaves `IsArchived` untouched.

---

### Restore Client

**Purpose** — Reverse whichever terminal state applies: un-delete a soft-deleted client, or un-archive an archived one. Single endpoint for both because they're mutually exclusive terminal states.

**URL** — `/api/v1/clients/{id}/restore`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (required).

**Authentication** — `CanManageClients` policy.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — None. No guard requiring the client to actually be deleted/archived — calling `restore` on an already-normal client is a harmless no-op (clears already-null `DeletedAtUtc`/`DeletedBy`, sets `IsArchived = false`).

**Error Codes** — `401`, `403`, `404 NOT_FOUND` (uses `GetByIdIncludingDeletedAsync`, so this only fires if the id never existed at all), `429`.

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/clients/00000000-0000-0000-0000-000000001201/restore" \
  -H "Authorization: Bearer $TOKEN"
```

**Best Practices**
- This is the only client mutation endpoint that looks up soft-deleted rows (`GetByIdIncludingDeletedAsync`) — the rest (`Get`, `Update`, `Activate`, `Deactivate`, `Archive`, `Delete`) operate on the non-deleted set only and will 404 on a deleted client.

---

## Company Registration API

Controller: `backend/EMS.API/Controllers/CompanyRegistrationController.cs`. Route: `api/v1/company-registration`. Class-level `[AllowAnonymous]` — the public onboarding entry point for a new tenant. Gated by `PlatformSettings.IsPublicRegistrationEnabled`.

### Get Registration Status

**Purpose** — Tells the frontend whether to show the public "Register your company" form.

**URL** — `/api/v1/company-registration/status`

**Method** — GET

**Headers** — None required.

**Authentication** — Public (`[AllowAnonymous]`).

**Request** — No parameters.

**Response** — `200 OK`
```json
{ "data": true, "message": "Request completed successfully.", "correlationId": "..." }
```
`data` is the raw boolean value of `PlatformSettings.IsPublicRegistrationEnabled`.

**Validation** — None (no request body/params).

**Error Codes** — None expected beyond `500` on unexpected failure.

**Examples**
```bash
curl -X GET "https://api.example.com/api/v1/company-registration/status"
```

**Best Practices**
- Poll this once on app load to decide whether to render the registration route/link at all — don't hardcode the assumption that registration is open.

---

### Register Company

**Purpose** — Public self-service onboarding: creates a new tenant Company and its first Admin User atomically. This is the sole entry point for a new tenant onto the platform (the old `/auth/register` was removed — see `docs/api-specification.md` §3.10).

**URL** — `/api/v1/company-registration`

**Method** — POST

**Headers** — `Content-Type: application/json`. No `Authorization` (public).

**Authentication** — Public, but rate-limited via `[EnableRateLimiting("RegisterPolicy")]` (5 requests/60s per client IP by default, `RateLimiting:Register:*` config).

**Request** — Body (`RegisterCompanyCommand`):
```json
{
  "companyName": "Acme Corp",
  "timezone": "UTC",
  "currency": "USD",
  "adminUserName": "admin",
  "adminEmail": "admin@acme.example.com",
  "adminPassword": "Password@123"
}
```
Required: `companyName`, `adminUserName`, `adminEmail`, `adminPassword`. `timezone`/`currency` have C# defaults (`"UTC"`/`"USD"`) if omitted, but the validator still requires them non-empty — the client should send them explicitly.

**Response** — `201 Created`
```json
{
  "data": {
    "companyId": "00000000-0000-0000-0000-00000000c001",
    "companyStatus": "PendingApproval",
    "requiresApproval": true,
    "accessToken": null,
    "refreshToken": null,
    "expiresInSeconds": null
  },
  "message": "Company registered. Awaiting Super Admin approval before you can log in.",
  "correlationId": "..."
}
```
If `PlatformSettings.RequireApprovalForNewCompanies` is `false`, the company lands directly in `Trial`, and `accessToken`/`refreshToken`/`expiresInSeconds` (900 = 15 min) are populated immediately (`message`: `"Company registered successfully."`), same shape as a normal login.

**Validation** (`RegisterCompanyCommandValidator`):
| Field | Rules |
|---|---|
| `companyName` | NotEmpty, MaxLength(200) |
| `timezone` | NotEmpty, MaxLength(100) |
| `currency` | NotEmpty, MaxLength(10) |
| `adminUserName` | NotEmpty, MaxLength(256) |
| `adminEmail` | NotEmpty, valid email format, MaxLength(256) |
| `adminPassword` | NotEmpty, MinimumLength(8) |

Handler-level checks (surface as `409`, not `400`, since they throw `InvalidOperationException` rather than a validation failure):
- `PlatformSettings.IsPublicRegistrationEnabled` must be `true`, else `"Public company registration is currently disabled."`
- `adminUserName` must not already exist (`"Username already exists."`)
- `adminEmail` must not already exist (`"Email already exists."`)
- The `Admin` role must exist in the system (`"Admin role is not configured."` — an environment misconfiguration, not a client error).

**Error Codes**
- `400 VALIDATION_ERROR` — field-level rule violations.
- `409 CONFLICT` — registration disabled, duplicate username, duplicate email, or missing `Admin` role.
- `429 RATE_LIMIT_EXCEEDED` — more than 5 requests/60s from the same IP.

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/company-registration" \
  -H "Content-Type: application/json" \
  -d '{"companyName":"Acme Corp","timezone":"UTC","currency":"USD","adminUserName":"admin","adminEmail":"admin@acme.example.com","adminPassword":"Password@123"}'
```
Response (`201`, approval required):
```json
{ "data": { "companyId": "...", "companyStatus": "PendingApproval", "requiresApproval": true, "accessToken": null, "refreshToken": null, "expiresInSeconds": null }, "message": "Company registered. Awaiting Super Admin approval before you can log in.", "correlationId": "..." }
```

**Best Practices**
- Always branch UI behavior on `requiresApproval` rather than on `companyStatus` string matching — the latter is an implementation-visible enum name, the former is the intended boolean contract.
- When `requiresApproval` is `true`, do not attempt an immediate login — the account cannot authenticate until a Super Admin calls `POST /platform/companies/{id}/approve`.
- Treat `429` specially in the UI (this endpoint shares a strict 5/min budget) — show a "try again in a minute" message rather than a generic error, and respect the `Retry-After` response header.

---

## Platform Audit Logs API

Controller: `backend/EMS.API/Controllers/PlatformAuditLogsController.cs`. Route: `api/v1/platform/audit-logs`. Class-level `[Authorize(Policy = "IsSuperAdmin")]` (`RequireRole("SuperAdmin")`, `Program.cs` line 337) — entirely separate from every tenant-scoped policy; no tenant role (`Admin`, `HR`, `Manager`) can reach this surface.

### List Platform Audit Logs

**Purpose** — Super Admin's cross-company audit trail; can optionally be filtered to a single company to drill into one tenant's activity.

**URL** — `/api/v1/platform/audit-logs`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin token — no `company_id` claim).

**Authentication** — `IsSuperAdmin` policy (`SuperAdmin` role only).

**Request** — Query parameters, bound to `GetPlatformAuditLogsQuery`:
| Param | Type | Required | Notes |
|---|---|---|---|
| `companyId` | guid | No | Omit to see every company |
| `userId` | guid | No | |
| `entityName` | string | No | e.g. `"Company"`, `"Client"`, `"PlatformSettings"` |
| `entityId` | guid | No | |
| `action` | string | No | e.g. `"Created"`, `"Updated"`, `"Suspended"`, `"Approved"`, `"Rejected"`, `"ForceLogout"`, `"Restored"`, `"Deleted"` |
| `dateFrom` | datetime (ISO 8601) | No | |
| `dateTo` | datetime (ISO 8601) | No | |
| `page` | int | No | Default 1 |
| `pageSize` | int | No | Default 20, clamped 1–100 server-side |

**Response** — `200 OK`
```json
{
  "data": {
    "data": [
      {
        "id": "...",
        "companyId": "00000000-0000-0000-0000-00000000c001",
        "userId": null,
        "entityName": "Company",
        "entityId": "00000000-0000-0000-0000-00000000c001",
        "action": "Suspended",
        "oldValuesJson": null,
        "newValuesJson": "{\"Reason\":\"Non-payment\"}",
        "ipAddress": "203.0.113.4",
        "userAgent": "Mozilla/5.0 ...",
        "createdAtUtc": "2026-01-01T00:00:00Z"
      }
    ],
    "page": 1, "pageSize": 20, "totalCount": 1, "totalPages": 1
  },
  "message": "Request completed successfully.",
  "correlationId": "..."
}
```

**Validation** — **No FluentValidation validator exists for `GetPlatformAuditLogsQuery`** (confirmed: only `GetAuditLogsQueryValidator`, for the tenant-scoped `GetAuditLogsQuery`, exists under `Features/AuditLogs/Validators/`). `page`/`pageSize` are silently normalized in the handler rather than rejected; `dateFrom`/`dateTo` ordering is **not** validated here (unlike the tenant-scoped equivalent, which rejects `dateFrom > dateTo`).

**Error Codes** — `401`, `403` (non-SuperAdmin role).

**Examples**
```bash
curl -X GET "https://api.example.com/api/v1/platform/audit-logs?companyId=00000000-0000-0000-0000-00000000c001&page=1&pageSize=20" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN"
```

**Best Practices**
- Because `dateFrom`/`dateTo` are unvalidated here, client code should itself enforce `dateFrom <= dateTo` before sending — the server will not reject an inverted range, it will simply return an empty/unexpected result set.
- Use `entityName`/`entityId` together (not `entityId` alone) to disambiguate — `entityId` is a bare GUID with no type information on its own.

---

## Platform Companies API

Controller: `backend/EMS.API/Controllers/PlatformCompaniesController.cs`. Route: `api/v1/platform/companies`. Class-level `[Authorize(Policy = "IsSuperAdmin")]`. Entirely separate surface from tenant HR data — a Super Admin manages companies here, never employees/leaves/etc. inside a tenant.

### List Companies

**Purpose** — Browse/search all tenants on the platform, e.g. for a Super Admin ops dashboard.

**URL** — `/api/v1/platform/companies`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin).

**Authentication** — `IsSuperAdmin` policy.

**Request** — Query parameters, bound to `GetCompaniesQuery`:
| Param | Type | Required | Notes |
|---|---|---|---|
| `page` | int | No | Default 1 |
| `pageSize` | int | No | Default 20, clamped 1–100 |
| `status` | `CompanyStatus` enum string | No | One of `Trial`, `Active`, `Suspended`, `Inactive`, `PendingApproval`, `Rejected` |
| `search` | string | No | Matches company name |

**Response** — `200 OK`, `ApiResponse<PagedResult<CompanyDto>>`:
```json
{
  "data": {
    "data": [
      {
        "id": "00000000-0000-0000-0000-00000000c001",
        "name": "Acme Corp",
        "status": "Trial",
        "timezone": "UTC",
        "currency": "USD",
        "logoUrl": null,
        "registeredAtUtc": "2026-01-01T00:00:00Z",
        "approvedAtUtc": "2026-01-01T00:00:00Z",
        "suspendedAtUtc": null,
        "suspendedReason": null,
        "rejectedAtUtc": null,
        "rejectedReason": null,
        "isDeleted": false,
        "createdAtUtc": "2026-01-01T00:00:00Z",
        "updatedAtUtc": null
      }
    ],
    "page": 1, "pageSize": 20, "totalCount": 1, "totalPages": 1
  },
  "message": "Request completed successfully.", "correlationId": "..."
}
```

**Validation** — No validator found for `GetCompaniesQuery`. `page`/`pageSize` normalized silently in `GetCompaniesQueryHandler`.

**Error Codes** — `401`, `403`.

**Examples**
```bash
curl -X GET "https://api.example.com/api/v1/platform/companies?status=PendingApproval&search=acme" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN"
```

**Best Practices**
- Filter by `status=PendingApproval` to drive an approvals queue view.

---

### Get Company Detail

**Purpose** — View one company's full profile, including its employee headcount and Admin-user roster — e.g. before deciding whether to suspend it.

**URL** — `/api/v1/platform/companies/{id}`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin).

**Authentication** — `IsSuperAdmin` policy.

**Request** — Path param `id` (guid).

**Response** — `200 OK`, `ApiResponse<CompanyDetailDto>` — all `CompanyDto` fields plus:
```json
{
  "data": {
    "id": "00000000-0000-0000-0000-00000000c001",
    "name": "Acme Corp",
    "status": "Trial",
    "timezone": "UTC",
    "currency": "USD",
    "logoUrl": null,
    "registeredAtUtc": "2026-01-01T00:00:00Z",
    "approvedAtUtc": "2026-01-01T00:00:00Z",
    "suspendedAtUtc": null,
    "suspendedReason": null,
    "rejectedAtUtc": null,
    "rejectedReason": null,
    "isDeleted": false,
    "createdAtUtc": "2026-01-01T00:00:00Z",
    "updatedAtUtc": null,
    "employeeCount": 12,
    "admins": [
      { "userId": "...", "userName": "admin", "email": "admin@acme.example.com", "isActive": true }
    ]
  },
  "message": "Request completed successfully.", "correlationId": "..."
}
```

**Validation** — None.

**Error Codes** — `401`, `403`, `404` (bare `NotFound()`, not the `ApiErrorResponse` envelope — note inconsistency, same as `ClientController.GetById`).

**Examples**
```bash
curl -X GET "https://api.example.com/api/v1/platform/companies/00000000-0000-0000-0000-00000000c001" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN"
```

**Best Practices**
- Use `admins[].isActive` to spot a company whose only Admin account has been deactivated (locked out) — not surfaced anywhere else.

---

### Create Company

**Purpose** — Super Admin directly provisions a new tenant with no approval workflow — distinct from public self-registration. Lands straight in `Active`.

**URL** — `/api/v1/platform/companies`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin); `Content-Type: application/json`.

**Authentication** — `IsSuperAdmin` policy.

**Request** — Body (`CreateCompanyCommand`):
```json
{ "name": "Acme Corp", "timezone": "UTC", "currency": "USD", "logoUrl": null }
```
Required: `name`. Optional: `timezone` (default `"UTC"`), `currency` (default `"USD"`), `logoUrl`.

**Response** — `201 Created`, `Location` header from `CreatedAtAction(Get)`, `ApiResponse<CompanyDto>`, message `"Company created successfully."`. Note: no first Admin user is created by this endpoint (unlike `RegisterCompanyCommand`) — the response DTO has no `admins`/`employeeCount` fields (those only exist on `CompanyDetailDto`, returned by `Get`, not `Create`).

**Validation** (`CreateCompanyCommandValidator`):
| Field | Rules |
|---|---|
| `name` | NotEmpty, MaxLength(200) |
| `timezone` | NotEmpty, MaxLength(100) |
| `currency` | NotEmpty, MaxLength(10) |
| `logoUrl` | MaxLength(500) |

No duplicate-name check exists for this endpoint (unlike Clients) — two companies can share a `name`.

**Error Codes** — `400 VALIDATION_ERROR`, `401`, `403`.

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/platform/companies" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Acme Corp","timezone":"UTC","currency":"USD"}'
```

**Best Practices**
- Because this endpoint creates a company with zero users, you must separately provision an Admin user through the tenant-scoped user-management API (or reset-password flow below) before anyone can log in.

---

### Update Company

**Purpose** — Edit a company's name/timezone/currency/logo.

**URL** — `/api/v1/platform/companies/{id}`

**Method** — PUT

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin); `Content-Type: application/json`.

**Authentication** — `IsSuperAdmin` policy.

**Request** — Path param `id` (guid). Body (`UpdateCompanyCommand`): `id` (must match route), `name` (required), `timezone`, `currency`, `logoUrl` (optional).

**Response** — `200 OK`, `ApiResponse<CompanyDto>`.

**Validation** (`UpdateCompanyCommandValidator`) — same rules as Create.

**Error Codes** — `400 ID_MISMATCH` (route/body id mismatch, controller-level check), `400 VALIDATION_ERROR`, `401`, `403`, `404 NOT_FOUND`.

**Examples**
```bash
curl -X PUT "https://api.example.com/api/v1/platform/companies/00000000-0000-0000-0000-00000000c001" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"id":"00000000-0000-0000-0000-00000000c001","name":"Acme Corp Inc","timezone":"UTC","currency":"USD"}'
```

**Best Practices**
- Status transitions (`activate`/`suspend`/`approve`/`reject`) are NOT done through this endpoint — `Status` is not a field on `UpdateCompanyCommand`. Use the dedicated action endpoints below.

---

### Delete Company

**Purpose** — Soft-delete a company (offboarding a tenant entirely).

**URL** — `/api/v1/platform/companies/{id}`

**Method** — DELETE

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin).

**Authentication** — `IsSuperAdmin` policy.

**Request** — Path param `id` (guid).

**Response** — `204 No Content`.

**Validation** — None.

**Error Codes** — `401`, `403`, `404 NOT_FOUND`.

**Examples**
```bash
curl -X DELETE "https://api.example.com/api/v1/platform/companies/00000000-0000-0000-0000-00000000c001" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN"
```

**Best Practices**
- This does not revoke tokens or force-logout users by itself (unlike `suspend`) — pair with `force-logout` first if you need users kicked out immediately, since a soft-deleted-but-still-Active-status company is not blocked by `TenantStatusMiddleware` (which only checks `Status`, not `IsDeleted`). Confirm this behavior against `ICompanyRepository.GetByIdAsync`'s soft-delete filtering if immediate lockout is required — worth flagging to the team as a potential gap.

---

### Restore Company

**Purpose** — Undo a soft-delete.

**URL** — `/api/v1/platform/companies/{id}/restore`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin).

**Authentication** — `IsSuperAdmin` policy.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — Handler-level guard only: throws `InvalidOperationException("Company is not deleted and cannot be restored.")` if `company.IsDeleted` is already `false`.

**Error Codes** — `401`, `403`, `404 NOT_FOUND` (id never existed, via `GetByIdIncludingDeletedAsync`), `409 CONFLICT` (company isn't currently deleted).

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/platform/companies/00000000-0000-0000-0000-00000000c001/restore" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN"
```

**Best Practices**
- Check `isDeleted` via `GET /platform/companies/{id}` first if unsure of current state, to avoid a predictable `409`.

---

### Activate Company

**Purpose** — General "make this company able to log in again" action: `Suspended`/`Inactive` → `Active`. (For the one-time `PendingApproval` → `Trial` transition, use Approve instead — see its docstring cross-reference.)

**URL** — `/api/v1/platform/companies/{id}/activate`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin).

**Authentication** — `IsSuperAdmin` policy.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — None — no state-machine guard. Can be called from any status, including `PendingApproval` or `Rejected`, and will force `Status = Active` regardless (also clears `SuspendedAtUtc`/`SuspendedReason`). This differs from `Approve`, which only accepts `PendingApproval`.

**Error Codes** — `401`, `403`, `404 NOT_FOUND`.

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/platform/companies/00000000-0000-0000-0000-00000000c001/activate" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN"
```

**Best Practices**
- Because this endpoint has no status guard, be deliberate — calling it on a `PendingApproval` company skips the approval audit trail entry (`"Approved"` action) that `Approve` would have recorded, and sets `ApprovedAtUtc` unchanged (still `null`) since only `Approve`'s handler sets that field.

---

### Suspend Company

**Purpose** — Immediately lock a company out: sets `Status = Suspended` and revokes every refresh token for its users in the same operation, so already-logged-in sessions die on their next request via `TenantStatusMiddleware`.

**URL** — `/api/v1/platform/companies/{id}/suspend`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin); `Content-Type: application/json` (if sending a body).

**Authentication** — `IsSuperAdmin` policy.

**Request** — Path param `id` (guid). Body (`SuspendCompanyRequest`, optional — controller accepts `null`):
```json
{ "reason": "Non-payment" }
```

**Response** — `204 No Content`.

**Validation** (`SuspendCompanyCommandValidator`) — `Reason` MaxLength(500). Note: this validator runs against `SuspendCompanyCommand` (constructed by the controller from the request body + route id), not directly against the wire-level `SuspendCompanyRequest`.

**Error Codes** — `400 VALIDATION_ERROR` (reason too long), `401`, `403`, `404 NOT_FOUND`.

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/platform/companies/00000000-0000-0000-0000-00000000c001/suspend" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"reason":"Non-payment"}'
```

**Best Practices**
- Always pass a `reason` — it's stored on the company (`SuspendedReason`) and surfaced back to the tenant's users in the `403 COMPANY_SUSPENDED` message they'll see from `TenantStatusMiddleware` on their next request (`"Your company's account is Suspended. Contact your administrator."` — note: the specific reason text is not echoed in that middleware message, only the status, so don't rely on end users seeing the `reason` value directly).
- This is the correct action for "lock this tenant out right now" — `activate`'s inverse.

---

### Approve Company

**Purpose** — Approve a company still awaiting registration approval: `PendingApproval` → `Trial`, and stamps `ApprovedAtUtc`. Enables the tenant's Admin user to log in for the first time.

**URL** — `/api/v1/platform/companies/{id}/approve`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin).

**Authentication** — `IsSuperAdmin` policy.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — Handler-level guard: throws `InvalidOperationException($"Company {name} is not pending approval (currently {status}).")` unless `company.Status == PendingApproval`.

**Error Codes** — `401`, `403`, `404 NOT_FOUND`, `409 CONFLICT` (not currently `PendingApproval`).

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/platform/companies/00000000-0000-0000-0000-00000000c001/approve" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN"
```

**Best Practices**
- Check `status == "PendingApproval"` via `GET /platform/companies/{id}` (or filter the list endpoint by `status=PendingApproval`) before calling, to avoid a predictable `409` — this is a one-shot transition, not idempotent.

---

### Reject Company

**Purpose** — Reject a company still awaiting registration approval: `PendingApproval` → `Rejected`.

**URL** — `/api/v1/platform/companies/{id}/reject`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin); `Content-Type: application/json` (if sending a body).

**Authentication** — `IsSuperAdmin` policy.

**Request** — Path param `id` (guid). Body (`RejectCompanyRequest`, optional):
```json
{ "reason": "Duplicate registration" }
```

**Response** — `204 No Content`.

**Validation** — No dedicated FluentValidation validator found for `RejectCompanyCommand`/`RejectCompanyRequest` (unlike `SuspendCompanyCommand`, which has one for its `Reason` field) — `reason` length is unbounded at the API layer. Handler-level guard: throws `InvalidOperationException` unless `company.Status == PendingApproval` (same pattern as Approve).

**Error Codes** — `401`, `403`, `404 NOT_FOUND`, `409 CONFLICT` (not currently `PendingApproval`).

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/platform/companies/00000000-0000-0000-0000-00000000c001/reject" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"reason":"Duplicate registration"}'
```

**Best Practices**
- Same as Approve: this is a one-shot transition from `PendingApproval` only — check status first to avoid `409`.
- Since `reason` is unvalidated, apply a reasonable client-side length cap yourself (e.g. mirror the 500-char cap used for `Suspend`) since the server won't do it for you here.

---

### Force Logout Company

**Purpose** — Revoke every refresh token for a company's users without changing its `Status` — for on-demand use (e.g. suspected compromised admin account) separate from the automatic force-logout that `Suspend` performs as a side effect.

**URL** — `/api/v1/platform/companies/{id}/force-logout`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin).

**Authentication** — `IsSuperAdmin` policy.

**Request** — Path param `id` (guid). No body.

**Response** — `204 No Content`.

**Validation** — None.

**Error Codes** — `401`, `403`, `404 NOT_FOUND`.

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/platform/companies/00000000-0000-0000-0000-00000000c001/force-logout" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN"
```

**Best Practices**
- Note this only revokes refresh tokens — any still-valid short-lived access token (≤15 min per `RegisterCompanyCommandHandler`'s `ExpiresInSeconds = 900`) keeps working until it naturally expires, since `Status` is untouched and `TenantStatusMiddleware` only blocks on `Status`. Pair with `Suspend` if you need the access token itself blocked immediately.

---

### Reset Admin Password

**Purpose** — Issue a password-reset token for one of a company's Admin users, reusing the self-service forgot-password mechanism — for Super Admin-assisted account recovery.

**URL** — `/api/v1/platform/companies/{id}/admins/{userId}/reset-password`

**Method** — POST

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin).

**Authentication** — `IsSuperAdmin` policy.

**Request** — Path params: `id` (company guid), `userId` (guid). No body.

**Response** — `200 OK`, `ApiResponse<string>` — `data` is the raw reset token:
```json
{ "data": "a1b2c3...", "message": "Password reset token issued.", "correlationId": "..." }
```

**Validation** — None beyond the controller's own ownership check.

**Error Codes** — `401`, `403`, `404` (bare `NotFound()`, not `ApiErrorResponse` — the controller explicitly checks `user == null || user.CompanyId != id` and returns plain `NotFound()`, so a `userId` that exists but belongs to a *different* company also 404s, not 403 — this correctly avoids leaking cross-tenant user existence).

**Examples**
```bash
curl -X POST "https://api.example.com/api/v1/platform/companies/00000000-0000-0000-0000-00000000c001/admins/00000000-0000-0000-0000-00000000u001/reset-password" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN"
```

**Best Practices**
- This endpoint is not restricted to users with the `Admin` role despite its name/docstring — it calls `_authRepo.GetByIdAsync(userId)` and only checks `CompanyId` matches, not `Role`. Verify against `IAuthRepository.GetByIdAsync` / the `User` entity if role-scoping matters for your use case; as written, it will issue a reset token for any user in the company, not only Admins.
- Treat the returned token as sensitive — it grants password-reset capability; deliver it to the admin out-of-band, don't log or display it in a UI accessible to other users.

---

## Platform Dashboard API

Controller: `backend/EMS.API/Controllers/PlatformDashboardController.cs`. Route: `api/v1/platform/dashboard`. Class-level `[Authorize(Policy = "IsSuperAdmin")]`.

### Get Platform Dashboard Summary

**Purpose** — Cross-company overview for the Super Admin landing page: counts by status, total employees platform-wide, and the most recently registered companies.

**URL** — `/api/v1/platform/dashboard/summary`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin).

**Authentication** — `IsSuperAdmin` policy.

**Request** — Query param `recentCount` (int, optional). Controller only overrides the query's default when `recentCount > 0`; values `<= 0` (including omitted/`0`) fall back to `GetPlatformDashboardSummaryQuery`'s default of `5`.

**Response** — `200 OK`, `ApiResponse<PlatformDashboardSummaryDto>`:
```json
{
  "data": {
    "totalCompanies": 42,
    "activeCompanies": 30,
    "suspendedCompanies": 2,
    "trialCompanies": 8,
    "totalEmployeesAcrossAllCompanies": 1350,
    "recentRegistrations": [
      { "id": "...", "name": "Acme Corp", "status": "Trial", "...": "..." }
    ]
  },
  "message": "Request completed successfully.", "correlationId": "..."
}
```
Note: `totalCompanies`/`activeCompanies`/`suspendedCompanies`/`trialCompanies` are derived purely from `GetStatusCountsAsync` grouped counts — `Inactive`, `PendingApproval`, and `Rejected` companies are counted in `totalCompanies` (sum of all status counts) but have no dedicated breakdown field of their own.

**Validation** — None; no upper bound enforced on `recentCount` (unlike the paged list endpoints' 100-item clamp) — a very large value will return that many recent registrations unclamped.

**Error Codes** — `401`, `403`.

**Examples**
```bash
curl -X GET "https://api.example.com/api/v1/platform/dashboard/summary?recentCount=10" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN"
```

**Best Practices**
- Cap `recentCount` client-side to a sane value (e.g. ≤ 50) since the API does not enforce one itself.
- `recentRegistrations` items are `CompanyDto`, not `CompanyDetailDto` — no `employeeCount`/`admins` per item; fetch `GET /platform/companies/{id}` if you need that detail for a specific entry.

---

## Platform Settings API

Controller: `backend/EMS.API/Controllers/PlatformSettingsController.cs`. Route: `api/v1/platform/settings`. Class-level `[Authorize(Policy = "IsSuperAdmin")]`. Reads/writes a single seeded `PlatformSettings` row (no id — there is only ever one).

### Get Platform Settings

**Purpose** — Fetch the current platform-wide registration toggles, e.g. to populate a Super Admin settings screen.

**URL** — `/api/v1/platform/settings`

**Method** — GET

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin).

**Authentication** — `IsSuperAdmin` policy.

**Request** — No parameters.

**Response** — `200 OK`
```json
{
  "data": { "isPublicRegistrationEnabled": true, "requireApprovalForNewCompanies": true },
  "message": "Request completed successfully.", "correlationId": "..."
}
```

**Validation** — None (no request body).

**Error Codes** — `401`, `403`.

**Examples**
```bash
curl -X GET "https://api.example.com/api/v1/platform/settings" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN"
```

**Best Practices**
- Cache this client-side sparingly, if at all — it directly governs whether `POST /company-registration` will succeed (`409` if disabled), so stale cached values can produce confusing UX for prospective tenants.

---

### Update Platform Settings

**Purpose** — Toggle whether public self-registration is open, and whether new registrations require Super Admin approval before activating.

**URL** — `/api/v1/platform/settings`

**Method** — PUT

**Headers** — `Authorization: Bearer <access_token>` (SuperAdmin); `Content-Type: application/json`.

**Authentication** — `IsSuperAdmin` policy.

**Request** — Body (`UpdatePlatformSettingsCommand`), both fields required (booleans, no `[Required]`/nullable — always present in the JSON):
```json
{ "isPublicRegistrationEnabled": true, "requireApprovalForNewCompanies": true }
```

**Response** — `200 OK`
```json
{
  "data": { "isPublicRegistrationEnabled": true, "requireApprovalForNewCompanies": true },
  "message": "Platform settings updated.", "correlationId": "..."
}
```
The controller re-fetches settings via a second `GetPlatformSettingsQuery` after the update, so the response always reflects the persisted state.

**Validation** — **No FluentValidation validator found** for `UpdatePlatformSettingsCommand` — both fields are plain `bool`, so there is nothing to validate beyond JSON deserialization (a missing field deserializes to `false`, not a `400`).

**Error Codes** — `401`, `403`.

**Examples**
```bash
curl -X PUT "https://api.example.com/api/v1/platform/settings" \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"isPublicRegistrationEnabled":false,"requireApprovalForNewCompanies":true}'
```

**Best Practices**
- Always send both fields explicitly — since there's no validator and both are non-nullable `bool`, an omitted field silently becomes `false` rather than erroring, which could unintentionally disable public registration.
- Changing `requireApprovalForNewCompanies` only affects future registrations — it does not retroactively move any already-`PendingApproval` or already-`Trial` company between states.

---

## Summary of endpoints covered

**Clients API** (`api/v1/clients`)
- `GET /clients` — list
- `GET /clients/{id}` — get by id
- `POST /clients` — create
- `PUT /clients/{id}` — update
- `DELETE /clients/{id}` — soft delete
- `POST /clients/{id}/activate`
- `POST /clients/{id}/deactivate`
- `POST /clients/{id}/archive`
- `POST /clients/{id}/restore`

**Company Registration API** (`api/v1/company-registration`)
- `GET /company-registration/status`
- `POST /company-registration`

**Platform Audit Logs API** (`api/v1/platform/audit-logs`)
- `GET /platform/audit-logs`

**Platform Companies API** (`api/v1/platform/companies`)
- `GET /platform/companies` — list
- `GET /platform/companies/{id}` — get detail
- `POST /platform/companies` — create
- `PUT /platform/companies/{id}` — update
- `DELETE /platform/companies/{id}` — soft delete
- `POST /platform/companies/{id}/restore`
- `POST /platform/companies/{id}/activate`
- `POST /platform/companies/{id}/suspend`
- `POST /platform/companies/{id}/approve`
- `POST /platform/companies/{id}/reject`
- `POST /platform/companies/{id}/force-logout`
- `POST /platform/companies/{id}/admins/{userId}/reset-password`

**Platform Dashboard API** (`api/v1/platform/dashboard`)
- `GET /platform/dashboard/summary`

**Platform Settings API** (`api/v1/platform/settings`)
- `GET /platform/settings`
- `PUT /platform/settings`
---
title: Dashboard, Reports, Exports, Audit Logs, Health APIs
---

> Derived from the actual controller/handler/validator code as of this writing (`backend/EMS.API/Controllers/{Dashboard,Reports,Exports,AuditLogs,Health}Controller.cs` and their `EMS.Application` counterparts). Where `docs/api-specification.md` disagrees with the code (e.g. it still marks `/exports` as "unbuilt"), the code wins and the discrepancy is called out.

## Conventions used below

- All non-Health endpoints are versioned under `api/v1/...` and return the shared envelope `ApiResponse<T>` on success: `{ "data": T, "message": string, "correlationId": string }` (defined in `EMS.API/Controllers/AuthController.cs:236`).
- Health endpoints are **not** versioned — the controller route is `[Route("health")]`, not `api/v1/health`.
- Standard error envelope (`ApiErrorResponse`, same file, line 246) — `{ "status": int, "code": string, "message": string, "errors": object|null, "correlationId": string }` — is produced by `EMS.API/Middleware/ExceptionHandlingMiddleware.cs` for:
  - FluentValidation failures → `400`, `code: "VALIDATION_ERROR"`, `errors: [{ propertyName, errorMessage }, ...]`
  - `InvalidOperationException` containing "not found" → `404`, `code: "NOT_FOUND"`
  - other `InvalidOperationException` → `409`, `code: "CONFLICT"`
  - `UnauthorizedAccessException` → `403`, `code: "FORBIDDEN"` (message is always the generic "You do not have permission to perform this action.", regardless of the exception's own message)
  - anything else → `500`, `code: "INTERNAL_ERROR"`
  - `401` (no/invalid JWT) is produced by the ASP.NET Core authentication middleware before MediatR runs, so it is **not** wrapped in `ApiErrorResponse` — it's the default JWT bearer challenge response.
  - **Important distinction**: a query-string value that fails *model binding* (e.g. a malformed GUID, or a string that doesn't match an enum like `AssetStatus`) never reaches FluentValidation — `[ApiController]`'s built-in behavior short-circuits with its own `400 application/problem+json` `ValidationProblemDetails` body (`{ type, title, status, errors, traceId }`), which looks different from `ApiErrorResponse`. This project has not customized `ApiBehaviorOptions.InvalidModelStateResponseFactory`, so this default shape applies wherever a validator doesn't also duplicate the check (e.g. an endpoint with no FluentValidation validator at all).
- No endpoint in this scope defines request bodies — every action binds from route/query parameters only (`[FromQuery]`), consistent with these being read-only reporting/export/health surfaces.

---

## Dashboard API

Controller: `EMS.API/Controllers/DashboardController.cs`. Route prefix: `api/v1/dashboard`. Class-level `[Authorize(Policy = "CanViewDashboard")]` (roles `Admin`, `HR`, `Manager` — `Program.cs:319`).

### GET /api/v1/dashboard/summary

**Purpose**: Returns aggregate employee, attendance, leave, and per-department metrics for the main dashboard screen. A client calls this on dashboard load, and again whenever the department/date filter changes.

**URL**: `/api/v1/dashboard/summary`

**Method**: GET

**Headers**: `Authorization: Bearer <access_token>` (required). No special request headers.

**Authentication**: Policy `CanViewDashboard` — roles `Admin`, `HR`, `Manager`.

**Request**:
Query params (bound to `GetDashboardSummaryQuery`):
| Param | Type | Required | Notes |
|---|---|---|---|
| `departmentId` | guid | optional | scopes metrics to one department |
| `officeLocationId` | guid | optional | **accepted but not enforced** — there is no `OfficeLocation` entity in the domain model yet; kept only for API-contract parity with `api-specification.md`. Passing it has no filtering effect. |
| `date` | date | optional | defaults to "today" (UTC) server-side if omitted |

No request body.

**Response** (`200 OK`, `ApiResponse<DashboardSummaryDto>`):
```json
{
  "data": {
    "totalEmployees": 120,
    "activeEmployees": 110,
    "inactiveEmployees": 10,
    "attendance": { "present": 95, "absent": 5, "late": 3, "onLeave": 7 },
    "leave": { "pending": 4, "approvedToday": 2, "rejectedToday": 1 },
    "departments": [
      { "departmentId": "guid", "departmentName": "Engineering", "activeEmployees": 40 }
    ]
  },
  "message": "Request completed successfully.",
  "correlationId": "..."
}
```

**Validation** (`GetDashboardSummaryQueryValidator`): `date` (if supplied) must not be in the future (`date.Value.Date <= DateTime.UtcNow.Date`), else `"Date cannot be in the future."`.

**Error Codes**:
- `400` — `date` is in the future (`VALIDATION_ERROR`).
- `401` — missing/invalid JWT.
- `403` — authenticated but not Admin/HR/Manager (`FORBIDDEN`).
- `500` — unexpected error (`INTERNAL_ERROR`).

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  "https://api.example.com/api/v1/dashboard/summary?departmentId=3fa85f64-5717-4562-b3fc-2c963f66afa6&date=2026-07-31"
```
Response: `200 OK` with the JSON shape above.

**Best Practices**:
- Omit `date` to always get "today" without doing client-side date math/timezone conversion.
- Don't rely on `officeLocationId` filtering — it's currently a no-op.
- Cache/poll sparingly; this aggregates several tables per call.

---

## Reports API

Controller: `EMS.API/Controllers/ReportsController.cs`. Route prefix: `api/v1/reports`. Class-level `[Authorize(Policy = "CanViewReports")]` (roles `Admin`, `HR`, `Manager`). Every endpoint here exposes org-wide aggregate data — none are scoped to a single employee/manager's team.

### GET /api/v1/reports/employees

**Purpose**: Total/active/inactive employee counts, for a reports landing page or KPI tile.

**URL**: `/api/v1/reports/employees`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`.
**Authentication**: Policy `CanViewReports` (Admin, HR, Manager).
**Request**: No params, no body (`GetEmployeeReportQuery` has no fields).

**Response** (`200 OK`, `ApiResponse<EmployeeReportDto>`):
```json
{
  "data": { "totalEmployees": 120, "activeEmployees": 110, "inactiveEmployees": 10 },
  "message": "Request completed successfully.",
  "correlationId": "..."
}
```

**Validation**: None — no validator registered for this query.

**Error Codes**: `401` (no token), `403` (wrong role), `500` (unexpected).

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/reports/employees
```

**Best Practices**:
- Cheap, side-effect-free — safe to call frequently for a summary tile.

---

### GET /api/v1/reports/departments

**Purpose**: Employee headcount grouped by department, for a departments breakdown chart/table.

**URL**: `/api/v1/reports/departments`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`.
**Authentication**: Policy `CanViewReports`.
**Request**: No params (`GetDepartmentCountsQuery` has no fields).

**Response** (`200 OK`, `ApiResponse<IEnumerable<DepartmentCountDto>>`):
```json
{
  "data": [
    { "departmentId": "guid", "departmentName": "Engineering", "employeeCount": 40 },
    { "departmentId": "guid", "departmentName": "Sales", "employeeCount": 25 }
  ],
  "message": "Request completed successfully.",
  "correlationId": "..."
}
```

**Validation**: None.

**Error Codes**: `401`, `403`, `500`.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" https://api.example.com/api/v1/reports/departments
```

**Best Practices**:
- Soft-deleted departments are excluded server-side; no client-side filtering needed.

---

### GET /api/v1/reports/departments/export

**Purpose**: Downloads the same department-headcount data as `/reports/departments`, as a CSV file (e.g. for offline analysis or attaching to an email).

**URL**: `/api/v1/reports/departments/export`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`. Response headers: `Content-Type: text/csv`, and a `Content-Disposition` attachment header with filename `department-counts_{yyyyMMddHHmmss}.csv` (set by ASP.NET Core's `File(content, contentType, fileName)` result).
**Authentication**: Policy `CanViewReports`.
**Request**: No params (`ExportDepartmentCountsQuery` has no fields).

**Response**: `200 OK`, binary CSV body. Columns: `DepartmentId,DepartmentName,EmployeeCount`. Each field is passed through `CsvFieldFormatter.Escape` — RFC-4180 quoting, plus CSV/formula-injection neutralization (CWE-1236): any field beginning with `=`, `+`, `-`, `@`, tab, or CR gets an apostrophe prefix so Excel/Sheets can't execute it as a formula.

**Validation**: None.

**Error Codes**: `401`, `403`, `500`.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  -o department-counts.csv \
  https://api.example.com/api/v1/reports/departments/export
```
Response: binary CSV stream saved to `department-counts.csv`.

**Best Practices**:
- Treat the response as an opaque binary stream — don't try to parse it as `ApiResponse<T>` JSON, this endpoint's `[ProducesResponseType(200)]` has no typed body.
- Use the server-provided filename from `Content-Disposition` rather than hardcoding one, since it's timestamped.

---

### GET /api/v1/reports/leave-summary

**Purpose**: Leave request counts by status (pending/approved/rejected) within a date range, for a leave-summary widget.

**URL**: `/api/v1/reports/leave-summary`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`.
**Authentication**: Policy `CanViewReports`.

**Request**: Query params (`GetLeaveSummaryQuery`):
| Param | Type | Required |
|---|---|---|
| `from` | date | required |
| `to` | date | required |

**Response** (`200 OK`, `ApiResponse<LeaveSummaryReportDto>`):
```json
{
  "data": { "totalRequests": 30, "pending": 4, "approved": 20, "rejected": 6 },
  "message": "Request completed successfully.",
  "correlationId": "..."
}
```

**Validation** (`GetLeaveSummaryQueryValidator`):
- `from` must not be `default(DateTime)` → `"from is required."`
- `to` must not be `default(DateTime)` → `"to is required."`
- `from` must be `<= to` → `"from must be before or equal to to."`

**Error Codes**: `400` (missing/inverted range — `VALIDATION_ERROR`), `401`, `403`, `500`.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  "https://api.example.com/api/v1/reports/leave-summary?from=2026-07-01&to=2026-07-31"
```

**Best Practices**:
- Always supply both `from` and `to` — omitting either fails validation (they default to `DateTime` min value on binding, which the validator rejects).

---

### GET /api/v1/reports/employee-turnover

**Purpose**: Lists employees who joined or exited within a date range (turnover report).

**URL**: `/api/v1/reports/employee-turnover`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`.
**Authentication**: Policy `CanViewReports`.

**Request**: Query params (`GetEmployeeJoinExitQuery`): `from` (date, required), `to` (date, required).

**Response** (`200 OK`, `ApiResponse<IEnumerable<EmployeeJoinExitDto>>`):
```json
{
  "data": [
    { "employeeId": "guid", "employeeName": "Jane Doe", "joinDate": "2020-03-01", "exitDate": null },
    { "employeeId": "guid", "employeeName": "John Smith", "joinDate": "2018-01-15", "exitDate": "2026-07-20" }
  ],
  "message": "Request completed successfully.",
  "correlationId": "..."
}
```

**Validation** (`GetEmployeeJoinExitQueryValidator`): same three rules as leave-summary — `from` required, `to` required, `from <= to`.

**Error Codes**: `400`, `401`, `403`, `500`.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  "https://api.example.com/api/v1/reports/employee-turnover?from=2026-01-01&to=2026-07-31"
```

**Best Practices**:
- `exitDate` is `null` for still-active employees — treat `null` as "still employed", not as missing data.

---

### GET /api/v1/reports/employee-turnover/export

**Purpose**: CSV download of the same turnover data as `/reports/employee-turnover`.

**URL**: `/api/v1/reports/employee-turnover/export`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`. Response: `Content-Type: text/csv`, `Content-Disposition` attachment, filename `employee-turnover_{from:yyyyMMdd}_{to:yyyyMMdd}.csv`.
**Authentication**: Policy `CanViewReports`.

**Request**: Query params (`ExportEmployeeJoinExitQuery`): `from` (date, required), `to` (date, required).

**Response**: `200 OK`, binary CSV. Columns: `EmployeeId,EmployeeName,JoinDate,ExitDate`, each field CSV/formula-escaped via `CsvFieldFormatter.Escape`.

**Validation** (`ExportEmployeeJoinExitQueryValidator`): identical to `/employee-turnover` — `from` required, `to` required, `from <= to`.

**Error Codes**: `400` (`VALIDATION_ERROR`), `401`, `403`, `500`.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  -o employee-turnover.csv \
  "https://api.example.com/api/v1/reports/employee-turnover/export?from=2026-01-01&to=2026-07-31"
```

**Best Practices**:
- Same as the department export: don't parse as JSON; read the filename from `Content-Disposition`.

---

## Exports API

Controller: `EMS.API/Controllers/ExportsController.cs`. Route prefix: `api/v1/exports`. Class-level `[Authorize]` (any authenticated user); every action additionally layers a method-level policy **except** `reimbursements`, which relies purely on the class-level `[Authorize]` plus in-handler self-scoping. All seven endpoints return an Excel (`.xlsx`) file **except** `dashboard-summary`, which returns a PDF. Note: `docs/api-specification.md` §18 still describes this module as "unbuilt" — that is stale; the code is fully implemented.

All export handlers build rows in memory via `IExcelExportService.GenerateAsync(sheetName, headers, rows)` (or `IPdfService` for the PDF one) and return `ExportFileResult { Content: byte[], ContentType, FileName }`, which the controller wraps in `File(content, contentType, fileName)`.

### GET /api/v1/exports/employees

**Purpose**: Export the employee list (same filters as the employee list screen) to Excel.

**URL**: `/api/v1/exports/employees`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`. Response: `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `Content-Disposition` attachment, filename `employees_{yyyyMMddHHmmss}.xlsx`.
**Authentication**: Policy `CanManageEmployees` (roles `Admin`, `HR`).

**Request**: Query params (`ExportEmployeesQuery`), all optional:
| Param | Type |
|---|---|
| `search` | string |
| `sortBy` | string |
| `sortDir` | string |
| `departmentId` | guid |
| `status` | string |

**Response**: `200 OK`, binary `.xlsx`. Columns: `Employee Code, First Name, Last Name, Email, Phone Number, Department, Designation, Join Date, Exit Date, Employment Status`.

**Validation**: None — no validator registered for `ExportEmployeesQuery`.

**Error Codes**: `401`, `403` (not Admin/HR), `500`.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  -o employees.xlsx \
  "https://api.example.com/api/v1/exports/employees?departmentId=3fa85f64-5717-4562-b3fc-2c963f66afa6&status=Active"
```

**Best Practices**:
- Scoped to the caller's own company (`ICurrentUserService.CompanyId`) automatically — no `companyId` param exists or is needed.

---

### GET /api/v1/exports/attendance

**Purpose**: Export attendance records (same filters as the attendance list) to Excel. Manager callers are automatically scoped to their own team.

**URL**: `/api/v1/exports/attendance`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`. Response: `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, attachment, filename `attendance_{yyyyMMddHHmmss}.xlsx`.
**Authentication**: Policy `CanViewReports` (Admin, HR, Manager).

**Request**: Query params (`ExportAttendanceQuery`), all optional:
| Param | Type |
|---|---|
| `employeeId` | guid |
| `departmentId` | guid |
| `managerId` | guid |
| `dateFrom` | date |
| `dateTo` | date |
| `status` | string |
| `isLateArrival` | bool |
| `isEarlyLeave` | bool |

`requestingUserId`, `isAdminOrHr`, `isManager` also exist on the C# type but are **overwritten server-side by the controller from the caller's JWT/role claims** (`ExportsController.cs:39-41`) — any values supplied for them in the query string are ignored.

**Response**: `200 OK`, binary `.xlsx`. Columns: `Employee Code, Employee Name, Attendance Date, Check-In (UTC), Check-Out (UTC), Status, Late Arrival, Early Leave, Total Work Minutes, Notes`.

**Validation** (`ExportAttendanceQueryValidator`): when both `dateFrom` and `dateTo` are supplied, `dateFrom` must be `<= dateTo`, else `"dateFrom must be before or equal to dateTo."`.

**Authorization/scoping logic in the handler** (not just the `[Authorize]` attribute):
- Admin/HR: sees everything; `employeeId` filter applied as-is; if `managerId` is supplied, further scoped to that manager's direct reports.
- Manager (non-Admin/HR): always scoped to themself + their direct reports. If `employeeId` is supplied and is **not** one of their own reports (or themselves), the handler throws `UnauthorizedAccessException` → `403 FORBIDDEN`. If the caller has no linked `EmployeeId` record at all, an empty result set is returned rather than erroring.

**Error Codes**:
- `400` — `dateFrom` after `dateTo` (`VALIDATION_ERROR`).
- `401` — no/invalid token.
- `403` — role check fails the `CanViewReports` policy, **or** a Manager requests an `employeeId` outside their team (`FORBIDDEN`).
- `500` — unexpected.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  -o attendance.xlsx \
  "https://api.example.com/api/v1/exports/attendance?dateFrom=2026-07-01&dateTo=2026-07-31&isLateArrival=true"
```

**Best Practices**:
- Don't attempt to pass `requestingUserId`/`isAdminOrHr`/`isManager` yourself — they're derived server-side from the token and any client-supplied value is discarded.
- If you're a Manager, omit `employeeId` to get your whole team's records in one call rather than looping per employee.

---

### GET /api/v1/exports/leave-requests

**Purpose**: Export leave requests (same filters as the leave list) to Excel. Access is restricted to Admin/HR/Manager, so — unlike attendance — there is no self-scoping; a Manager sees the same unrestricted set as Admin/HR here.

**URL**: `/api/v1/exports/leave-requests`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`. Response: `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, attachment, filename `leave-requests_{yyyyMMddHHmmss}.xlsx`.
**Authentication**: Policy `CanViewReports` (Admin, HR, Manager).

**Request**: Query params (`ExportLeaveRequestsQuery`), all optional:
| Param | Type |
|---|---|
| `employeeId` | guid |
| `leaveTypeId` | guid |
| `year` | int |
| `status` | string |

**Response**: `200 OK`, binary `.xlsx`. Columns: `Employee Code, Employee Name, Leave Type, Start Date, End Date, Total Days, Status, Reason, Decision At, Decision Comments`.

**Validation**: None — no validator registered.

**Error Codes**: `401`, `403`, `500`.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  -o leave-requests.xlsx \
  "https://api.example.com/api/v1/exports/leave-requests?year=2026&status=Approved"
```

**Best Practices**:
- `leaveTypeId` filter includes soft-deleted leave types transparently — the handler resolves leave type names via `GetLeaveTypeByIdIncludingDeletedAsync`, so historical rows still show a name even if the type was later deleted.

---

### GET /api/v1/exports/dashboard-summary

**Purpose**: Export the dashboard summary (same data/filters as `/dashboard/summary`) as a PDF report.

**URL**: `/api/v1/exports/dashboard-summary`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`. Response: `Content-Type: application/pdf`, attachment, filename `dashboard-summary_{yyyyMMdd}.pdf` (date-only, not timestamped like the Excel exports).
**Authentication**: Policy `CanViewReports` (Admin, HR, Manager).

**Request**: Query params (`ExportDashboardSummaryQuery`):
| Param | Type | Required | Notes |
|---|---|---|---|
| `departmentId` | guid | optional | |
| `officeLocationId` | guid | optional | accepted but not enforced, same caveat as `/dashboard/summary` |
| `date` | date | optional | defaults to today (UTC) |

**Response**: `200 OK`, binary PDF stream containing the dashboard summary (totals, attendance, leave, per-department breakdown) rendered via `IPdfService.GenerateDashboardSummaryPdfAsync`.

**Validation** (`ExportDashboardSummaryQueryValidator`): `date` (if supplied) must not be in the future — `"Date cannot be in the future."` (same rule as `/dashboard/summary`).

**Error Codes**: `400` (future date, `VALIDATION_ERROR`), `401`, `403`, `500`.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  -o dashboard-summary.pdf \
  "https://api.example.com/api/v1/exports/dashboard-summary?date=2026-07-31"
```

**Best Practices**:
- Same access policy (`CanViewReports`) as Reports, distinct from `CanViewDashboard` used by `/dashboard/summary` itself — a Manager who can view the live dashboard can also export it as PDF.

---

### GET /api/v1/exports/reimbursements

**Purpose**: Export reimbursements to Excel. Non-Admin callers are always restricted to their own reimbursements regardless of any `employeeId` filter they pass.

**URL**: `/api/v1/exports/reimbursements`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`. Response: `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, attachment, filename `reimbursements_{yyyyMMddHHmmss}.xlsx`.
**Authentication**: Class-level `[Authorize]` only — **any authenticated user**, no role/policy restriction on this specific action (unlike the other six export endpoints). Access to *other people's* data is enforced entirely by handler-level scoping, not by `[Authorize]`.

**Request**: Query params (`ExportReimbursementsQuery`):
| Param | Type | Notes |
|---|---|---|
| `employeeId` | guid | optional; **silently ignored for non-Admin callers** — see below |
| `status` | string | optional; one of `Draft`, `Submitted`, `UnderReview`, `Approved`, `Rejected`, `ChangesRequested`, `Paid` (`ReimbursementStatus` enum) |

`requestingUserId` and `isPrivileged` also exist on the type but are set server-side by the controller from the JWT (`GetCurrentUserId()`, `IsAdmin()`) — never bound from the query string.

**Response**: `200 OK`, binary `.xlsx`. Columns: `Reimbursement Number, Employee Code, Employee Name, Expense Title, Expense Category, Expense Date, Amount, Currency, Status, Submitted At, Approved At, Payroll Processed, Payroll Date`.

**Scoping logic**: if the caller is not `Admin`, the handler ignores any `employeeId` supplied and instead resolves the caller's own `EmployeeId` from their user record, then exports only that employee's reimbursements (if the caller has no linked employee record, `Guid.Empty` is used, yielding an empty export rather than an error or someone else's data).

**Validation**: None — no validator registered.

**Error Codes**: `401` (no token — this is the only export endpoint reachable by any authenticated role), `500`.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  -o reimbursements.xlsx \
  "https://api.example.com/api/v1/exports/reimbursements?status=Approved"
```

**Best Practices**:
- Don't rely on `employeeId` to view someone else's reimbursements as a non-Admin — it's silently overridden, not rejected, so a caller can't tell from the response alone that their filter was ignored.

---

### GET /api/v1/exports/assets

**Purpose**: Export the asset register to Excel, including each asset's current assignee (if any).

**URL**: `/api/v1/exports/assets`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`. Response: `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, attachment, filename `assets_{yyyyMMddHHmmss}.xlsx`.
**Authentication**: Policy `CanManageAssets` (roles `Admin`, `HR`).

**Request**: Query params (`ExportAssetsQuery`), all optional:
| Param | Type | Notes |
|---|---|---|
| `status` | string | one of `Available`, `Assigned`, `UnderRepair`, `Retired`, `Lost` (`AssetStatus` enum) |
| `category` | string | |
| `search` | string | |

**Response**: `200 OK`, binary `.xlsx`. Columns: `Asset Tag, Category, Brand, Model, Serial Number, Purchase Date, Purchase Cost, Status, Currently Assigned To, Notes`. "Currently Assigned To" is resolved from active assignments and left blank if unassigned.

**Validation**: None — no validator registered. An invalid `status` string that doesn't match the `AssetStatus` enum fails ASP.NET Core model binding (default `400 application/problem+json`, not `ApiErrorResponse`) before the handler runs.

**Error Codes**: `400` (invalid `status` enum value — default model-binding shape, see Conventions above), `401`, `403` (not Admin/HR), `500`.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  -o assets.xlsx \
  "https://api.example.com/api/v1/exports/assets?status=Assigned&category=Laptop"
```

**Best Practices**:
- Validate `status` client-side against the known enum values before sending, since binding failures return the differently-shaped default ProblemDetails error rather than `ApiErrorResponse`.

---

### GET /api/v1/exports/candidates

**Purpose**: Export recruitment candidates to Excel.

**URL**: `/api/v1/exports/candidates`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`. Response: `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, attachment, filename `candidates_{yyyyMMddHHmmss}.xlsx`.
**Authentication**: Policy `CanManageRecruitment` (roles `Admin`, `HR`).

**Request**: Query params (`ExportCandidatesQuery`), all optional:
| Param | Type | Notes |
|---|---|---|
| `status` | string | one of `Applied`, `Screening`, `Interviewing`, `Offered`, `Hired`, `Rejected`, `Withdrawn` (`CandidateStatus` enum) |
| `designationId` | guid | |
| `search` | string | |

**Response**: `200 OK`, binary `.xlsx`. Columns: `Candidate Number, First Name, Last Name, Email, Phone Number, Designation, Department, Source, Applied Date, Status, Notes`.

**Validation**: None — no validator registered. Invalid `status` enum value fails model binding (default ProblemDetails `400`, not `ApiErrorResponse`).

**Error Codes**: `400` (bad enum), `401`, `403` (not Admin/HR), `500`.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  -o candidates.xlsx \
  "https://api.example.com/api/v1/exports/candidates?status=Interviewing"
```

**Best Practices**:
- Same enum-validation caveat as `/exports/assets` — validate `status` client-side.

---

## Audit Logs API

Controller: `EMS.API/Controllers/AuditLogsController.cs`. Route prefix: `api/v1/audit-logs`. Class-level `[Authorize]` (authenticated), with per-action policy/role overrides. This is the tenant-scoped audit log surface; the cross-company equivalent (`GET /platform/audit-logs`, Super Admin only) is out of scope here.

### GET /api/v1/audit-logs

**Purpose**: List/search audit log entries with filtering and pagination — for an admin audit trail screen.

**URL**: `/api/v1/audit-logs`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`.
**Authentication**: Policy `CanViewAuditLogs` — role `Admin` only.

**Request**: Query params (`GetAuditLogsQuery`), all optional except pagination defaults:
| Param | Type | Default | Notes |
|---|---|---|---|
| `userId` | guid | — | |
| `entityName` | string | — | |
| `entityId` | guid | — | |
| `action` | string | — | |
| `dateFrom` | date | — | |
| `dateTo` | date | — | |
| `page` | int | 1 | |
| `pageSize` | int | 20 | max 100 |

Automatically scoped server-side to the caller's own company (`ICurrentUserService.CompanyId`); a Super Admin (`CompanyId == null`) sees every company's logs.

**Response** (`200 OK`, `ApiResponse<PagedResult<AuditLogDto>>`):
```json
{
  "data": {
    "data": [
      {
        "id": "guid",
        "companyId": "guid",
        "userId": "guid",
        "entityName": "Employee",
        "entityId": "guid",
        "action": "Update",
        "oldValuesJson": "{...}",
        "newValuesJson": "{...}",
        "ipAddress": "203.0.113.5",
        "userAgent": "Mozilla/5.0 ...",
        "createdAtUtc": "2026-07-31T10:15:00Z"
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 143,
    "totalPages": 8
  },
  "message": "Request completed successfully.",
  "correlationId": "..."
}
```

**Validation** (`GetAuditLogsQueryValidator`):
- `page` must be `> 0`.
- `pageSize` must be in `[1, 100]`.
- `dateFrom` must be `<= dateTo` when both are supplied — `"dateFrom must be before or equal to dateTo."`.

Note: the handler itself also clamps `pageSize`/`page` defensively (`pageSize is > 0 and <= 100`, else 20; `page > 0`, else 1) as a second line of defense even though the validator should already reject out-of-range values.

**Error Codes**: `400` (bad pagination/date range, `VALIDATION_ERROR`), `401`, `403` (non-Admin), `500`.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  "https://api.example.com/api/v1/audit-logs?entityName=Employee&page=1&pageSize=20"
```

**Best Practices**:
- Use `entityName` + `entityId` together when auditing one record's full history — or better, use `GET /audit-logs/entity/{entityName}/{entityId}` below, which is purpose-built for that.
- `oldValuesJson`/`newValuesJson` are raw JSON strings, not nested objects — parse them client-side if you need structured diffing.

---

### GET /api/v1/audit-logs/{id}

**Purpose**: Fetch a single audit log entry by ID (e.g. drill-in from the list view).

**URL**: `/api/v1/audit-logs/{id}`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`.
**Authentication**: Policy `CanViewAuditLogs` — role `Admin` only.

**Request**: Path param `id` (guid, required). No query params, no body.

**Response** (`200 OK`, `ApiResponse<AuditLogDto>`): same `AuditLogDto` shape as the list endpoint's items.

**Validation**: None beyond route-model binding of `id` as a `guid`.

**Authorization/scoping**: a tenant Admin (non-null `CompanyId`) gets `404` if the log belongs to a different company — this is a deliberate "treated as not-found on mismatch" convention used elsewhere in this codebase (see project memory: 404-implies-owner pattern), not a `403`. A Super Admin (`CompanyId == null`) can fetch any company's log.

**Error Codes**: `401`, `403` (non-Admin role), `404` — either the ID truly doesn't exist, or it belongs to another company (both return a bare `404` with no body per `return NotFound();` — no `ApiErrorResponse` envelope here, unlike middleware-generated 404s), `500`.

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  https://api.example.com/api/v1/audit-logs/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Best Practices**:
- Treat `404` from this endpoint as "not visible to you," not necessarily "doesn't exist" — don't infer non-existence from it in cross-tenant contexts.

---

### GET /api/v1/audit-logs/entity/{entityName}/{entityId}

**Purpose**: Get the paginated audit history for one specific entity (e.g. "show me every change ever made to this Employee record").

**URL**: `/api/v1/audit-logs/entity/{entityName}/{entityId}`
**Method**: GET
**Headers**: `Authorization: Bearer <access_token>`.
**Authentication**: `[Authorize(Roles = "Admin,HR")]` — note this is a **role list**, not a named policy (unlike every other endpoint in this doc) — functionally equivalent to Admin-or-HR.

**Request**:
- Path params: `entityName` (string, required), `entityId` (guid, required).
- Query params: `page` (int, default 1), `pageSize` (int, default 20) — these have C#-level defaults on the action method itself, not a FluentValidation validator.

**Response** (`200 OK`, `ApiResponse<PagedResult<AuditLogDto>>`): same paged shape as `GET /audit-logs`, filtered to the one entity.

**Validation**: No FluentValidation validator for `GetAuditLogsForEntityQuery`. `page`/`pageSize` are clamped defensively in the handler the same way as the list endpoint (`pageSize is > 0 and <= 100` else 20, `page > 0` else 1) but out-of-range values are silently corrected rather than rejected with `400` — unlike `GET /audit-logs`, which validates and rejects them.

**Error Codes**: `401`, `403` (not Admin/HR), `500`. (No `404` — an unmatched `entityName`/`entityId` simply yields an empty page, `totalCount: 0`.)

**Examples**:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  "https://api.example.com/api/v1/audit-logs/entity/Employee/3fa85f64-5717-4562-b3fc-2c963f66afa6?page=1&pageSize=20"
```

**Best Practices**:
- Check `totalCount` rather than HTTP status to distinguish "no history" from an error — both return `200`.
- `entityName` is a free-text string match against however the auditing infrastructure recorded it (e.g. `"Employee"`) — get the exact casing/value from an existing `AuditLogDto.entityName` rather than guessing.

---

## Health API

Controller: `EMS.API/Controllers/HealthController.cs`. Route: `[Route("health")]` — **not versioned**, unlike every other controller in this doc (confirmed: no `api/v1` prefix). Class-level `[AllowAnonymous]` — all three endpoints are fully public, no JWT required. Intended for container orchestrators / uptime monitors / load balancers.

### GET /health

**Purpose**: Basic API health check — confirms the process is up and can serve HTTP requests at all. Typically used as a generic "is it alive" probe or root health check.

**URL**: `/health`
**Method**: GET
**Headers**: None required.
**Authentication**: Public (`[AllowAnonymous]`).
**Request**: No params, no body.

**Response** (`200 OK`, `ApiResponse<HealthStatusDto>`):
```json
{
  "data": { "status": "Healthy", "timestampUtc": "2026-08-01T09:00:00Z" },
  "message": "Request completed successfully.",
  "correlationId": "..."
}
```
This handler is synchronous and hardcodes `Status = "Healthy"` — it does **not** check the database or any dependency (contrast with `/health/ready` below).

**Validation**: None.

**Error Codes**: `500` only in the event of a truly unexpected crash — there is no failure path in the code as written (no dependency calls, can't return anything but `200 Healthy`).

**Examples**:
```bash
curl https://api.example.com/health
```

**Best Practices**:
- Use this only to confirm the process/HTTP pipeline is up — it says nothing about database connectivity. Use `/health/ready` for that.

---

### GET /health/live

**Purpose**: Liveness probe for container orchestrators (e.g. Kubernetes/Docker) — confirms the process itself hasn't deadlocked or crashed, distinct from readiness to serve real traffic.

**URL**: `/health/live`
**Method**: GET
**Headers**: None required.
**Authentication**: Public (`[AllowAnonymous]`).
**Request**: No params, no body.

**Response** (`200 OK`, `ApiResponse<HealthStatusDto>`): identical shape/behavior to `GET /health` — same hardcoded `"Healthy"`, no dependency checks. As written, this endpoint's implementation is functionally identical to `GET /health` (both just construct a `HealthStatusDto` with `Status = "Healthy"` and the current UTC timestamp); the distinction is purely semantic/organizational for orchestrator wiring, not a code-level difference today.

**Validation**: None.

**Error Codes**: None expected in normal operation; `500` only on an unexpected crash.

**Examples**:
```bash
curl https://api.example.com/health/live
```

**Best Practices**:
- Wire this to your orchestrator's liveness probe (restart-on-failure), and use `/health/ready` for the readiness probe (remove-from-load-balancer-on-failure) — don't use the same endpoint for both if you want them to diverge in the future.

---

### GET /health/ready

**Purpose**: Readiness probe — confirms the API can actually serve traffic, specifically including database connectivity. This is the one Health endpoint that does real work.

**URL**: `/health/ready`
**Method**: GET
**Headers**: None required.
**Authentication**: Public (`[AllowAnonymous]`).
**Request**: No params, no body.

**Response**:
- `200 OK` when the database is reachable (`ApiResponse<ReadinessStatusDto>`):
```json
{
  "data": { "status": "Healthy", "databaseConnected": true, "timestampUtc": "2026-08-01T09:00:00Z" },
  "message": "Request completed successfully.",
  "correlationId": "..."
}
```
- `503 Service Unavailable` when the database is not reachable — **still wrapped in the success envelope** (`ApiResponse<ReadinessStatusDto>.Success(...)`, not `ApiErrorResponse`), just with a different top-level HTTP status and `message: "Service is not ready."`:
```json
{
  "data": { "status": "Unhealthy", "databaseConnected": false, "timestampUtc": "2026-08-01T09:00:00Z" },
  "message": "Service is not ready.",
  "correlationId": "..."
}
```

**Validation**: None.

**Error Codes**: `503` is the only "failure" status this endpoint returns, and it's a deliberate readiness signal, not an exception — `GetReadinessQueryHandler` catches nothing; `IHealthCheckRepository.CanConnectToDatabaseAsync` is expected to return `false` rather than throw on a down database. A genuinely unexpected exception (e.g. the repository itself throwing) would still fall through to the global `500 INTERNAL_ERROR` via `ExceptionHandlingMiddleware`.

**Examples**:
```bash
curl -i https://api.example.com/health/ready
```
Healthy response: `HTTP/1.1 200 OK` with `databaseConnected: true`. Unhealthy: `HTTP/1.1 503 Service Unavailable` with `databaseConnected: false`.

**Best Practices**:
- Check the HTTP status code (`200` vs `503`), not just `data.status`, since both live under the same success envelope shape — don't assume `200` implies healthy without checking, and don't assume a non-`200` means a hard error (it's an expected readiness signal here).
- A failed check is logged server-side at `LogError` level (`"Readiness check failed: database is not reachable."`) — useful to know when correlating orchestrator restarts with server logs.
