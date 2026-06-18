import apiClient from './client'

export async function getAuditLogs(query = {}) {
  const { data } = await apiClient.get('Reports/audit/logs', { params: query })
  return data
}

export async function getApprovalHistory(query = {}) {
  const { data } = await apiClient.get('ApprovalWorkflow/history', { params: query })
  return data
}
