import { lazy, Suspense } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './ProtectedRoute'
import { PlatformProtectedRoute } from './PlatformProtectedRoute'
import { AppLayout } from '@/app/core/layout/AppLayout'
import { PlatformLayout } from '@/app/core/layout/PlatformLayout'

const LoginPage = lazy(() =>
  import('@/app/features/auth/pages/LoginPage').then((m) => ({ default: m.LoginPage })),
)
const MfaChallengePage = lazy(() =>
  import('@/app/features/auth/pages/MfaChallengePage').then((m) => ({ default: m.MfaChallengePage })),
)
const CompanyRegisterPage = lazy(() =>
  import('@/app/features/company-registration/pages/CompanyRegisterPage').then((m) => ({
    default: m.CompanyRegisterPage,
  })),
)
const ForgotPasswordPage = lazy(() =>
  import('@/app/features/auth/pages/ForgotPasswordPage').then((m) => ({
    default: m.ForgotPasswordPage,
  })),
)
const ResetPasswordPage = lazy(() =>
  import('@/app/features/auth/pages/ResetPasswordPage').then((m) => ({
    default: m.ResetPasswordPage,
  })),
)
const ProfilePage = lazy(() =>
  import('@/app/features/auth/pages/ProfilePage').then((m) => ({ default: m.ProfilePage })),
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
const EmployeeCreatePage = lazy(() =>
  import('@/app/features/employees/pages/EmployeeCreatePage').then((m) => ({
    default: m.EmployeeCreatePage,
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
const DesignationListPage = lazy(() =>
  import('@/app/features/designations/pages/DesignationListPage').then((m) => ({
    default: m.DesignationListPage,
  })),
)
const DesignationDetailPage = lazy(() =>
  import('@/app/features/designations/pages/DesignationDetailPage').then((m) => ({
    default: m.DesignationDetailPage,
  })),
)
const TeamListPage = lazy(() =>
  import('@/app/features/teams/pages/TeamListPage').then((m) => ({ default: m.TeamListPage })),
)
const TeamDetailPage = lazy(() =>
  import('@/app/features/teams/pages/TeamDetailPage').then((m) => ({ default: m.TeamDetailPage })),
)
const OfficeLocationListPage = lazy(() =>
  import('@/app/features/office-locations/pages/OfficeLocationListPage').then((m) => ({
    default: m.OfficeLocationListPage,
  })),
)
const OfficeLocationDetailPage = lazy(() =>
  import('@/app/features/office-locations/pages/OfficeLocationDetailPage').then((m) => ({
    default: m.OfficeLocationDetailPage,
  })),
)
const LeaveListPage = lazy(() =>
  import('@/app/features/leave/pages/LeaveListPage').then((m) => ({ default: m.LeaveListPage })),
)
const LeaveRequestDetailPage = lazy(() =>
  import('@/app/features/leave/pages/LeaveRequestDetailPage').then((m) => ({
    default: m.LeaveRequestDetailPage,
  })),
)
const LeaveApprovalQueuePage = lazy(() =>
  import('@/app/features/leave/pages/LeaveApprovalQueuePage').then((m) => ({
    default: m.LeaveApprovalQueuePage,
  })),
)
const LeaveBalancesPage = lazy(() =>
  import('@/app/features/leave/pages/LeaveBalancesPage').then((m) => ({
    default: m.LeaveBalancesPage,
  })),
)
const LeaveTypeListPage = lazy(() =>
  import('@/app/features/leave-types/pages/LeaveTypeListPage').then((m) => ({
    default: m.LeaveTypeListPage,
  })),
)
const HolidayListPage = lazy(() =>
  import('@/app/features/holidays/pages/HolidayListPage').then((m) => ({
    default: m.HolidayListPage,
  })),
)
const MyPayslipsPage = lazy(() =>
  import('@/app/features/payroll/pages/MyPayslipsPage').then((m) => ({ default: m.MyPayslipsPage })),
)
const SalaryStructuresPage = lazy(() =>
  import('@/app/features/payroll/pages/SalaryStructuresPage').then((m) => ({
    default: m.SalaryStructuresPage,
  })),
)
const PayrollRunsPage = lazy(() =>
  import('@/app/features/payroll/pages/PayrollRunsPage').then((m) => ({ default: m.PayrollRunsPage })),
)
const PayrollRunDetailPage = lazy(() =>
  import('@/app/features/payroll/pages/PayrollRunDetailPage').then((m) => ({
    default: m.PayrollRunDetailPage,
  })),
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
const UserListPage = lazy(() =>
  import('@/app/features/administration/pages/UserListPage').then((m) => ({ default: m.UserListPage })),
)
const UserCreatePage = lazy(() =>
  import('@/app/features/administration/pages/UserCreatePage').then((m) => ({
    default: m.UserCreatePage,
  })),
)
const UserEditPage = lazy(() =>
  import('@/app/features/administration/pages/UserEditPage').then((m) => ({ default: m.UserEditPage })),
)
const RoleListPage = lazy(() =>
  import('@/app/features/administration/pages/RoleListPage').then((m) => ({ default: m.RoleListPage })),
)
const RoleDetailPage = lazy(() =>
  import('@/app/features/administration/pages/RoleDetailPage').then((m) => ({
    default: m.RoleDetailPage,
  })),
)
const ReportsOverviewPage = lazy(() =>
  import('@/app/features/reports/pages/ReportsOverviewPage').then((m) => ({
    default: m.ReportsOverviewPage,
  })),
)
const ExportsPage = lazy(() =>
  import('@/app/features/reports/pages/ExportsPage').then((m) => ({ default: m.ExportsPage })),
)
const AuditLogListPage = lazy(() =>
  import('@/app/features/audit-logs/pages/AuditLogListPage').then((m) => ({
    default: m.AuditLogListPage,
  })),
)
const AnnouncementListPage = lazy(() =>
  import('@/app/features/announcements/pages/AnnouncementListPage').then((m) => ({
    default: m.AnnouncementListPage,
  })),
)
const NotificationsPage = lazy(() =>
  import('@/app/features/notifications/pages/NotificationsPage').then((m) => ({
    default: m.NotificationsPage,
  })),
)
const AssetListPage = lazy(() =>
  import('@/app/features/assets/pages/AssetListPage').then((m) => ({ default: m.AssetListPage })),
)
const AssetDetailPage = lazy(() =>
  import('@/app/features/assets/pages/AssetDetailPage').then((m) => ({ default: m.AssetDetailPage })),
)
const ClientListPage = lazy(() =>
  import('@/app/features/clients/pages/ClientListPage').then((m) => ({ default: m.ClientListPage })),
)
const ClientDetailPage = lazy(() =>
  import('@/app/features/clients/pages/ClientDetailPage').then((m) => ({ default: m.ClientDetailPage })),
)
const TaskListPage = lazy(() =>
  import('@/app/features/tasks/pages/TaskListPage').then((m) => ({ default: m.TaskListPage })),
)
const TaskDetailPage = lazy(() =>
  import('@/app/features/tasks/pages/TaskDetailPage').then((m) => ({ default: m.TaskDetailPage })),
)
const ReimbursementListPage = lazy(() =>
  import('@/app/features/reimbursements/pages/ReimbursementListPage').then((m) => ({
    default: m.ReimbursementListPage,
  })),
)
const ReimbursementDetailPage = lazy(() =>
  import('@/app/features/reimbursements/pages/ReimbursementDetailPage').then((m) => ({
    default: m.ReimbursementDetailPage,
  })),
)
const ReimbursementReviewQueuePage = lazy(() =>
  import('@/app/features/reimbursements/pages/ReimbursementReviewQueuePage').then((m) => ({
    default: m.ReimbursementReviewQueuePage,
  })),
)
const CandidateListPage = lazy(() =>
  import('@/app/features/recruitment/pages/CandidateListPage').then((m) => ({ default: m.CandidateListPage })),
)
const CandidateDetailPage = lazy(() =>
  import('@/app/features/recruitment/pages/CandidateDetailPage').then((m) => ({ default: m.CandidateDetailPage })),
)
const GoalListPage = lazy(() =>
  import('@/app/features/performance/pages/GoalListPage').then((m) => ({ default: m.GoalListPage })),
)
const GoalDetailPage = lazy(() =>
  import('@/app/features/performance/pages/GoalDetailPage').then((m) => ({ default: m.GoalDetailPage })),
)
const ReviewListPage = lazy(() =>
  import('@/app/features/performance/pages/ReviewListPage').then((m) => ({ default: m.ReviewListPage })),
)
const ReviewDetailPage = lazy(() =>
  import('@/app/features/performance/pages/ReviewDetailPage').then((m) => ({ default: m.ReviewDetailPage })),
)
const PromotionListPage = lazy(() =>
  import('@/app/features/performance/pages/PromotionListPage').then((m) => ({ default: m.PromotionListPage })),
)
const PromotionDetailPage = lazy(() =>
  import('@/app/features/performance/pages/PromotionDetailPage').then((m) => ({ default: m.PromotionDetailPage })),
)
const ConversationListPage = lazy(() =>
  import('@/app/features/messaging/pages/ConversationListPage').then((m) => ({ default: m.ConversationListPage })),
)
const ConversationDetailPage = lazy(() =>
  import('@/app/features/messaging/pages/ConversationDetailPage').then((m) => ({ default: m.ConversationDetailPage })),
)
const PlatformDashboardPage = lazy(() =>
  import('@/app/features/platform/pages/PlatformDashboardPage').then((m) => ({ default: m.PlatformDashboardPage })),
)
const CompanyListPage = lazy(() =>
  import('@/app/features/platform/pages/CompanyListPage').then((m) => ({ default: m.CompanyListPage })),
)
const CompanyDetailPage = lazy(() =>
  import('@/app/features/platform/pages/CompanyDetailPage').then((m) => ({ default: m.CompanyDetailPage })),
)
const PlatformAuditLogsPage = lazy(() =>
  import('@/app/features/platform/pages/PlatformAuditLogsPage').then((m) => ({ default: m.PlatformAuditLogsPage })),
)
const PlatformSettingsPage = lazy(() =>
  import('@/app/features/platform/pages/PlatformSettingsPage').then((m) => ({ default: m.PlatformSettingsPage })),
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
          <Route path="/login/mfa" element={<MfaChallengePage />} />
          <Route path="/register" element={<CompanyRegisterPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />

          <Route element={<PlatformProtectedRoute />}>
            <Route element={<PlatformLayout />}>
              <Route path="/platform/dashboard" element={<PlatformDashboardPage />} />
              <Route path="/platform/companies" element={<CompanyListPage />} />
              <Route path="/platform/companies/:id" element={<CompanyDetailPage />} />
              <Route path="/platform/audit-logs" element={<PlatformAuditLogsPage />} />
              <Route path="/platform/settings" element={<PlatformSettingsPage />} />
            </Route>
          </Route>

          <Route element={<ProtectedRoute />}>
            <Route element={<AppLayout />}>
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/employees" element={<EmployeeListPage />} />
              <Route path="/employees/new" element={<EmployeeCreatePage />} />
              <Route path="/employees/:id" element={<EmployeeProfilePage />} />
              <Route path="/departments" element={<DepartmentListPage />} />
              <Route path="/departments/:id" element={<DepartmentDetailPage />} />
              <Route path="/designations" element={<DesignationListPage />} />
              <Route path="/designations/:id" element={<DesignationDetailPage />} />
              <Route path="/teams" element={<TeamListPage />} />
              <Route path="/teams/:id" element={<TeamDetailPage />} />
              <Route path="/office-locations" element={<OfficeLocationListPage />} />
              <Route path="/office-locations/:id" element={<OfficeLocationDetailPage />} />
              <Route path="/leave" element={<LeaveListPage />} />
              <Route path="/leave/approvals" element={<LeaveApprovalQueuePage />} />
              <Route path="/leave/balances" element={<LeaveBalancesPage />} />
              <Route path="/leave/:id" element={<LeaveRequestDetailPage />} />
              <Route path="/leave-types" element={<LeaveTypeListPage />} />
              <Route path="/holidays" element={<HolidayListPage />} />
              <Route path="/payslips" element={<MyPayslipsPage />} />
              <Route path="/payroll/salary-structures" element={<SalaryStructuresPage />} />
              <Route path="/payroll/runs" element={<PayrollRunsPage />} />
              <Route path="/payroll/runs/:id" element={<PayrollRunDetailPage />} />
              <Route path="/attendance" element={<MyAttendancePage />} />
              <Route path="/attendance/records" element={<AttendanceRecordsPage />} />
              <Route path="/attendance/corrections" element={<AttendanceCorrectionsPage />} />
              <Route path="/shifts" element={<ShiftListPage />} />
              <Route path="/shifts/assignments" element={<ShiftAssignmentsPage />} />
              <Route path="/admin/users" element={<UserListPage />} />
              <Route path="/admin/users/new" element={<UserCreatePage />} />
              <Route path="/admin/users/:id/edit" element={<UserEditPage />} />
              <Route path="/admin/roles" element={<RoleListPage />} />
              <Route path="/admin/roles/:id" element={<RoleDetailPage />} />
              <Route path="/reports" element={<ReportsOverviewPage />} />
              <Route path="/reports/exports" element={<ExportsPage />} />
              <Route path="/audit-logs" element={<AuditLogListPage />} />
              <Route path="/announcements" element={<AnnouncementListPage />} />
              <Route path="/notifications" element={<NotificationsPage />} />
              <Route path="/assets" element={<AssetListPage />} />
              <Route path="/assets/:id" element={<AssetDetailPage />} />
              <Route path="/clients" element={<ClientListPage />} />
              <Route path="/clients/:id" element={<ClientDetailPage />} />
              <Route path="/tasks" element={<TaskListPage />} />
              <Route path="/tasks/:id" element={<TaskDetailPage />} />
              <Route path="/reimbursements" element={<ReimbursementListPage />} />
              <Route path="/reimbursements/review" element={<ReimbursementReviewQueuePage />} />
              <Route path="/reimbursements/:id" element={<ReimbursementDetailPage />} />
              <Route path="/candidates" element={<CandidateListPage />} />
              <Route path="/candidates/:id" element={<CandidateDetailPage />} />
              <Route path="/performance/goals" element={<GoalListPage />} />
              <Route path="/performance/goals/:id" element={<GoalDetailPage />} />
              <Route path="/performance/reviews" element={<ReviewListPage />} />
              <Route path="/performance/reviews/:id" element={<ReviewDetailPage />} />
              <Route path="/performance/promotions" element={<PromotionListPage />} />
              <Route path="/performance/promotions/:id" element={<PromotionDetailPage />} />
              <Route path="/messages" element={<ConversationListPage />} />
              <Route path="/messages/:id" element={<ConversationDetailPage />} />
              <Route path="/profile" element={<ProfilePage />} />
            </Route>
          </Route>

          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  )
}
