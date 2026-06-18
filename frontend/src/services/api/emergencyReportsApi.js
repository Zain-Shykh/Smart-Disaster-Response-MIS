import apiClient from './client'

export async function getEmergencyReports(query = {}) {
  const { data } = await apiClient.get('EmergencyReport', { params: query })
  return data
}

export async function createEmergencyReport(payload) {
  const { data } = await apiClient.post('EmergencyReport', payload)
  return data
}

export async function updateEmergencyReport(reportId, payload) {
  const { data } = await apiClient.put(`EmergencyReport/${reportId}`, payload)
  return data
}

export async function updateEmergencyReportStatus(reportId, payload) {
  const { data } = await apiClient.patch(`EmergencyReport/${reportId}/status`, payload)
  return data
}

export async function recalculateEmergencyReportPriority(reportId) {
  const { data } = await apiClient.put(`EmergencyReport/${reportId}/priority`)
  return data
}
