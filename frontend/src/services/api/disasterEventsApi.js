import apiClient from './client'

export async function getDisasterEvents(query = {}) {
  const { data } = await apiClient.get('DisasterEvent', { params: query })
  return data
}

export async function createDisasterEvent(payload) {
  const { data } = await apiClient.post('DisasterEvent', payload)
  return data
}

export async function updateDisasterEvent(eventId, payload) {
  const { data } = await apiClient.put(`DisasterEvent/${eventId}`, payload)
  return data
}

export async function updateDisasterEventStatus(eventId, payload) {
  const { data } = await apiClient.patch(`DisasterEvent/${eventId}/status`, payload)
  return data
}
