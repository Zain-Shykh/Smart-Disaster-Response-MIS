import apiClient from './client'

export async function getTeamActivities(query = {}) {
  const { data } = await apiClient.get('TeamActivity', { params: query })
  return data
}

export async function createTeamActivity(payload) {
  const { data } = await apiClient.post('TeamActivity', payload)
  return data
}

export async function getTeamActivitySummary(teamId) {
  const { data } = await apiClient.get(`TeamActivity/summary/${teamId}`)
  return data
}
