# Frontend Architecture - React Native & Web

## Overview

The frontend has been restructured to support both **mobile** (iOS/Android) and **web** platforms using a monorepo architecture. This enables code sharing between platforms while maintaining platform-specific optimizations.

## Project Structure

```
frontend/
├── package.json                 # Root workspace configuration
├── shared/                      # Shared code library
│   ├── src/
│   │   ├── types.ts            # Common TypeScript types and schemas
│   │   ├── errors.ts           # Error classes and definitions
│   │   ├── httpClient.ts       # Shared HTTP client with auth interceptors
│   │   ├── hooks.ts            # Platform-independent hooks utilities
│   │   ├── utils.ts            # Shared utility functions
│   │   └── index.ts            # Main export file
│   ├── package.json            # Shared package configuration
│   └── tsconfig.json           # TypeScript configuration
│
├── web/                         # React web application (Vite)
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/           # Core infrastructure (auth, routes, API)
│   │   │   ├── features/       # Feature modules (employees, dashboard, etc.)
│   │   │   └── shared/         # Shared web components and utilities
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── index.html
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
│
├── mobile/                      # React Native app (Expo)
│   ├── app/
│   │   ├── (auth)/             # Authentication screens (login, forgot password)
│   │   ├── (app)/              # Main authenticated app (dashboard, employees)
│   │   ├── _layout.tsx         # Root layout and authentication router
│   │   └── ...
│   ├── src/
│   │   ├── context/            # React Context (Auth)
│   │   ├── components/         # Reusable components
│   │   ├── features/           # Feature screens
│   │   └── types/              # Type definitions
│   ├── assets/                 # Icons, images, splash screen
│   ├── app.json               # Expo configuration
│   ├── package.json
│   ├── tsconfig.json
│   └── index.js               # Entry point
│
└── README.md
```

## Technology Stack

### Shared Library (`@ems/shared`)
- **TypeScript** - Type-safe code across platforms
- **Axios** - HTTP client with auth interceptors
- **Zod** - Runtime schema validation
- **Platform-independent** - No React-specific dependencies

### Web App
- **React 19+** - Latest React with hooks
- **Vite** - Fast development server and build tool
- **TypeScript** - Full type safety
- **Tailwind CSS v4** - Utility-first CSS framework
- **React Router v7** - Client-side routing
- **React Hook Form + Zod** - Form management and validation
- **Axios** - Own HTTP client (`core/api/httpClient.ts`), not yet migrated onto `@ems/shared`
- **Mock/live repository pattern** - Each feature exposes a repository interface with a `mock*Repository` and an `api*Repository`; `VITE_DATA_SOURCE` picks which one backs the app (see `web/README.md`)

### Mobile App
- **React Native 0.76+** - Cross-platform mobile framework
- **Expo 52+** - Managed React Native platform
- **Expo Router** - File-based routing for React Native
- **TypeScript** - Full type safety
- **React Native Navigation** - Tab and stack navigation
- **React Hook Form + Zod** - Form management and validation
- **AsyncStorage** - Cross-platform storage for tokens
- **React Native Gesture Handler** - Native gesture handling

## Monorepo Workspace Structure

The project uses npm workspaces to manage dependencies and facilitate code sharing:

```json
{
  "workspaces": [
    "web",
    "mobile",
    "shared"
  ]
}
```

### Benefits
- **Single dependency install** - All packages installed from root
- **Shared code** - Common logic in `@ems/shared` imported by both apps
- **Unified tooling** - Consistent TypeScript, ESLint, testing setup
- **Simplified development** - One repository for web and mobile

## Authentication Flow

The two platforms currently run **parallel, independent** auth implementations — they are not yet unified on `@ems/shared`:

- **Mobile** (`mobile/src/context/AuthContext.tsx`) uses `@ems/shared`'s `createHttpClient()`, `LoginCredentialsSchema`, and `AsyncStorage` for token persistence.
- **Web** (`web/src/app/core/auth/`, `web/src/app/core/api/httpClient.ts`) has its own axios client, its own auth context, and its own `localStorage` token storage. It does not import `@ems/shared`.

Both follow the same conceptual flow (login → JWT issued → token attached via interceptor → refresh on 401 → logout on session expiry); the code just isn't shared between them yet. Unifying web onto `@ems/shared` is a deliberate future step, not something already done.

## API Integration

**Mobile** calls go through the shared HTTP client:

```typescript
createHttpClient(baseURL, tokenStorage, callbacks)
```

**Web** calls go through its own local repository pattern per feature — a `*Repository` interface with a `mock*Repository` and an `api*Repository` implementation, selected by `VITE_DATA_SOURCE` via `web/src/app/core/config/selectRepository.ts`. See `web/README.md` for details.

## Code Sharing Strategy

### Shared today (mobile only)
- `@ems/shared` (types, HTTP client, error classes, utilities) is consumed by **mobile**.
- **Web** does not consume `@ems/shared` yet — it has its own equivalent code, kept separate deliberately so its mock/live repository pattern isn't disturbed. Generalizing that pattern into `@ems/shared` is future work, once mobile has real feature screens to justify the shared abstraction.

### Platform-Specific
- UI components (Tailwind for web, React Native for mobile)
- Navigation (React Router for web, Expo Router for mobile)
- Storage (localStorage for web, AsyncStorage for mobile)
- Platform-specific features (camera, location, etc.)

## Feature Implementation Pattern

When adding a feature, follow this structure:

```
features/
├── auth/                    # Example feature
│   ├── api/                # API client (might go in shared if used by both)
│   │   └── authRepository.ts
│   ├── components/         # Feature components
│   ├── hooks/             # Feature hooks
│   ├── pages/ OR screens/ # Pages (web) or screens (mobile)
│   └── types/             # Feature types
```

## Development Workflow

### Web Development
```bash
cd frontend
npm install                 # Install all workspace packages
npm run dev:web            # Start Vite dev server on http://localhost:5173
npm run build:web          # Build for production
npm run lint               # Lint all packages
npm run type-check         # Type check all packages
```

### Mobile Development
```bash
cd frontend
npm install                 # Install all workspace packages
npm run dev:mobile         # Start Expo dev server
npm run android            # Launch Android emulator
npm run ios                # Launch iOS simulator
npm run build:mobile       # Build for production
```

## Environment Configuration

### Web App (`.env` in `web/` folder)
```
VITE_API_URL=http://localhost:5000/api/v1
```

### Mobile App (`.env` in `mobile/` folder)
```
EXPO_PUBLIC_API_URL=http://localhost:5000/api/v1
```

## Testing Strategy

- **Unit Tests** - For shared library functions
- **Integration Tests** - For API clients and context
- **E2E Tests** - Playwright for web, Detox for mobile (future)
- **Component Tests** - React Testing Library for web, React Native Testing Library for mobile

## Performance Considerations

1. **Code Splitting** - Lazy load features in web app
2. **Tree Shaking** - Remove unused code in builds
3. **Bundle Size** - Monitor with Vite visualizer
4. **Image Optimization** - Use modern formats (WebP)
5. **API Caching** - Implement request deduplication
6. **Mobile Bundle** - Use Expo's managed build service

## Deployment Strategy

### Web
- Deploy to Vercel, Netlify, or static hosting
- Environment variables via build-time or runtime config

### Mobile
- iOS: Distribute via App Store (using EAS Build)
- Android: Distribute via Google Play Store (using EAS Build)
- Testing: Use TestFlight (iOS) and Internal Testing (Android)

## Migration Path from Original React Web App

The original standalone web app was relocated as-is from `frontend/web-app/` into `frontend/web/` so it lives at the path the root workspace already expected. Its code was **not** refactored onto `@ems/shared` in that move — it kept its own httpClient, auth context, and mock/live repository pattern per feature. Adopting `@ems/shared` in web (replacing its local httpClient/auth code) is a separate, not-yet-started task.

## Next Steps

- [ ] Complete mobile UI implementation (dashboard, employees, leave, etc.)
- [ ] Implement mobile-specific features (camera for document upload, push notifications)
- [ ] Add E2E tests with Detox
- [ ] Set up CI/CD for both web and mobile
- [ ] Configure EAS Build for mobile app signing
- [ ] Implement analytics and error tracking (Sentry)
- [ ] Add offline support for mobile app
