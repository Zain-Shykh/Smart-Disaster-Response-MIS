import apiClient from './client'

export async function login(payload) {
  const { data } = await apiClient.post('Auth/login', payload, {
    metadata: {
      suppressGlobalError: true,
      suppressUnauthorizedHandler: true,
    },
  })
  return data
}

export async function getCurrentUser() {
  const { data } = await apiClient.get('Auth/me')
  return data
}
