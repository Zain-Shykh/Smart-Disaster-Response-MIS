import apiClient from './client'

export async function getApprovalRequests(query = {}) {
  const { data } = await apiClient.get('ApprovalWorkflow/requests', { params: query })
  return data
}

export async function getApprovalRequest(requestId) {
  const { data } = await apiClient.get(`ApprovalWorkflow/requests/${requestId}`)
  return data
}

export async function createApprovalRequest(payload) {
  const { data } = await apiClient.post('ApprovalWorkflow/requests', payload)
  return data
}

export async function decideApprovalRequest(requestId, payload) {
  const { data } = await apiClient.patch(`ApprovalWorkflow/requests/${requestId}/decision`, payload)
  return data
}

export async function getApprovalHistory(requestId) {
  const { data } = await apiClient.get(`ApprovalWorkflow/requests/${requestId}/history`)
  return data
}

export async function getAllApprovalHistory(query = {}) {
  const { data } = await apiClient.get('ApprovalWorkflow/history', { params: query })
  return data
}
