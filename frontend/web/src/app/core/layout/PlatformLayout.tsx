import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '@/app/core/auth/useAuth'

const navLinks = [
  { to: '/platform/dashboard', label: 'Dashboard' },
  { to: '/platform/companies', label: 'Companies' },
  { to: '/platform/audit-logs', label: 'Audit Logs' },
  { to: '/platform/settings', label: 'Settings' },
]

export function PlatformLayout() {
  const { user, logout } = useAuth()

  return (
    <div className="min-h-screen bg-canvas">
      <header className="border-b border-hairline bg-surface-1">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-8 py-3">
          <div className="flex items-center gap-6">
            <div className="flex items-center gap-2">
              <span className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-sm font-semibold text-white">
                E
              </span>
              <span className="text-sm font-semibold text-ink">EMS Platform</span>
            </div>
            <nav className="flex items-center gap-1">
              {navLinks.map((link) => (
                <NavLink
                  key={link.to}
                  to={link.to}
                  className={({ isActive }) =>
                    `rounded-md px-3 py-2 text-sm font-medium transition ${
                      isActive ? 'bg-surface-2 text-ink' : 'text-ink-subtle hover:bg-surface-2 hover:text-ink'
                    }`
                  }
                >
                  {link.label}
                </NavLink>
              ))}
            </nav>
          </div>
          <div className="flex items-center gap-3">
            <span className="text-sm text-ink-subtle">{user?.email}</span>
            <button
              type="button"
              onClick={() => void logout()}
              className="rounded-md px-3 py-2 text-sm font-medium text-ink-subtle transition hover:bg-surface-2 hover:text-ink"
            >
              Sign out
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-8 py-8">
        <Outlet />
      </main>
    </div>
  )
}
