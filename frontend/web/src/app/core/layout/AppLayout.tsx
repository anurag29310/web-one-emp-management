import { type ReactNode, useState } from 'react'
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '@/app/core/auth/useAuth'
import { Avatar } from '@/app/shared/components/Avatar'
import type { Role } from '@/app/shared/models/user'
import { NotificationBell } from '@/app/features/notifications/components/NotificationBell'

interface NavLeaf {
  to: string
  label: string
  icon: ReactNode
  roles?: Role[]
}

interface NavGroupDef {
  key: string
  label: string
  icon: ReactNode
  items: NavLeaf[]
}

type NavEntry = { kind: 'leaf'; leaf: NavLeaf } | { kind: 'group'; group: NavGroupDef }

const dashboardIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M3.75 6a2.25 2.25 0 0 1 2.25-2.25h.75A2.25 2.25 0 0 1 9 6v.75a2.25 2.25 0 0 1-2.25 2.25h-.75A2.25 2.25 0 0 1 3.75 6.75V6ZM3.75 15a2.25 2.25 0 0 1 2.25-2.25h.75A2.25 2.25 0 0 1 9 15v.75a2.25 2.25 0 0 1-2.25 2.25h-.75A2.25 2.25 0 0 1 3.75 15.75V15ZM12.75 6a2.25 2.25 0 0 1 2.25-2.25h.75A2.25 2.25 0 0 1 18 6v.75a2.25 2.25 0 0 1-2.25 2.25h-.75A2.25 2.25 0 0 1 12.75 6.75V6ZM12.75 15a2.25 2.25 0 0 1 2.25-2.25h.75a2.25 2.25 0 0 1 2.25 2.25v.75a2.25 2.25 0 0 1-2.25 2.25h-.75a2.25 2.25 0 0 1-2.25-2.25V15Z"
  />
)

const employeesIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M15 19.128a9.38 9.38 0 0 0 2.625.372 9.337 9.337 0 0 0 4.121-.952 4.125 4.125 0 0 0-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 0 1 8.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0 1 11.964-3.07M12 6.375a3.375 3.375 0 1 1-6.75 0 3.375 3.375 0 0 1 6.75 0Zm8.25 2.25a2.625 2.625 0 1 1-5.25 0 2.625 2.625 0 0 1 5.25 0Z"
  />
)

const departmentsIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6 21v-3.375c0-.621.504-1.125 1.125-1.125h1.5c.621 0 1.125.504 1.125 1.125V21"
  />
)

const designationsIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M4.5 19.5h15M4.5 19.5v-15m0 15h15m-15-15h15m-15 0v15m15-15v15M9 12.75h1.5v3H9v-3Zm4.5-3H15v6h-1.5v-6Z"
  />
)

const teamsIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M18 18.72a9.094 9.094 0 0 0 3.741-.479 3 3 0 0 0-4.682-2.72m.94 3.198.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0 1 12 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 0 1 6 18.719m12 0a5.971 5.971 0 0 0-.941-3.197m0 0A5.995 5.995 0 0 0 12 12.75a5.995 5.995 0 0 0-5.058 2.772m0 0a3 3 0 0 0-4.681 2.72 8.986 8.986 0 0 0 3.74.477m.94-3.197a5.971 5.971 0 0 0-.94 3.197M15 6.75a3 3 0 1 1-6 0 3 3 0 0 1 6 0Zm6 3a2.25 2.25 0 1 1-4.5 0 2.25 2.25 0 0 1 4.5 0Zm-13.5 0a2.25 2.25 0 1 1-4.5 0 2.25 2.25 0 0 1 4.5 0Z"
  />
)

const officeLocationsIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M15 10.5a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z M19.5 10.5c0 7.142-7.5 11.25-7.5 11.25S4.5 17.642 4.5 10.5a7.5 7.5 0 1 1 15 0Z"
  />
)

const assetsIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M20.25 14.15v4.25c0 1.094-.787 2.036-1.872 2.18-2.087.277-4.216.42-6.378.42s-4.291-.143-6.378-.42c-1.085-.144-1.872-1.086-1.872-2.18v-4.25m16.5 0a2.18 2.18 0 0 0 .75-1.653v-2.494a2.25 2.25 0 0 0-.383-1.257l-1.5-2.25A2.25 2.25 0 0 0 17.5 5.25H6.5a2.25 2.25 0 0 0-1.867.997l-1.5 2.25a2.25 2.25 0 0 0-.383 1.257v2.494c0 .627.28 1.223.75 1.653m16.5 0a2.25 2.25 0 0 1-1.5.573H5.25a2.25 2.25 0 0 1-1.5-.573m16.5 0v-.075a2.25 2.25 0 0 0-.373-1.246l-1.373-2.06a2.25 2.25 0 0 0-1.873-1.001H8.62a2.25 2.25 0 0 0-1.873 1.001l-1.373 2.06A2.25 2.25 0 0 0 5 12.075v.075"
  />
)

const attendanceIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M12 6v6l4 2M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
  />
)

const leaveIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0V11.25A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5m-9-6h.008v.008H12v-.008ZM12 15h.008v.008H12V15Zm0 2.25h.008v.008H12v-.008ZM9.75 15h.008v.008H9.75V15Zm0 2.25h.008v.008H9.75v-.008ZM7.5 15h.008v.008H7.5V15Zm0 2.25h.008v.008H7.5v-.008Z"
  />
)

const leaveTypesIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M9 12h3.75M9 15h3.75M9 18h3.75M3.75 3.75h16.5v16.5H3.75V3.75Zm5.25 4.5v-1.5m0 1.5h6m-6 0h-2.25"
  />
)

const shiftsIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M4.5 3.75h15A.75.75 0 0 1 20.25 4.5v3.75a.75.75 0 0 1-.75.75h-15a.75.75 0 0 1-.75-.75V4.5a.75.75 0 0 1 .75-.75Zm0 8.25h9a.75.75 0 0 1 .75.75v6.75a.75.75 0 0 1-.75.75h-9a.75.75 0 0 1-.75-.75v-6.75a.75.75 0 0 1 .75-.75Zm12.75 1.5v6"
  />
)

const holidaysIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0V11.25A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5M8.25 12.75h1.5v1.5h-1.5v-1.5Zm3 0h1.5v1.5h-1.5v-1.5Zm3 0h1.5v1.5h-1.5v-1.5Z"
  />
)

const announcementsIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M8.625 12a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm0 0H8.25m4.125 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm0 0H12m4.125 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm0 0h-.375M21 12c0 4.556-4.03 8.25-9 8.25a9.764 9.764 0 0 1-2.555-.337A5.972 5.972 0 0 1 5.41 20.97a5.969 5.969 0 0 1-.474-.065 4.48 4.48 0 0 0 .978-2.025c.09-.457-.133-.901-.467-1.226C3.93 16.178 3 14.189 3 12c0-4.556 4.03-8.25 9-8.25s9 3.694 9 8.25Z"
  />
)

const payslipsIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z"
  />
)

const salaryStructuresIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M12 6v12m-3-2.818.879.659c1.171.879 3.07.879 4.242 0 1.172-.879 1.172-2.303 0-3.182C13.536 12.219 12.768 12 12 12c-.725 0-1.45-.22-2.003-.659-1.106-.879-1.106-2.303 0-3.182s2.9-.879 4.006 0l.415.33M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
  />
)

const payrollRunsIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 0 0 2.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 0 0-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 0 0 .75-.75 2.25 2.25 0 0 0-.1-.664m-5.8 0A2.251 2.251 0 0 1 13.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25Z"
  />
)

const userManagementIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M18 18.72a9.094 9.094 0 0 0 3.741-.479 3 3 0 0 0-4.682-2.72m.94 3.198.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0 1 12 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 0 1 6 18.719m12 0a5.971 5.971 0 0 0-.941-3.197m0 0A5.995 5.995 0 0 0 12 12.75a5.995 5.995 0 0 0-5.058 2.772m0 0a3 3 0 0 0-4.681 2.72 8.986 8.986 0 0 0 3.74.477m.94-3.197a5.971 5.971 0 0 0-.94 3.197M15 6.75a3 3 0 1 1-6 0 3 3 0 0 1 6 0Zm6 3a2.25 2.25 0 1 1-4.5 0 2.25 2.25 0 0 1 4.5 0Zm-13.5 0a2.25 2.25 0 1 1-4.5 0 2.25 2.25 0 0 1 4.5 0Z"
  />
)

const reportsIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z"
  />
)

const auditLogsIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M9 12.75 11.25 15 15 9.75M21 12c0 4.556-3.04 8.55-7.5 9.93-4.46-1.38-7.5-5.374-7.5-9.93 0-.245.014-.487.04-.725a2.25 2.25 0 0 1 1.638-1.905 48.607 48.607 0 0 0 6.115-2.13c.325-.145.7-.145 1.025 0a48.554 48.554 0 0 0 6.115 2.13 2.25 2.25 0 0 1 1.638 1.905c.026.238.04.48.04.725Z"
  />
)

const profileIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M17.982 18.725A7.488 7.488 0 0 0 12 15.75a7.488 7.488 0 0 0-5.982 2.975m11.964 0a9 9 0 1 0-11.964 0m11.964 0A8.966 8.966 0 0 1 12 21a8.966 8.966 0 0 1-5.982-2.275M15 9.75a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z"
  />
)

const chevronIcon = (
  <path strokeLinecap="round" strokeLinejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
)

const tasksIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M8.25 6.75h12M8.25 12h12m-12 5.25h12M3.75 6.75h.007v.008H3.75V6.75Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0ZM3.75 12h.007v.008H3.75V12Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm-.375 5.25h.007v.008H3.75v-.008Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Z"
  />
)

const clientsIcon = (
  <path
    strokeLinecap="round"
    strokeLinejoin="round"
    d="M15 9h3.75M15 12h3.75M15 15h3.75M4.5 19.5h15a2.25 2.25 0 0 0 2.25-2.25V6.75A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25v10.5A2.25 2.25 0 0 0 4.5 19.5Zm6-10.125a1.875 1.875 0 1 1-3.75 0 1.875 1.875 0 0 1 3.75 0Zm1.294 6.336a6.721 6.721 0 0 1-3.17.789 6.721 6.721 0 0 1-3.168-.789 3.376 3.376 0 0 1 6.338 0Z"
  />
)

const navEntries: NavEntry[] = [
  { kind: 'leaf', leaf: { to: '/dashboard', label: 'Dashboard', icon: dashboardIcon } },
  { kind: 'leaf', leaf: { to: '/tasks', label: 'Tasks', icon: tasksIcon } },
  {
    kind: 'group',
    group: {
      key: 'organization',
      label: 'Organization',
      icon: employeesIcon,
      items: [
        { to: '/employees', label: 'Employees', icon: employeesIcon },
        { to: '/departments', label: 'Departments', icon: departmentsIcon },
        { to: '/designations', label: 'Designations', icon: designationsIcon },
        { to: '/teams', label: 'Teams', icon: teamsIcon },
        { to: '/office-locations', label: 'Office Locations', icon: officeLocationsIcon },
        { to: '/clients', label: 'Clients', icon: clientsIcon },
        { to: '/assets', label: 'Assets', icon: assetsIcon, roles: ['Admin', 'HR'] },
      ],
    },
  },
  {
    kind: 'group',
    group: {
      key: 'attendance-leave',
      label: 'Attendance & Leave',
      icon: attendanceIcon,
      items: [
        { to: '/attendance', label: 'Attendance', icon: attendanceIcon },
        { to: '/leave', label: 'Leave', icon: leaveIcon },
        { to: '/leave-types', label: 'Leave Types', icon: leaveTypesIcon, roles: ['Admin', 'HR'] },
        { to: '/shifts', label: 'Shifts', icon: shiftsIcon, roles: ['Admin', 'HR'] },
        { to: '/holidays', label: 'Holidays', icon: holidaysIcon },
      ],
    },
  },
  { kind: 'leaf', leaf: { to: '/announcements', label: 'Announcements', icon: announcementsIcon } },
  { kind: 'leaf', leaf: { to: '/reimbursements', label: 'Reimbursements', icon: payslipsIcon } },
  {
    kind: 'group',
    group: {
      key: 'payroll',
      label: 'Payroll',
      icon: payslipsIcon,
      items: [
        { to: '/payslips', label: 'My Payslips', icon: payslipsIcon },
        { to: '/payroll/salary-structures', label: 'Salary Structures', icon: salaryStructuresIcon, roles: ['Admin', 'HR'] },
        { to: '/payroll/runs', label: 'Payroll Runs', icon: payrollRunsIcon, roles: ['Admin', 'HR'] },
      ],
    },
  },
  {
    kind: 'group',
    group: {
      key: 'administration',
      label: 'Administration',
      icon: userManagementIcon,
      items: [
        { to: '/admin/users', label: 'User Management', icon: userManagementIcon, roles: ['Admin'] },
        { to: '/reports', label: 'Reports', icon: reportsIcon, roles: ['Admin', 'HR', 'Manager'] },
        { to: '/audit-logs', label: 'Audit Logs', icon: auditLogsIcon, roles: ['Admin'] },
      ],
    },
  },
  { kind: 'leaf', leaf: { to: '/profile', label: 'Profile', icon: profileIcon } },
]

function isRoleVisible(roles: Role[] | undefined, role: Role | null | undefined) {
  return !roles || (role && roles.includes(role))
}

function matchesPath(pathname: string, to: string) {
  return pathname === to || pathname.startsWith(`${to}/`)
}

function NavLeafLink({ leaf, indented }: { leaf: NavLeaf; indented?: boolean }) {
  return (
    <NavLink
      to={leaf.to}
      className={({ isActive }) =>
        `flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition ${
          indented ? 'pl-3' : ''
        } ${isActive ? 'bg-surface-2 text-ink' : 'text-ink-subtle hover:bg-surface-2 hover:text-ink'}`
      }
    >
      {({ isActive }) => (
        <>
          <svg
            viewBox="0 0 24 24"
            fill="none"
            strokeWidth={1.5}
            stroke="currentColor"
            className={`h-5 w-5 shrink-0 ${isActive ? 'text-primary' : ''}`}
          >
            {leaf.icon}
          </svg>
          {leaf.label}
        </>
      )}
    </NavLink>
  )
}

function NavGroupSection({
  group,
  visibleItems,
  isOpen,
  isActive,
  onToggle,
}: {
  group: NavGroupDef
  visibleItems: NavLeaf[]
  isOpen: boolean
  isActive: boolean
  onToggle: () => void
}) {
  return (
    <div>
      <button
        type="button"
        onClick={onToggle}
        aria-expanded={isOpen}
        className={`flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition ${
          isActive ? 'text-ink' : 'text-ink-subtle hover:bg-surface-2 hover:text-ink'
        }`}
      >
        <svg
          viewBox="0 0 24 24"
          fill="none"
          strokeWidth={1.5}
          stroke="currentColor"
          className={`h-5 w-5 shrink-0 ${isActive ? 'text-primary' : ''}`}
        >
          {group.icon}
        </svg>
        <span className="flex-1 text-left">{group.label}</span>
        <svg
          viewBox="0 0 24 24"
          fill="none"
          strokeWidth={1.5}
          stroke="currentColor"
          className={`h-4 w-4 shrink-0 transition-transform duration-300 ease-in-out ${isOpen ? 'rotate-90' : ''}`}
        >
          {chevronIcon}
        </svg>
      </button>

      <div
        className={`grid transition-[grid-template-rows] duration-300 ease-in-out ${
          isOpen ? 'grid-rows-[1fr]' : 'grid-rows-[0fr]'
        }`}
      >
        <div className="overflow-hidden">
          <div
            className={`ml-4 space-y-1 border-l border-hairline pl-2 pt-1 transition-opacity duration-300 ease-in-out ${
              isOpen ? 'opacity-100' : 'opacity-0'
            }`}
          >
            {visibleItems.map((item) => (
              <NavLeafLink key={item.to} leaf={item} indented />
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}

export function AppLayout() {
  const { user, logout } = useAuth()
  const location = useLocation()

  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>(() => {
    const initial: Record<string, boolean> = {}
    for (const entry of navEntries) {
      if (entry.kind === 'group' && entry.group.items.some((item) => matchesPath(location.pathname, item.to))) {
        initial[entry.group.key] = true
      }
    }
    return initial
  })

  // Auto-expand whichever group contains the newly-active path. Adjusted during render (React's
  // documented pattern for state derived from a changing prop) rather than in an effect, so a
  // manual collapse afterward isn't immediately fought by a re-run — see
  // https://react.dev/learn/you-might-not-need-an-effect#adjusting-some-state-when-a-prop-changes.
  const [prevPathname, setPrevPathname] = useState(location.pathname)
  if (location.pathname !== prevPathname) {
    setPrevPathname(location.pathname)
    setOpenGroups((prev) => {
      let changed = false
      const next = { ...prev }
      for (const entry of navEntries) {
        if (
          entry.kind === 'group' &&
          !next[entry.group.key] &&
          entry.group.items.some((item) => matchesPath(location.pathname, item.to))
        ) {
          next[entry.group.key] = true
          changed = true
        }
      }
      return changed ? next : prev
    })
  }

  const toggleGroup = (key: string) => {
    setOpenGroups((prev) => ({ ...prev, [key]: !prev[key] }))
  }

  return (
    <div className="flex min-h-screen bg-canvas">
      <aside className="flex w-64 flex-col border-r border-hairline bg-surface-1">
        <div className="flex items-center gap-2 px-5 py-5">
          <span className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-sm font-semibold text-white">
            E
          </span>
          <span className="text-sm font-semibold text-ink">EMS</span>
        </div>

        <nav className="flex-1 space-y-1 overflow-y-auto px-3 pb-3">
          {navEntries.map((entry) => {
            if (entry.kind === 'leaf') {
              if (!isRoleVisible(entry.leaf.roles, user?.role)) return null
              return <NavLeafLink key={entry.leaf.to} leaf={entry.leaf} />
            }

            const visibleItems = entry.group.items.filter((item) => isRoleVisible(item.roles, user?.role))
            if (visibleItems.length === 0) return null

            const isActive = visibleItems.some((item) => matchesPath(location.pathname, item.to))

            return (
              <NavGroupSection
                key={entry.group.key}
                group={entry.group}
                visibleItems={visibleItems}
                isOpen={Boolean(openGroups[entry.group.key])}
                isActive={isActive}
                onToggle={() => toggleGroup(entry.group.key)}
              />
            )
          })}
        </nav>

        <div className="border-t border-hairline p-3">
          <Link
            to="/profile"
            className="flex items-center gap-2 rounded-md px-2 py-2 transition hover:bg-surface-2"
          >
            <Avatar name={user?.email ?? '?'} size="sm" />
            <div className="min-w-0 flex-1">
              <p className="truncate text-xs font-medium text-ink">{user?.email}</p>
              <p className="truncate text-xs text-ink-subtle">{user?.role}</p>
            </div>
          </Link>
          <button
            type="button"
            onClick={() => void logout()}
            className="mt-1 w-full rounded-md px-3 py-2 text-left text-sm font-medium text-ink-subtle transition hover:bg-surface-2 hover:text-ink"
          >
            Sign out
          </button>
        </div>
      </aside>

      <main className="flex-1 overflow-y-auto">
        <header className="flex items-center justify-end border-b border-hairline bg-surface-1 px-8 py-2.5">
          <NotificationBell />
        </header>
        <div className="mx-auto max-w-5xl px-8 py-8">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
