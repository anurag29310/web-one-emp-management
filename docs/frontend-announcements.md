# Frontend Requirements — Announcements

This document exists because the "Announcements" feature (company-wide broadcast messages, distinct from personal `Notifications`) needs frontend requirements that go beyond what `docs/api-specification.md` covers — component/UX expectations, not just the wire contract. No dedicated frontend-requirements doc convention existed in this repo before this file; it is being introduced deliberately for this feature and can be used as a template for future features that need the same treatment.

The backend for this feature is fully implemented (entity, CQRS, controller, tests) as of this writing. **No frontend code has been written yet** — this document is the spec for whoever picks that up next.

## 1. What this feature is

- **Announcements**: company-wide broadcast messages created by Admin/HR. Default audience is everyone; an announcement can optionally be scoped to one department or one role. Supports a `priority` (`Normal` / `Important` / `Critical`), an optional expiry, and per-user read tracking.
- **Notifications** (already exists conceptually, but also has no frontend yet): personal, per-user messages (e.g. "your leave was approved"). Different backend table, different endpoints, different mental model — do not conflate the two in the UI. A reasonable UI split is a personal bell/notification dropdown for `Notifications` and a company-wide banner/list for `Announcements`, but that's a UX call for whoever implements this, not a mandated layout.

## 2. API contract

Full contract: [api-specification.md §19](api-specification.md#19-notification-and-announcement-apis-phase-2). Summary for Announcements:

| Method | Endpoint | Access | Notes |
| --- | --- | --- | --- |
| `POST` | `/announcements` | Admin/HR | Create/broadcast |
| `GET` | `/announcements` | Any authenticated user | Visible-to-me list, paginated, `onlyUnread` filter |
| `GET` | `/announcements/{id}` | Any authenticated user | 404 if not visible/expired/retracted |
| `POST` | `/announcements/{id}/mark-read` | Any authenticated user | |
| `DELETE` | `/announcements/{id}` | Admin/HR | Retract |

There is no update/edit endpoint — don't build an edit UI; retract-and-recreate is the only supported flow.

Visibility filtering (department/role scoping, expiry, retraction) happens **server-side** based on the caller's JWT — the frontend never needs to compute or duplicate that logic, just render whatever the list endpoint returns.

## 3. TypeScript types

Follow this repo's established convention (confirmed in `features/employees`, `features/leave`, etc.): hand-mirror the backend DTO, PascalCase → camelCase, `Guid` → `string`, `DateTime`/`DateTime?` → `string` / `string | null`.

```ts
// frontend/web/src/app/features/announcements/api/announcementTypes.ts
export type AnnouncementPriority = 'Normal' | 'Important' | 'Critical'
export type AnnouncementAudienceType = 'All' | 'Department' | 'Role'

export interface Announcement {
  id: string
  title: string
  message: string
  priority: AnnouncementPriority
  audienceType: AnnouncementAudienceType
  departmentId: string | null
  targetRole: string | null
  createdByUserId: string
  createdAtUtc: string
  expiresAtUtc: string | null
  isReadByMe: boolean
}

export interface CreateAnnouncementInput {
  title: string
  message: string
  priority: AnnouncementPriority
  audienceType: AnnouncementAudienceType
  departmentId?: string | null
  targetRole?: string | null
  expiresAtUtc?: string | null
}
```

Place these next to the repository contract (`features/announcements/api/`), not in `shared/models/`, following the `features/leave/api/leaveRepository.ts` pattern (feature-local types) rather than the `shared/models/employee.ts` pattern (cross-feature types) — Announcements aren't referenced from other features today.

## 4. Feature module layout

Follow the existing `features/<name>/{api,components,hooks,pages,types}` structure exactly (see `frontend/web/src/app/features/employees/` as the most complete reference implementation):

```
frontend/web/src/app/features/announcements/
  api/
    announcementRepository.ts       # interface (contract) — list/getById/create/markRead/retract
    apiAnnouncementRepository.ts    # real Axios-backed implementation
    mockAnnouncementRepository.ts   # in-memory mock
    mockData.ts                     # fixture announcements
    announcementTypes.ts            # types shown above
    index.ts                        # selectRepository({ mock, api }) — see features/employees/api/index.ts
  hooks/
    useAnnouncements.ts             # list hook, hand-rolled useState/useEffect + cancelled flag + refresh()
  components/
    AnnouncementBanner.tsx          # new — see §6
    AnnouncementPriorityBadge.tsx   # new — see §6
  pages/
    AnnouncementsPage.tsx
```

Wire the repository switch through `selectRepository()` (`frontend/web/src/app/core/config/selectRepository.ts`), exactly as `features/employees/api/index.ts` does — this repo has no other DI mechanism for mock-vs-real data, don't invent one.

## 5. Data fetching

This repo does **not** use React Query, Redux, Zustand, or SWR (confirmed via `frontend/web/package.json`). Data fetching is hand-rolled hooks: `useState`/`useEffect` with a `cancelled` flag to avoid race conditions on unmount, catching the shared `AppError` type. Follow `frontend/web/src/app/features/employees/hooks/useEmployees.ts` as the template. If you want a manual refresh (e.g. after marking read), expose a `refresh()` that bumps an internal `refreshToken` counter, following `features/leave`'s `useLeaveRequests` hook.

Since delivery is poll-based (see [architecture.md §8.6](architecture.md#86-notification-and-announcement-delivery) — there is no SignalR/push in this system), if you want a "live-ish" unread badge, poll `GET /announcements?onlyUnread=true` on an interval from the hook. Don't build a WebSocket/SignalR client — that would be new infrastructure this repo doesn't have, introduced silently.

## 6. UI components

No bell/toast/badge/banner component exists yet anywhere in the frontend. `frontend/web/src/app/shared/components/` currently has three primitives, all plain function components styled with Tailwind utility classes and CSS custom-property tokens (`bg-canvas`, `bg-surface-1/2`, `text-ink`/`text-ink-subtle`, `border-hairline`, `text-success/warning/danger`, etc.) — no component library (no MUI/Radix/shadcn):

- **`Modal.tsx`**: portal-based dialog (`createPortal`, Escape-to-close, body-scroll lock, `role="dialog" aria-modal`). Reuse this pattern for an announcement detail view rather than building a new dialog primitive.
- **`StatusBadge.tsx`**: pill badge driven by a `STATUS_STYLES` lookup keyed by a status string. Reuse this exact pattern for `AnnouncementPriorityBadge` (`Normal`/`Important`/`Critical` → three lookup entries), not a new badge abstraction.
- **`Avatar.tsx`**: not directly relevant here, but same styling convention.

Build `AnnouncementBanner`/`AnnouncementPriorityBadge` as new additions to `shared/components/` in this same style — plain component + Tailwind classes + the existing design tokens — not a new UI framework.

## 7. Role-based visibility in the UI

There is **no route-level role guard** in this codebase. `frontend/web/src/app/core/routes/ProtectedRoute.tsx` only checks authentication (`isAuthenticated`), not role. Per-page "can this user manage X" gating is done ad hoc inside page components, e.g. `frontend/web/src/app/features/departments/pages/DepartmentListPage.tsx`:

```ts
const canManage = user?.role === 'Admin' || user?.role === 'HR'
```

Follow this same component-level pattern to show/hide the "compose announcement" UI (`canManage` gates a create button/page, not a router guard). `Role` is typed as `'Admin' | 'HR' | 'Manager' | 'Employee'` in `frontend/web/src/app/shared/models/user.ts`. Do not introduce a `<RequireRole>` router guard as part of this feature — that would be new routing infrastructure, out of scope for an Announcements feature.

## 8. A note on DESIGN.md

`DESIGN.md` at the repo root currently contains an unrelated Linear.app design-system analysis (colors/typography/spacing for a Linear.app clone), not this project's actual design spec, even though root `CLAUDE.md` cites it as the frontend styling reference. Don't use it as a literal source of truth for Announcements styling — use the existing `shared/components/` Tailwind-token conventions (§6 above) as the real reference instead. This mismatch predates this feature and is called out here so it doesn't cause confusion; fixing `DESIGN.md` itself is a separate concern outside this feature's scope.

## 9. Out of scope for this feature

- No update/edit endpoint or UI (retract-and-recreate only).
- No real-time push (SignalR/WebSockets) — poll-based only.
- No route-level role guard — component-level checks only, per existing convention.
- No React Query/global state library migration — hand-rolled hooks only, per existing convention.
