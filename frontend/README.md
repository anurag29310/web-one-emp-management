# Frontend - Web & Mobile Monorepo

This directory contains the Employee Management System frontend applications for both web and mobile platforms.

## Directory Structure

```
frontend/
├── shared/          # Shared code library (@ems/shared)
│   ├── src/
│   │   ├── types.ts         # All TypeScript types and Zod schemas
│   │   ├── errors.ts        # Error classes
│   │   ├── httpClient.ts    # Shared HTTP client with auth
│   │   ├── hooks.ts         # Platform-independent hook utilities
│   │   ├── utils.ts         # Utility functions
│   │   └── index.ts         # Main export
│   └── package.json
│
├── web/             # React web application
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/        # Authentication, routing, API client
│   │   │   ├── features/    # Feature modules
│   │   │   └── shared/      # Web-specific shared components
│   │   └── main.tsx
│   ├── index.html
│   ├── vite.config.ts
│   └── package.json
│
├── mobile/          # React Native mobile application
│   ├── app/
│   │   ├── (auth)/          # Authentication screens
│   │   └── (app)/           # Main app screens
│   ├── src/
│   │   ├── context/         # Auth context
│   │   ├── components/      # Reusable components
│   │   └── features/        # Feature screens
│   ├── app.json             # Expo configuration
│   └── package.json
│
└── package.json             # Root workspace configuration
```

## Quick Start

### Prerequisites

- Node.js >= 18
- npm or pnpm

### Web Development

```bash
cd frontend

# Install all dependencies
npm install

# Start development server
npm run dev:web

# Build for production
npm run build:web

# Type check
npm run typecheck

# Lint
npm run lint
```

Web app will run at `http://localhost:5173`

### Mobile Development

```bash
cd frontend

# Install all dependencies
npm install

# Start Expo dev server
npm run dev:mobile

# In another terminal:
npm run android    # Launch Android Emulator
npm run ios        # Launch iOS Simulator (macOS only)
npm run web        # Launch web preview
```

## Project Structure

### `@ems/shared` - Shared Library

Contains all platform-independent code:

**Types & Validation** (`types.ts`)
```typescript
// All shared types with Zod validation
- AuthenticatedUser
- LoginCredentials
- Session
- Employee
- Department
- Leave
- API response types
```

**HTTP Client** (`httpClient.ts`)
```typescript
// Configured axios instance with:
- JWT authentication
- Token refresh logic
- Error transformation
- Request/response interceptors
```

**Error Handling** (`errors.ts`)
```typescript
- AppError
- ValidationError
- AuthenticationError
- AuthorizationError
- NotFoundError
```

**Utilities** (`utils.ts`)
```typescript
- Date formatting
- String validation
- Array operations
- Data transformations
```

### `web` - React Web Application

**Features**
- React 19 with hooks
- Vite for fast development
- Tailwind CSS v4 for styling
- React Router for navigation
- React Hook Form for forms
- Full TypeScript support

**Structure**
```
web/src/app/
├── core/
│   ├── api/         # HTTP client setup
│   ├── auth/        # Auth context & hooks
│   ├── config/      # Configuration
│   ├── layout/      # App layout
│   └── routes/      # Route definitions
├── features/        # Feature modules
└── shared/          # Web components & hooks
```

**Development Commands**
```bash
npm run dev          # Start dev server
npm run build        # Production build
npm run typecheck    # Type checking
npm run lint         # Linting
npm run preview      # Preview production build
```

### `mobile` - React Native Application

**Features**
- React Native 0.76+
- Expo managed platform
- Expo Router for file-based routing
- Bottom tab navigation
- Cross-platform (iOS, Android, Web)
- AsyncStorage for persistence

**Structure**
```
mobile/
├── app/
│   ├── (auth)/      # Login screens
│   ├── (app)/       # Authenticated screens
│   └── _layout.tsx  # Root layout & routing
├── src/
│   ├── context/     # Auth context
│   ├── components/  # Reusable components
│   ├── features/    # Feature screens
│   └── types/       # Type definitions
└── assets/          # Icons, images, splash
```

**Development Commands**
```bash
npm run start        # Start Expo dev server
npm run android      # Android Emulator/Device
npm run ios          # iOS Simulator
npm run web          # Web Preview
npm run build        # Production build (EAS)
```

## Using the Shared Library

### In Web App

```typescript
import { 
  createHttpClient,
  LoginCredentialsSchema,
  AppError,
  formatDateDisplay
} from '@ems/shared'

// Types come with full validation
import type { AuthenticatedUser, Employee } from '@ems/shared'
```

### In Mobile App

```typescript
import {
  createHttpClient,
  LoginCredentialsSchema,
  useAuth
} from '@ems/shared'
```

## Authentication Flow

1. User enters credentials on login screen
2. Credentials validated against Zod schema
3. API request via shared HTTP client
4. Tokens stored in platform-specific storage
5. Subsequent requests include token in Authorization header
6. Automatic token refresh on 401 response
7. Session expiration triggers logout

## API Integration

All API calls use the shared HTTP client:

**Web Implementation**
```typescript
const httpClient = createHttpClient(
  'http://localhost:5000/api/v1',
  new LocalStorageTokenStorage(),  // Browser localStorage
  callbacks
)
```

**Mobile Implementation**
```typescript
const httpClient = createHttpClient(
  'http://localhost:5000/api/v1',
  new AsyncStorageTokenStorage(),  // React Native AsyncStorage
  callbacks
)
```

## Code Sharing Guidelines

### Share in `@ems/shared`
- ✅ All API types and schemas
- ✅ HTTP client and auth logic
- ✅ Error classes
- ✅ Business logic utilities
- ✅ Data validation

### Keep Platform-Specific
- ❌ UI Components (different toolkits)
- ❌ Navigation (different frameworks)
- ❌ Storage implementation (different APIs)
- ❌ Platform-specific features (camera, location, etc.)

## Environment Setup

### Web (`web/.env`)
```
VITE_API_URL=http://localhost:5000/api/v1
```

### Mobile (`mobile/.env`)
```
EXPO_PUBLIC_API_URL=http://localhost:5000/api/v1
```

## Build & Deploy

### Web
```bash
npm run build:web
# Output in web/dist/
# Deploy to Vercel, Netlify, or any static host
```

### Mobile
```bash
npm run build:mobile
# Uses EAS Build for signing
# Outputs APK (Android) and IPA (iOS)
```

## Monorepo Commands

From `frontend/` directory:

```bash
npm install              # Install all workspace packages
npm run dev:web         # Start web dev
npm run dev:mobile      # Start mobile dev
npm run build:web       # Build web
npm run build:mobile    # Build mobile
npm run lint            # Lint all packages
npm run type-check      # Type check all packages
```

## Testing

### Shared Library
```bash
cd shared
npm test
```

### Web
```bash
cd web
npm test
# Uses Vitest + React Testing Library
```

### Mobile
```bash
cd mobile
npm test
# Uses Jest + React Native Testing Library
```

## Troubleshooting

### Dependencies not installing
```bash
cd frontend
npm install
npm install -w @ems/shared
npm install -w web
npm install -w mobile
```

### Port conflicts
- Web: Change in `vite.config.ts`
- Mobile: Expo will prompt for alternative port if 8081 is in use

### TypeScript errors
```bash
npm run type-check
# Run in specific package
cd web && npm run typecheck
```

## Performance Tips

- **Web**: Use React.lazy for code splitting
- **Mobile**: Implement image optimization with expo-image
- **Shared**: Keep library size minimal, no unnecessary dependencies
- **Monorepo**: Run builds in parallel when possible

## Related Documentation

- [ARCHITECTURE_MOBILE.md](../docs/ARCHITECTURE_MOBILE.md) - Detailed frontend architecture
- [API Specification](../docs/api-specification.md) - API endpoints and contracts
- [DESIGN.md](../DESIGN.md) - UI/UX design guidelines
