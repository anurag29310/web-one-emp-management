import { NavLink } from 'react-router-dom'

const tabs = [
  { to: '/admin/users', label: 'Users' },
  { to: '/admin/roles', label: 'Roles' },
]

/** Sub-navigation shared by the user and role administration screens. */
export function AdminSubNav() {
  return (
    <div className="flex gap-1 border-b border-hairline">
      {tabs.map((tab) => (
        <NavLink
          key={tab.to}
          to={tab.to}
          className={({ isActive }) =>
            `border-b-2 px-3 py-2 text-sm font-medium transition ${
              isActive
                ? 'border-primary text-ink'
                : 'border-transparent text-ink-subtle hover:text-ink'
            }`
          }
        >
          {tab.label}
        </NavLink>
      ))}
    </div>
  )
}
