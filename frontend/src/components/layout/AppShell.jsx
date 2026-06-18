import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { moduleRoutes, navigationSections } from '../../app/navigation'
import { useAuth } from '../../context/AuthContext'

function canAccess(roles, requiredRoles) {
  if (!requiredRoles || requiredRoles.length === 0) {
    return true
  }

  return requiredRoles.some((role) => roles.includes(role))
}

function getPageTitle(pathname) {
  if (pathname === '/') {
    return {
      title: 'Operations Dashboard',
      description: 'Unified control surface for response, logistics, finance, and governance.',
    }
  }

  const route = moduleRoutes.find((item) => `/${item.path}` === pathname)

  if (!route) {
    return {
      title: 'Workflow Module',
      description: 'Role-aware operational module for the disaster MIS frontend.',
    }
  }

  return {
    title: route.label,
    description: route.description,
  }
}

export function AppShell() {
  const { user, roles, logout } = useAuth()
  const location = useLocation()
  const pageMeta = getPageTitle(location.pathname)

  const visibleSections = navigationSections
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => canAccess(roles, item.roles)),
    }))
    .filter((section) => section.items.length > 0)

  return (
    <div className="app-shell">
      <aside className="app-sidebar">
        <div className="brand">
          <h2 className="brand-title">SDRMIS — NDMA</h2>
          <p className="brand-subtitle">Pakistan Crisis Command Center</p>
        </div>

        <nav aria-label="Main navigation">
          {visibleSections.map((section) => (
            <div className="menu-section" key={section.title}>
              <h3 className="menu-section-title">{section.title}</h3>
              {section.items.map((item) => (
                <NavLink
                  key={item.path}
                  to={item.path === '/' ? '/' : `/${item.path}`}
                  end={item.path === '/'}
                  className={({ isActive }) => `menu-link${isActive ? ' active' : ''}`}
                >
                  <span>{item.label}</span>
                  {item.tag ? <span className="menu-tag">{item.tag}</span> : null}
                </NavLink>
              ))}
            </div>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="sidebar-user">
            <strong>{user?.fullName || 'Signed in user'}</strong>
            <span>{user?.email || 'No profile loaded'}</span>
          </div>
          <button type="button" className="btn-ghost" onClick={logout}>
            Sign out
          </button>
        </div>
      </aside>

      <main className="app-main">
        <header className="app-header">
          <h1>{pageMeta.title}</h1>
          <p>{pageMeta.description}</p>
        </header>

        <section className="app-content">
          <Outlet />
        </section>
      </main>
    </div>
  )
}
