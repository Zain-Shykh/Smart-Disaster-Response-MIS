import apiClient from './client'

export async function getRescueTeams(query = {}) {
  const { data } = await apiClient.get('RescueTeam', { params: query })
  return data
}

export async function createRescueTeam(payload) {
  const { data } = await apiClient.post('RescueTeam', payload)
  return data
}

export async function updateRescueTeamAvailability(teamId, payload) {
  const { data } = await apiClient.patch(`RescueTeam/${teamId}/availability`, payload)
  return data
}

export async function getTeamAssignments(teamId) {
  const { data } = await apiClient.get(`RescueTeam/${teamId}/assignments`)
  return data
}

export async function createTeamAssignment(teamId, payload) {
  const { data } = await apiClient.post(`RescueTeam/${teamId}/assignments`, payload)
  return data
}

export async function updateTeamAssignmentStatus(teamId, assignmentId, payload) {
  const { data } = await apiClient.patch(
    `RescueTeam/${teamId}/assignments/${assignmentId}/status`,
    payload,
  )
  return data
}

export async function getRescueTeamRecommendations(reportId, limit = 5) {
  const { data } = await apiClient.get('RescueTeam/recommendations', {
    params: { reportId, limit },
  })
  return data
}
