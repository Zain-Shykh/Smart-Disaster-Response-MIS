import apiClient from './client'

export async function submitPublicReport(payload) {
  const { data } = await apiClient.post('PublicEmergencyReport', payload)
  return data
}
