import { Link } from 'react-router-dom'

export function UnauthorizedPage() {
  return (
    <section className="status-card">
      <h2>Access restricted</h2>
      <p>
        Your account is authenticated, but this route requires a different role or
        permission scope.
      </p>
      <div className="status-actions">
        <Link className="btn-link-solid" to="/">
          Back to dashboard
        </Link>
        <Link className="btn-link-muted" to="/login">
          Return to login
        </Link>
      </div>
    </section>
  )
}
