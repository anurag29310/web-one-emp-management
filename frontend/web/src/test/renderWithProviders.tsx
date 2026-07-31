import type { ReactElement } from 'react'
import { render } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '@/app/core/auth/authContextType'
import type { AuthenticatedUser, Role } from '@/app/shared/models/user'

function buildUser(role: Role): AuthenticatedUser {
  return {
    id: '90000000-0000-0000-0000-000000000001',
    userName: 'test.user',
    email: 'test.user@example.com',
    role,
    isActive: true,
    isMfaEnabled: false,
  }
}

export function buildAuthContextValue(role: Role | null): AuthContextValue {
  return {
    user: role ? buildUser(role) : null,
    isAuthenticated: role !== null,
    isInitializing: false,
    login: vi.fn(),
    completeMfaLogin: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
  }
}

interface RenderWithProvidersOptions {
  role?: Role | null
  route?: string
  /** A router path pattern (e.g. "/assets/:id") to resolve `route` against — only needed for pages that read useParams(). */
  path?: string
}

export function renderWithProviders(
  ui: ReactElement,
  { role = 'Admin', route = '/', path }: RenderWithProvidersOptions = {},
) {
  const content = path ? (
    <Routes>
      <Route path={path} element={ui} />
    </Routes>
  ) : (
    ui
  )

  return render(
    <MemoryRouter initialEntries={[route]}>
      <AuthContext.Provider value={buildAuthContextValue(role)}>{content}</AuthContext.Provider>
    </MemoryRouter>,
  )
}
