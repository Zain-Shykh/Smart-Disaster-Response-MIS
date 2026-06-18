import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useNotification } from '../context/NotificationContext'
import {
  createEmergencyReport,
  getEmergencyReports,
  recalculateEmergencyReportPriority,
  updateEmergencyReportStatus,
} from '../services/api/emergencyReportsApi'

const STATUS_OPTIONS = ['Pending', 'InProgress', 'Resolved', 'Closed']
const SEVERITY_OPTIONS = ['Low', 'Medium', 'High', 'Critical']
const SEVERITY_LABELS = {
  Low: 'Low / Kam',
  Medium: 'Medium / Darmiyana',
  High: 'High / Zyada',
  Critical: 'Critical / Shadeed',
}
const SOURCE_OPTIONS = ['Mobile', 'Helpline', 'MonitoringSystem', 'Rescue1122', 'EdhiFoundation', 'PDMA']
const DISASTER_TYPES = [
  'Monsoon Flash Flood',
  'River Flood',
  'GLOF (Glacial Lake Outburst)',
  'Earthquake',
  'Heatwave Emergency',
  'Urban Fire',
  'Wildfire',
  'Building Collapse',
  'Landslide',
  'Cyclone / Storm',
  'Chemical Spill',
  'Industrial Accident',
  'Terrorist Attack',
  'Medical Emergency',
  'Other',
]
const PROVINCE_OPTIONS = ['Punjab', 'Sindh', 'KPK', 'Balochistan', 'Gilgit-Baltistan', 'AJK', 'ICT']
const CITY_SUGGESTIONS = [
  'Islamabad', 'Karachi', 'Lahore', 'Peshawar', 'Quetta', 'Muzaffarabad',
  'Nowshera', 'Charsadda', 'Balakot', 'Jacobabad', 'Sukkur', 'Rajanpur',
  'Dera Ismail Khan', 'Gilgit', 'Hunza', 'Swat', 'Multan', 'Faisalabad',
]

function toDisplayDateTime(value) {
  if (!value) {
    return '-'
  }

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return '-'
  }

  return parsed.toLocaleString()
}

function toIsoString(value) {
  if (!value) {
    return null
  }

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return null
  }

  return parsed.toISOString()
}

function getDefaultFormState() {
  const now = new Date()
  now.setSeconds(0)
  now.setMilliseconds(0)

  return {
    citizenId: '',
    eventId: '',
    street: '',
    area: '',
    city: '',
    province: '',
    disasterType: '',
    severityLevel: 'Medium',
    reportTime: now.toISOString().slice(0, 16),
    status: 'Pending',
    source: 'Mobile',
    description: '',
  }
}

export function EmergencyReportsPage() {
  const { notify } = useNotification()

  const [reports, setReports] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [statusFilter, setStatusFilter] = useState('')
  const [severityFilter, setSeverityFilter] = useState('')
  const [sourceFilter, setSourceFilter] = useState('')
  const [cityFilter, setCityFilter] = useState('')
  const [disasterTypeFilter, setDisasterTypeFilter] = useState('')
  const [fromFilter, setFromFilter] = useState('')
  const [toFilter, setToFilter] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [formState, setFormState] = useState(() => getDefaultFormState())
  const [formError, setFormError] = useState('')
  const [processingActionKey, setProcessingActionKey] = useState('')

  /* Keep a ref to current filter values so the loadReports callback
     never changes identity when the user types in a filter field.
     This prevents the useEffect from re-firing on every keystroke. */
  const filtersRef = useRef({
    statusFilter, severityFilter, sourceFilter,
    cityFilter, disasterTypeFilter, fromFilter, toFilter,
  })
  filtersRef.current = {
    statusFilter, severityFilter, sourceFilter,
    cityFilter, disasterTypeFilter, fromFilter, toFilter,
  }

  const loadReports = useCallback(
    async ({ refreshOnly = false } = {}) => {
      if (refreshOnly) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }

      try {
        const f = filtersRef.current
        const query = {
          ...(f.statusFilter ? { status: f.statusFilter } : {}),
          ...(f.severityFilter ? { severityLevel: f.severityFilter } : {}),
          ...(f.sourceFilter ? { source: f.sourceFilter } : {}),
          ...(f.cityFilter.trim() ? { city: f.cityFilter.trim() } : {}),
          ...(f.disasterTypeFilter.trim()
            ? { disasterType: f.disasterTypeFilter.trim() }
            : {}),
          ...(toIsoString(f.fromFilter) ? { from: toIsoString(f.fromFilter) } : {}),
          ...(toIsoString(f.toFilter) ? { to: toIsoString(f.toFilter) } : {}),
        }

        const data = await getEmergencyReports(query)
        setReports(Array.isArray(data) ? data : [])
      } finally {
        setIsLoading(false)
        setIsRefreshing(false)
      }
    },
    [],  // stable identity — never re-created
  )

  // Only load once on mount
  useEffect(() => {
    loadReports()
  }, [loadReports])

  function handleFormChange(event) {
    const { name, value } = event.target
    setFormState((previous) => ({
      ...previous,
      [name]: value,
    }))
  }

  async function handleCreateReport(event) {
    event.preventDefault()
    setFormError('')

    const payload = {
      citizenId: Number(formState.citizenId),
      eventId: formState.eventId ? Number(formState.eventId) : null,
      street: formState.street.trim(),
      area: formState.area.trim(),
      city: formState.city.trim(),
      province: formState.province.trim(),
      disasterType: formState.disasterType.trim(),
      severityLevel: formState.severityLevel,
      reportTime: toIsoString(formState.reportTime),
      status: formState.status,
      source: formState.source,
      description: formState.description.trim() || null,
    }

    if (!payload.citizenId || payload.citizenId <= 0) {
      setFormError('Citizen ID must be greater than zero.')
      return
    }

    if (!payload.reportTime) {
      setFormError('Report time must be a valid date-time value.')
      return
    }

    setIsSubmitting(true)

    try {
      await createEmergencyReport(payload)
      notify({
        title: 'Emergency report created',
        message: 'A new emergency report was submitted successfully.',
        variant: 'success',
      })
      setFormState(getDefaultFormState())
      await loadReports({ refreshOnly: true })
    } catch {
      setFormError('Unable to create emergency report. Verify fields and referenced IDs.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const updateReportStatus = useCallback(
    async (reportId, nextStatus) => {
      setProcessingActionKey(`${reportId}-${nextStatus}`)

      try {
        await updateEmergencyReportStatus(reportId, { status: nextStatus })
        notify({
          title: 'Report status updated',
          message: `Report status changed to ${nextStatus}.`,
          variant: 'success',
        })
        await loadReports({ refreshOnly: true })
      } finally {
        setProcessingActionKey('')
      }
    },
    [loadReports, notify],
  )

  const recalculatePriority = useCallback(
    async (reportId) => {
      setProcessingActionKey(`${reportId}-priority`)

      try {
        const priority = await recalculateEmergencyReportPriority(reportId)
        notify({
          title: 'Priority recalculated',
          message: `Report #${reportId} is now ${priority.priorityLabel} (score ${priority.priorityScore}).`,
          variant: 'info',
        })
        await loadReports({ refreshOnly: true })
      } finally {
        setProcessingActionKey('')
      }
    },
    [loadReports, notify],
  )

  function isActionBusy(reportId, action) {
    return processingActionKey === `${reportId}-${action}`
  }

  const columns = useMemo(
    () => [
      {
        key: 'reportId',
        header: 'ID',
        render: (row) => <span className="mono-cell">#{row.reportId}</span>,
      },
      { key: 'disasterType', header: 'Type' },
      {
        key: 'severityLevel',
        header: 'Severity',
        render: (row) => (
          <StatusBadge
            label={row.severityLevel}
            status={row.severityLevel === 'Critical' ? 'danger' : 'warning'}
          />
        ),
      },
      {
        key: 'status',
        header: 'Status',
        render: (row) => <StatusBadge label={row.status} status={row.status} />,
      },
      {
        key: 'priorityLabel',
        header: 'Priority',
        render: (row) => (
          <div className="priority-cell">
            <StatusBadge label={row.priorityLabel} status="active" />
            <span className="priority-score">Score {row.priorityScore}</span>
          </div>
        ),
      },
      {
        key: 'location',
        header: 'Location',
        render: (row) => `${row.area}, ${row.city}`,
      },
      {
        key: 'reportTime',
        header: 'Reported At',
        render: (row) => toDisplayDateTime(row.reportTime),
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <div className="table-actions">
            <button
              type="button"
              className="table-action-btn"
              disabled={isActionBusy(row.reportId, 'InProgress') || row.status === 'InProgress'}
              onClick={() => updateReportStatus(row.reportId, 'InProgress')}
            >
              Start
            </button>
            <button
              type="button"
              className="table-action-btn"
              disabled={isActionBusy(row.reportId, 'Resolved') || row.status === 'Resolved'}
              onClick={() => updateReportStatus(row.reportId, 'Resolved')}
            >
              Resolve
            </button>
            <button
              type="button"
              className="table-action-btn"
              disabled={isActionBusy(row.reportId, 'Closed') || row.status === 'Closed'}
              onClick={() => updateReportStatus(row.reportId, 'Closed')}
            >
              Close
            </button>
            <button
              type="button"
              className="table-action-btn"
              disabled={isActionBusy(row.reportId, 'priority')}
              onClick={() => recalculatePriority(row.reportId)}
            >
              Recalculate
            </button>
          </div>
        ),
      },
    ],
    [processingActionKey, updateReportStatus, recalculatePriority],
  )

  if (isLoading) {
    return (
      <LoadingState
        title="Loading emergency reports"
        message="Collecting report intake and queue data from the backend."
      />
    )
  }

  return (
    <div>


      <AppCard
        title="Create Emergency Report"
        subtitle="Capture incident intake details and route the report into the response queue."
      >
        <form className="event-create-form" onSubmit={handleCreateReport}>
          {formError ? <AlertBanner variant="warning" title="Validation check" message={formError} /> : null}

          <div className="event-form-grid">
            <label>
              Citizen ID
              <input
                type="number"
                name="citizenId"
                value={formState.citizenId}
                onChange={handleFormChange}
                min={1}
                required
              />
            </label>
            <label>
              Event ID (Optional)
              <input
                type="number"
                name="eventId"
                value={formState.eventId}
                onChange={handleFormChange}
                min={1}
              />
            </label>
            <label>
              Street
              <input
                type="text"
                name="street"
                value={formState.street}
                onChange={handleFormChange}
                required
                maxLength={150}
              />
            </label>
            <label>
              Area
              <input
                type="text"
                name="area"
                value={formState.area}
                onChange={handleFormChange}
                required
                maxLength={120}
              />
            </label>
            <label>
              City
              <input
                type="text"
                name="city"
                value={formState.city}
                onChange={handleFormChange}
                required
                maxLength={120}
                list="city-suggestions"
                placeholder="e.g., Nowshera, Karachi, Muzaffarabad"
              />
              <datalist id="city-suggestions">
                {CITY_SUGGESTIONS.map((c) => (
                  <option key={c} value={c} />
                ))}
              </datalist>
            </label>
            <label>
              Province
              <select
                name="province"
                value={formState.province}
                onChange={handleFormChange}
                required
              >
                <option value="">Select Province</option>
                {PROVINCE_OPTIONS.map((p) => (
                  <option key={p} value={p}>{p}</option>
                ))}
              </select>
            </label>
            <label>
              Disaster Type
              <select
                name="disasterType"
                value={formState.disasterType}
                onChange={handleFormChange}
                required
              >
                <option value="">Select Disaster Type</option>
                {DISASTER_TYPES.map((dt) => (
                  <option key={dt} value={dt}>{dt}</option>
                ))}
              </select>
            </label>
            <label>
              Severity (Shadiddat)
              <select
                name="severityLevel"
                value={formState.severityLevel}
                onChange={handleFormChange}
                required
              >
                {SEVERITY_OPTIONS.map((option) => (
                  <option key={option} value={option}>
                    {SEVERITY_LABELS[option] || option}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Report Time
              <input
                type="datetime-local"
                name="reportTime"
                value={formState.reportTime}
                onChange={handleFormChange}
                required
              />
            </label>
            <label>
              Initial Status
              <select name="status" value={formState.status} onChange={handleFormChange} required>
                {STATUS_OPTIONS.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Source
              <select name="source" value={formState.source} onChange={handleFormChange} required>
                {SOURCE_OPTIONS.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Description
              <input
                type="text"
                name="description"
                value={formState.description}
                onChange={handleFormChange}
                maxLength={2000}
              />
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isSubmitting}>
              {isSubmitting ? 'Submitting report...' : 'Submit Report'}
            </button>
          </div>
        </form>
      </AppCard>

      <AppCard
        title="Emergency Report Queue"
        subtitle="Review incoming reports with operational filters and prioritization controls."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="reportStatusFilter" className="toolbar-label">
              Status
            </label>
            <select
              id="reportStatusFilter"
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value)}
            >
              <option value="">All</option>
              {STATUS_OPTIONS.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>

            <label htmlFor="reportSeverityFilter" className="toolbar-label">
              Severity
            </label>
            <select
              id="reportSeverityFilter"
              value={severityFilter}
              onChange={(event) => setSeverityFilter(event.target.value)}
            >
              <option value="">All</option>
              {SEVERITY_OPTIONS.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>

            <label htmlFor="reportSourceFilter" className="toolbar-label">
              Source
            </label>
            <select
              id="reportSourceFilter"
              value={sourceFilter}
              onChange={(event) => setSourceFilter(event.target.value)}
            >
              <option value="">All</option>
              {SOURCE_OPTIONS.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>

            <label htmlFor="reportCityFilter" className="toolbar-label">
              City
            </label>
            <input
              id="reportCityFilter"
              value={cityFilter}
              onChange={(event) => setCityFilter(event.target.value)}
              placeholder="City"
            />

            <label htmlFor="reportTypeFilter" className="toolbar-label">
              Type
            </label>
            <input
              id="reportTypeFilter"
              value={disasterTypeFilter}
              onChange={(event) => setDisasterTypeFilter(event.target.value)}
              placeholder="Disaster type"
            />

            <label htmlFor="reportFromFilter" className="toolbar-label">
              From
            </label>
            <input
              id="reportFromFilter"
              type="datetime-local"
              value={fromFilter}
              onChange={(event) => setFromFilter(event.target.value)}
            />

            <label htmlFor="reportToFilter" className="toolbar-label">
              To
            </label>
            <input
              id="reportToFilter"
              type="datetime-local"
              value={toFilter}
              onChange={(event) => setToFilter(event.target.value)}
            />

            <button
              type="button"
              className="table-action-btn"
              onClick={() => loadReports({ refreshOnly: true })}
              disabled={isRefreshing}
            >
              {isRefreshing ? 'Searching...' : 'Search'}
            </button>
          </div>
        }
      >
        <DataTable
          caption="Live emergency report queue"
          columns={columns}
          rows={reports}
          getRowKey={(row) => row.reportId}
          emptyMessage="No emergency reports matched current filters."
        />
      </AppCard>
    </div>
  )
}
