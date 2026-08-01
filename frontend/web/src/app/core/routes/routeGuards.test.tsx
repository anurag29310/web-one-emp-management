import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthContext } from '@/app/core/auth/authContextType'
import { buildAuthContextValue } from '@/test/renderWithProviders'
import type { Role } from '@/app/shared/models/user'
import { ProtectedRoute } from './ProtectedRoute'
import { PlatformProtectedRoute } from './PlatformProtectedRoute'

function renderGuardedApp(role: Role, initialRoute: string) {
  return render(
    <MemoryRouter initialEntries={[initialRoute]}>
      <AuthContext.Provider value={buildAuthContextValue(role)}>
        <Routes>
          <Route element={<PlatformProtectedRoute />}>
            <Route path="/platform/dashboard" element={<div>Platform Dashboard</div>} />
          </Route>
          <Route element={<ProtectedRoute />}>
            <Route path="/dashboard" element={<div>Tenant Dashboard</div>} />
          </Route>
        </Routes>
      </AuthContext.Provider>
    </MemoryRouter>,
  )
}

describe('route guards', () => {
  it('redirects a non-SuperAdmin away from /platform/dashboard to /dashboard', () => {
    renderGuardedApp('Admin', '/platform/dashboard')

    expect(screen.getByText('Tenant Dashboard')).toBeInTheDocument()
    expect(screen.queryByText('Platform Dashboard')).not.toBeInTheDocument()
  })

  it('redirects a SuperAdmin away from /dashboard to /platform/dashboard', () => {
    renderGuardedApp('SuperAdmin', '/dashboard')

    expect(screen.getByText('Platform Dashboard')).toBeInTheDocument()
    expect(screen.queryByText('Tenant Dashboard')).not.toBeInTheDocument()
  })

  it('lets a SuperAdmin reach /platform/dashboard directly', () => {
    renderGuardedApp('SuperAdmin', '/platform/dashboard')

    expect(screen.getByText('Platform Dashboard')).toBeInTheDocument()
  })

  it('lets a tenant Admin reach /dashboard directly', () => {
    renderGuardedApp('Admin', '/dashboard')

    expect(screen.getByText('Tenant Dashboard')).toBeInTheDocument()
  })
})
