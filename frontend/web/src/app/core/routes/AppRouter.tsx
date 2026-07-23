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
const MyAttendancePage = lazy(() =>
  import('@/app/features/attendance/pages/MyAttendancePage').then((m) => ({
    default: m.MyAttendancePage,
  })),
)
const AttendanceRecordsPage = lazy(() =>
  import('@/app/features/attendance/pages/AttendanceRecordsPage').then((m) => ({
    default: m.AttendanceRecordsPage,
  })),
)
const AttendanceCorrectionsPage = lazy(() =>
  import('@/app/features/attendance/pages/AttendanceCorrectionsPage').then((m) => ({
    default: m.AttendanceCorrectionsPage,
  })),
)
const ShiftListPage = lazy(() =>
  import('@/app/features/shifts/pages/ShiftListPage').then((m) => ({ default: m.ShiftListPage })),
)
const ShiftAssignmentsPage = lazy(() =>
  import('@/app/features/shifts/pages/ShiftAssignmentsPage').then((m) => ({
    default: m.ShiftAssignmentsPage,
  })),
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
              <Route path="/attendance" element={<MyAttendancePage />} />
              <Route path="/attendance/records" element={<AttendanceRecordsPage />} />
              <Route path="/attendance/corrections" element={<AttendanceCorrectionsPage />} />
              <Route path="/shifts" element={<ShiftListPage />} />
              <Route path="/shifts/assignments" element={<ShiftAssignmentsPage />} />
            </Route>
          </Route>

          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  )
}
