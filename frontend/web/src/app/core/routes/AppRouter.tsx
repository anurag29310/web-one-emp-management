import { lazy, Suspense } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './ProtectedRoute'
import { AppLayout } from '@/app/core/layout/AppLayout'

const LoginPage = lazy(() =>
  import('@/app/features/auth/pages/LoginPage').then((m) => ({ default: m.LoginPage })),
)
const DashboardPage = lazy(() =>
  import('@/app/features/dashboard/pages/DashboardPage').then((m) => ({ default: m.DashboardPage })),
)
const EmployeeListPage = lazy(() =>
  import('@/app/features/employees/pages/EmployeeListPage').then((m) => ({
    default: m.EmployeeListPage,
  })),
)
const EmployeeProfilePage = lazy(() =>
  import('@/app/features/employees/pages/EmployeeProfilePage').then((m) => ({
    default: m.EmployeeProfilePage,
  })),
)
const DepartmentListPage = lazy(() =>
  import('@/app/features/departments/pages/DepartmentListPage').then((m) => ({
    default: m.DepartmentListPage,
  })),
)
const DepartmentDetailPage = lazy(() =>
  import('@/app/features/departments/pages/DepartmentDetailPage').then((m) => ({
    default: m.DepartmentDetailPage,
  })),
)
const LeaveListPage = lazy(() =>
  import('@/app/features/leave/pages/LeaveListPage').then((m) => ({ default: m.LeaveListPage })),
)

function PageFallback() {
  return <div className="min-h-screen bg-canvas p-6 text-sm text-ink-subtle">Loading…</div>
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <Suspense fallback={<PageFallback />}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />

          <Route element={<ProtectedRoute />}>
            <Route element={<AppLayout />}>
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/employees" element={<EmployeeListPage />} />
              <Route path="/employees/:id" element={<EmployeeProfilePage />} />
              <Route path="/departments" element={<DepartmentListPage />} />
              <Route path="/departments/:id" element={<DepartmentDetailPage />} />
              <Route path="/leave" element={<LeaveListPage />} />
            </Route>
          </Route>

          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  )
}
