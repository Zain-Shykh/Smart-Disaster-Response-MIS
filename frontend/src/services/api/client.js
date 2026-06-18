import axios from 'axios'
import { emitApiProblem, emitUnauthorized } from './apiEvents'

const baseURL = import.meta.env.VITE_API_BASE_URL?.trim() || '/api'

const apiClient = axios.create({
  baseURL,
  timeout: 15000,
})

function mapProblemDetails(error) {
  if (!error.response) {
    return {
      title: 'Network error',
      detail: 'Unable to reach the backend service. Verify the API is running.',
      status: 0,
    }
  }

  const status = error.response.status
  const payload = error.response.data

  if (typeof payload === 'string') {
    return {
      title: `Request failed (${status})`,
      detail: payload,
      status,
    }
  }

  if (payload && typeof payload === 'object') {
    return {
      title: payload.title || `Request failed (${status})`,
      detail: payload.detail || 'The request could not be processed.',
      status,
    }
  }

  return {
    title: `Request failed (${status})`,
    detail: 'An unexpected error occurred while processing the request.',
    status,
  }
}

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('sdrmis.accessToken')

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const metadata = error.config?.metadata || {}
    const problem = mapProblemDetails(error)

    if (!metadata.suppressGlobalError) {
      emitApiProblem(problem)
    }

    if (problem.status === 401 && !metadata.suppressUnauthorizedHandler) {
      emitUnauthorized(problem)
    }

    return Promise.reject(error)
  },
)

export default apiClient
