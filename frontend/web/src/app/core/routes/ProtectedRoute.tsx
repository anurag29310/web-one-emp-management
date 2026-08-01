import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '@/app/core/auth/useAuth'

export function ProtectedRoute() {
  const { user, isAuthenticated, isInitializing } = useAuth()
  const location = useLocation()

  if (isInitializing) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-canvas text-sm text-ink-subtle">
        Loading…
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  // A Super Admin's JWT carries no company_id and never touches tenant HR data — it belongs in
  // the separate /platform/* surface (see PlatformProtectedRoute), not here.
  if (user?.role === 'SuperAdmin') {
    return <Navigate to="/platform/dashboard" replace />
  }

  return <Outlet />
}
