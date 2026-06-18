import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { getCurrentUser, login as loginApi } from '../services/api/authApi'
import { subscribeUnauthorized } from '../services/api/apiEvents'

const TOKEN_STORAGE_KEY = 'sdrmis.accessToken'

const AuthContext = createContext(null)

function mapLoginResponseToUser(response) {
  return {
    userId: response.userId,
    username: response.username,
    email: response.email,
    fullName: response.fullName,
    roles: response.roles || [],
  }
}

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => localStorage.getItem(TOKEN_STORAGE_KEY))
  const [user, setUser] = useState(null)
  const [isInitializing, setIsInitializing] = useState(true)

  useEffect(() => {
    let isMounted = true

    async function bootstrapSession() {
      const storedToken = localStorage.getItem(TOKEN_STORAGE_KEY)

      if (!storedToken) {
        if (isMounted) {
          setToken(null)
          setUser(null)
          setIsInitializing(false)
        }
        return
      }

      try {
        const me = await getCurrentUser()
        if (isMounted) {
          setToken(storedToken)
          setUser(me)
        }
      } catch {
        localStorage.removeItem(TOKEN_STORAGE_KEY)
        if (isMounted) {
          setToken(null)
          setUser(null)
        }
      } finally {
        if (isMounted) {
          setIsInitializing(false)
        }
      }
    }

    bootstrapSession()

    return () => {
      isMounted = false
    }
  }, [])

  useEffect(() => {
    return subscribeUnauthorized(() => {
      localStorage.removeItem(TOKEN_STORAGE_KEY)
      setToken(null)
      setUser(null)
      setIsInitializing(false)
    })
  }, [])

  async function login(credentials) {
    const response = await loginApi(credentials)
    localStorage.setItem(TOKEN_STORAGE_KEY, response.accessToken)
    setToken(response.accessToken)
    setUser(mapLoginResponseToUser(response))
    return response
  }

  function logout() {
    localStorage.removeItem(TOKEN_STORAGE_KEY)
    setToken(null)
    setUser(null)
  }

  const roles = user?.roles || []

  const value = useMemo(
    () => ({
      token,
      user,
      roles,
      isInitializing,
      isAuthenticated: Boolean(token),
      login,
      logout,
    }),
    [token, user, roles, isInitializing],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }

  return context
}
