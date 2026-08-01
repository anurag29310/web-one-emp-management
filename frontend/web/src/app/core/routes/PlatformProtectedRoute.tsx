import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '@/app/core/auth/useAuth'

export function PlatformProtectedRoute() {
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

  if (user?.role !== 'SuperAdmin') {
    return <Navigate to="/dashboard" replace />
  }

  return <Outlet />
}
