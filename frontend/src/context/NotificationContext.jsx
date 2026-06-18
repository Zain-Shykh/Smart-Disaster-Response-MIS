import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { subscribeApiProblem } from '../services/api/apiEvents'

const NotificationContext = createContext(null)

function resolveVariant(status) {
  if (status >= 500) {
    return 'danger'
  }

  if (status >= 400) {
    return 'warning'
  }

  return 'info'
}

export function NotificationProvider({ children }) {
  const [notifications, setNotifications] = useState([])

  const dismiss = useCallback((id) => {
    setNotifications((previous) => previous.filter((notification) => notification.id !== id))
  }, [])

  const notify = useCallback(
    ({ title, message, status, variant }) => {
      const id = `${Date.now()}-${Math.random().toString(36).slice(2, 10)}`
      const toast = {
        id,
        title: title || 'Request update',
        message: message || 'An action completed with additional details.',
        status,
        variant: variant || resolveVariant(status || 0),
      }

      setNotifications((previous) => [toast, ...previous].slice(0, 5))
      window.setTimeout(() => dismiss(id), 6500)
    },
    [dismiss],
  )

  useEffect(() => {
    return subscribeApiProblem((problem) => {
      notify({
        title: problem.title,
        message: problem.detail,
        status: problem.status,
      })
    })
  }, [notify])

  const value = useMemo(
    () => ({
      notify,
      dismiss,
      clear: () => setNotifications([]),
    }),
    [notify, dismiss],
  )

  return (
    <NotificationContext.Provider value={value}>
      {children}
      <div className="notification-center" aria-live="polite" aria-atomic="false">
        {notifications.map((notification) => (
          <article
            key={notification.id}
            className={`notification-toast ${notification.variant}`}
            role="status"
          >
            <div className="notification-toast-content">
              <strong>{notification.title}</strong>
              <p>{notification.message}</p>
            </div>
            <button
              type="button"
              className="notification-close"
              onClick={() => dismiss(notification.id)}
              aria-label="Dismiss notification"
            >
              x
            </button>
          </article>
        ))}
      </div>
    </NotificationContext.Provider>
  )
}

export function useNotification() {
  const context = useContext(NotificationContext)

  if (!context) {
    throw new Error('useNotification must be used within NotificationProvider')
  }

  return context
}
