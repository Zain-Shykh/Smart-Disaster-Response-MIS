import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'

function hasRoleMatch(userRoles, allowedRoles) {
  if (!allowedRoles || allowedRoles.length === 0) {
    return true
  }

  return allowedRoles.some((role) => userRoles.includes(role))
}

export function ProtectedRoute({ allowedRoles = [], children }) {
  const location = useLocation()
  const { isAuthenticated, roles } = useAuth()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  if (!hasRoleMatch(roles, allowedRoles)) {
    return <Navigate to="/unauthorized" replace />
  }

  return children || <Outlet />
}
