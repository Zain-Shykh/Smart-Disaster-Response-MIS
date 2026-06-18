const statusToneMap = {
  complete: 'success',
  completed: 'success',
  success: 'success',
  active: 'info',
  planned: 'warning',
  pending: 'warning',
  blocked: 'danger',
  error: 'danger',
}

export function StatusBadge({ label, status = 'planned' }) {
  const normalized = String(status).trim().toLowerCase()
  const tone = statusToneMap[normalized] || 'neutral'

  return <span className={`status-badge ${tone}`}>{label || status}</span>
}
