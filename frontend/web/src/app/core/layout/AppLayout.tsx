import type { ReactNode } from 'react'
import { Link, NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '@/app/core/auth/useAuth'
import { Avatar } from '@/app/shared/components/Avatar'
import type { Role } from '@/app/shared/models/user'

const navItems: { to: string; label: string; icon: ReactNode; roles?: Role[] }[] = [
  {
    to: '/dashboard',
    label: 'Dashboard',
    icon: (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M3.75 6a2.25 2.25 0 0 1 2.25-2.25h.75A2.25 2.25 0 0 1 9 6v.75a2.25 2.25 0 0 1-2.25 2.25h-.75A2.25 2.25 0 0 1 3.75 6.75V6ZM3.75 15a2.25 2.25 0 0 1 2.25-2.25h.75A2.25 2.25 0 0 1 9 15v.75a2.25 2.25 0 0 1-2.25 2.25h-.75A2.25 2.25 0 0 1 3.75 15.75V15ZM12.75 6a2.25 2.25 0 0 1 2.25-2.25h.75A2.25 2.25 0 0 1 18 6v.75a2.25 2.25 0 0 1-2.25 2.25h-.75A2.25 2.25 0 0 1 12.75 6.75V6ZM12.75 15a2.25 2.25 0 0 1 2.25-2.25h.75a2.25 2.25 0 0 1 2.25 2.25v.75a2.25 2.25 0 0 1-2.25 2.25h-.75a2.25 2.25 0 0 1-2.25-2.25V15Z"
      />
    ),
  },
  {
    to: '/employees',
    label: 'Employees',
    icon: (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M15 19.128a9.38 9.38 0 0 0 2.625.372 9.337 9.337 0 0 0 4.121-.952 4.125 4.125 0 0 0-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 0 1 8.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0 1 11.964-3.07M12 6.375a3.375 3.375 0 1 1-6.75 0 3.375 3.375 0 0 1 6.75 0Zm8.25 2.25a2.625 2.625 0 1 1-5.25 0 2.625 2.625 0 0 1 5.25 0Z"
      />
    ),
  },
  {
    to: '/departments',
    label: 'Departments',
    icon: (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6 21v-3.375c0-.621.504-1.125 1.125-1.125h1.5c.621 0 1.125.504 1.125 1.125V21"
      />
    ),
  },
  {
    to: '/leave',
    label: 'Leave',
    icon: (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0V11.25A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5m-9-6h.008v.008H12v-.008ZM12 15h.008v.008H12V15Zm0 2.25h.008v.008H12v-.008ZM9.75 15h.008v.008H9.75V15Zm0 2.25h.008v.008H9.75v-.008ZM7.5 15h.008v.008H7.5V15Zm0 2.25h.008v.008H7.5v-.008Z"
      />
    ),
  },
  {
    to: '/attendance',
    label: 'Attendance',
    icon: (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M12 6v6l4 2M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
      />
    ),
  },
  {
    to: '/shifts',
    label: 'Shifts',
    roles: ['Admin', 'HR'],
    icon: (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M4.5 3.75h15A.75.75 0 0 1 20.25 4.5v3.75a.75.75 0 0 1-.75.75h-15a.75.75 0 0 1-.75-.75V4.5a.75.75 0 0 1 .75-.75Zm0 8.25h9a.75.75 0 0 1 .75.75v6.75a.75.75 0 0 1-.75.75h-9a.75.75 0 0 1-.75-.75v-6.75a.75.75 0 0 1 .75-.75Zm12.75 1.5v6"
      />
    ),
  },
  {
    to: '/leave-types',
    label: 'Leave Types',
    roles: ['Admin', 'HR'],
    icon: (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M9 12h3.75M9 15h3.75M9 18h3.75M3.75 3.75h16.5v16.5H3.75V3.75Zm5.25 4.5v-1.5m0 1.5h6m-6 0h-2.25"
      />
    ),
  },
  {
    to: '/holidays',
    label: 'Holidays',
    icon: (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0V11.25A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5M8.25 12.75h1.5v1.5h-1.5v-1.5Zm3 0h1.5v1.5h-1.5v-1.5Zm3 0h1.5v1.5h-1.5v-1.5Z"
      />
    ),
  },
  {
    to: '/payslips',
    label: 'My Payslips',
    icon: (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z"
      />
    ),
  },
  {
    to: '/payroll/salary-structures',
    label: 'Salary Structures',
    roles: ['Admin', 'HR'],
    icon: (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M12 6v12m-3-2.818.879.659c1.171.879 3.07.879 4.242 0 1.172-.879 1.172-2.303 0-3.182C13.536 12.219 12.768 12 12 12c-.725 0-1.45-.22-2.003-.659-1.106-.879-1.106-2.303 0-3.182s2.9-.879 4.006 0l.415.33M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
      />
    ),
  },
  {
    to: '/payroll/runs',
    label: 'Payroll Runs',
    roles: ['Admin', 'HR'],
    icon: (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 0 0 2.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 0 0-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 0 0 .75-.75 2.25 2.25 0 0 0-.1-.664m-5.8 0A2.251 2.251 0 0 1 13.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25Z"
      />
    ),
  },
  {
    to: '/admin/users',
    label: 'User Management',
    roles: ['Admin'],
    icon: (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M18 18.72a9.094 9.094 0 0 0 3.741-.479 3 3 0 0 0-4.682-2.72m.94 3.198.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0 1 12 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 0 1 6 18.719m12 0a5.971 5.971 0 0 0-.941-3.197m0 0A5.995 5.995 0 0 0 12 12.75a5.995 5.995 0 0 0-5.058 2.772m0 0a3 3 0 0 0-4.681 2.72 8.986 8.986 0 0 0 3.74.477m.94-3.197a5.971 5.971 0 0 0-.94 3.197M15 6.75a3 3 0 1 1-6 0 3 3 0 0 1 6 0Zm6 3a2.25 2.25 0 1 1-4.5 0 2.25 2.25 0 0 1 4.5 0Zm-13.5 0a2.25 2.25 0 1 1-4.5 0 2.25 2.25 0 0 1 4.5 0Z"
      />
    ),
  },
  {
    to: '/profile',
    label: 'Profile',
    icon: (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M17.982 18.725A7.488 7.488 0 0 0 12 15.75a7.488 7.488 0 0 0-5.982 2.975m11.964 0a9 9 0 1 0-11.964 0m11.964 0A8.966 8.966 0 0 1 12 21a8.966 8.966 0 0 1-5.982-2.275M15 9.75a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z"
      />
    ),
  },
]

export function AppLayout() {
  const { user, logout } = useAuth()
  const visibleNavItems = navItems.filter((item) => !item.roles || (user?.role && item.roles.includes(user.role)))

  return (
    <div className="flex min-h-screen bg-canvas">
      <aside className="flex w-60 flex-col border-r border-hairline bg-surface-1">
        <div className="flex items-center gap-2 px-5 py-5">
          <span className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-sm font-semibold text-white">
            E
          </span>
          <span className="text-sm font-semibold text-ink">EMS</span>
        </div>

        <nav className="flex-1 space-y-1 px-3">
          {visibleNavItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition ${
                  isActive ? 'bg-surface-2 text-ink' : 'text-ink-subtle hover:bg-surface-2 hover:text-ink'
                }`
              }
            >
              {({ isActive }) => (
                <>
                  <svg
                    viewBox="0 0 24 24"
                    fill="none"
                    strokeWidth={1.5}
                    stroke="currentColor"
                    className={`h-5 w-5 ${isActive ? 'text-primary' : ''}`}
                  >
                    {item.icon}
                  </svg>
                  {item.label}
                </>
              )}
            </NavLink>
          ))}
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
        <div className="mx-auto max-w-5xl px-8 py-8">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
