import apiClient from './client'

export async function getIncidentsByLocation(query = {}) {
  const { data } = await apiClient.get('Reports/incidents/by-location', { params: query })
  return data
}

export async function getIncidentsByType(query = {}) {
  const { data } = await apiClient.get('Reports/incidents/by-type', { params: query })
  return data
}

export async function getPrioritizedIncidents(query = {}) {
  const { data } = await apiClient.get('Reports/incidents/prioritized', { params: query })
  return data
}

export async function getResourceUtilization(query = {}) {
  const { data } = await apiClient.get('Reports/resources/utilization', { params: query })
  return data
}

export async function getOverviewReport() {
  const { data } = await apiClient.get('Reports/overview')
  return data
}

export async function getFinancialSummary(query = {}) {
  const { data } = await apiClient.get('Reports/financial/summary', { params: query })
  return data
}

export async function getApprovalSummary() {
  const { data } = await apiClient.get('Reports/approvals/summary')
  return data
}

export async function getAuditLogs(query = {}) {
  const { data } = await apiClient.get('Reports/audit/logs', { params: query })
  return data
}

export async function getIncidentsBySeverity(query = {}) {
  const { data } = await apiClient.get('Reports/incidents/by-severity', { params: query })
  return data
}

export async function getIncidentTrend(query = {}) {
  const { data } = await apiClient.get('Reports/incidents/trend', { params: query })
  return data
}
