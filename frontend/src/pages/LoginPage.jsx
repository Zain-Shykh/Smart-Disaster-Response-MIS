import { useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { hasTrimmedText } from '../utils/formGuards'

function getErrorMessage(error) {
  if (!error?.response) {
    return 'Login failed. Check backend availability and try again.'
  }

  const data = error.response.data

  if (typeof data === 'string') {
    return data
  }

  if (typeof data?.detail === 'string') {
    return data.detail
  }

  if (typeof data?.title === 'string') {
    return data.title
  }

  return 'Login failed. Please verify credentials and try again.'
}

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { login, isAuthenticated } = useAuth()

  const [formState, setFormState] = useState({
    usernameOrEmail: '',
    password: '',
  })
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  const intendedPath = location.state?.from?.pathname || '/'
  const isLoginReady = hasTrimmedText(formState.usernameOrEmail, 3) && formState.password.length >= 4

  async function handleSubmit(event) {
    event.preventDefault()
    setErrorMessage('')

    if (!isLoginReady) {
      setErrorMessage('Enter a valid username/email and password before signing in.')
      return
    }

    setIsSubmitting(true)

    try {
      await login(formState)
      navigate(intendedPath, { replace: true })
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsSubmitting(false)
    }
  }

  function handleInputChange(event) {
    const { name, value } = event.target
    setFormState((previous) => ({
      ...previous,
      [name]: value,
    }))
  }

  return (
    <div className="auth-screen">
      <section className="auth-promo">
        <div>
          <h1>Pakistan Crisis Command Center — NDMA</h1>
          <p>
            Securely manage disasters, rescue teams (1122, Edhi, PDMA),
            logistics, hospitals, and PKR financial flows with role-based
            workflows aligned to NDMA operational approvals.
          </p>
        </div>

        <div className="auth-promo-metrics">

          <article>
            <span>Active Provinces</span>
            <strong>6</strong>
          </article>
          <article>
            <span>Rescue Teams</span>
            <strong>1122+</strong>
          </article>
          <article>
            <span>NDMA Controllers</span>
            <strong>17</strong>
          </article>
        </div>
      </section>

      <section className="auth-card-wrap">
        <form className="auth-card" onSubmit={handleSubmit} noValidate>
          <h2>Sign in</h2>
          <p className="auth-subtitle">Use your authorized system account to continue.</p>

          {errorMessage ? <div className="auth-error">{errorMessage}</div> : null}

          <div className="form-row">
            <label htmlFor="usernameOrEmail">Username or Email</label>
            <input
              id="usernameOrEmail"
              name="usernameOrEmail"
              type="text"
              required
              minLength={3}
              maxLength={255}
              value={formState.usernameOrEmail}
              onChange={handleInputChange}
              autoComplete="username"
            />
          </div>

          <p className="event-form-meta">Use the username or email attached to your authorized account.</p>

          <div className="form-row">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              name="password"
              type="password"
              required
              minLength={4}
              maxLength={200}
              value={formState.password}
              onChange={handleInputChange}
              autoComplete="current-password"
            />
          </div>

          <button className="btn-primary-solid" type="submit" disabled={isSubmitting || !isLoginReady}>
            {isSubmitting ? 'Signing in...' : 'Sign in securely'}
          </button>
          
          <div style={{ marginTop: '2rem', textAlign: 'center', borderTop: '1px solid var(--border-color)', paddingTop: '1.5rem' }}>
            <p className="event-form-meta" style={{ marginBottom: '1rem' }}>Civilian emergency helpline</p>
            <button className="table-action-btn" type="button" onClick={() => navigate('/public-report')} style={{ width: '100%' }}>
              Report A Disaster
            </button>
          </div>
        </form>
      </section>
    </div>
  )
}
