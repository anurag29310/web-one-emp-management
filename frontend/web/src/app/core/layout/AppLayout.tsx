import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '@/app/core/auth/useAuth'
import { Avatar } from '@/app/shared/components/Avatar'

const navItems = [
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
]

export function AppLayout() {
  const { user, logout } = useAuth()

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
          {navItems.map((item) => (
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
          <div className="flex items-center gap-2 rounded-md px-2 py-2">
            <Avatar name={user?.email ?? '?'} size="sm" />
            <div className="min-w-0 flex-1">
              <p className="truncate text-xs font-medium text-ink">{user?.email}</p>
              <p className="truncate text-xs text-ink-subtle">{user?.role}</p>
            </div>
          </div>
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
