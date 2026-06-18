import apiClient from './client'

export async function getUsers(query = {}) {
  const { data } = await apiClient.get('User', { params: query })
  return data
}

export async function getUserById(userId) {
  const { data } = await apiClient.get(`User/${userId}`)
  return data
}

export async function createUser(payload) {
  const { data } = await apiClient.post('User', payload)
  return data
}

export async function updateUser(userId, payload) {
  const { data } = await apiClient.put(`User/${userId}`, payload)
  return data
}

export async function deactivateUser(userId) {
  await apiClient.delete(`User/${userId}`)
}

export async function getRoles() {
  const { data } = await apiClient.get('Rbac/roles')
  return data
}

export async function getUserPhones(userId) {
  const { data } = await apiClient.get(`User/${userId}/Phone`)
  return data
}

export async function addUserPhone(userId, payload) {
  const { data } = await apiClient.post(`User/${userId}/Phone`, payload)
  return data
}

export async function updateUserPhone(userId, currentPhone, payload) {
  const { data } = await apiClient.put(`User/${userId}/Phone/${encodeURIComponent(currentPhone)}`, payload)
  return data
}

export async function deleteUserPhone(userId, currentPhone) {
  await apiClient.delete(`User/${userId}/Phone/${encodeURIComponent(currentPhone)}`)
}
