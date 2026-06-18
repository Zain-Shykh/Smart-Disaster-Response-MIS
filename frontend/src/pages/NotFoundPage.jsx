import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <section className="status-card">
      <h2>Page not found</h2>
      <p>
        The route does not exist in the current frontend module map. Continue from the
        dashboard navigation.
      </p>
      <div className="status-actions">
        <Link className="btn-link-solid" to="/">
          Go to dashboard
        </Link>
        <Link className="btn-link-muted" to="/login">
          Go to login
        </Link>
      </div>
    </section>
  )
}
