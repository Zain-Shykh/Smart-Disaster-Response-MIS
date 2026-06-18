import apiClient from './client'

export async function getRoles(query = {}) {
  const { data } = await apiClient.get('Role', { params: query })
  return data
}

export async function getRoleById(roleId) {
  const { data } = await apiClient.get(`Role/${roleId}`)
  return data
}

export async function createRole(payload) {
  const { data } = await apiClient.post('Role', payload)
  return data
}

export async function updateRole(roleId, payload) {
  const { data } = await apiClient.put(`Role/${roleId}`, payload)
  return data
}

export async function getPermissions(query = {}) {
  const { data } = await apiClient.get('Permission', { params: query })
  return data
}

export async function getPermissionById(permissionId) {
  const { data } = await apiClient.get(`Permission/${permissionId}`)
  return data
}

export async function createPermission(payload) {
  const { data } = await apiClient.post('Permission', payload)
  return data
}

export async function getRolePermissions(roleId) {
  const { data } = await apiClient.get(`Rbac/roles/${roleId}/permissions`)
  return data
}

export async function mapRolePermission(payload) {
  const { data } = await apiClient.post('Rbac/role-permission', payload)
  return data
}

export async function unmapRolePermission(roleId, permissionId) {
  await apiClient.delete(`Rbac/role-permission/${roleId}/${permissionId}`)
}

export async function getUsers(query = {}) {
  const { data } = await apiClient.get('User', { params: query })
  return data
}

export async function getUserRoles(userId) {
  const { data } = await apiClient.get(`Rbac/users/${userId}/roles`)
  return data
}

export async function assignRoleToUser(userId, payload) {
  const { data } = await apiClient.post(`Rbac/users/${userId}/roles`, payload)
  return data
}

export async function removeRoleFromUser(userId, roleId) {
  await apiClient.delete(`Rbac/users/${userId}/roles/${roleId}`)
}
