import { useCallback, useEffect, useMemo, useState , useRef} from 'react'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useNotification } from '../context/NotificationContext'
import {
  createDisasterEvent,
  getDisasterEvents,
  updateDisasterEvent,
  updateDisasterEventStatus,
} from '../services/api/disasterEventsApi'

const STATUS_OPTIONS = ['Active', 'Contained', 'Resolved']

function getDefaultFormState() {
  const now = new Date()
  now.setSeconds(0)
  now.setMilliseconds(0)

  return {
    eventName: '',
    disasterType: '',
    startTime: now.toISOString().slice(0, 16),
    endTime: '',
    street: '',
    area: '',
    city: '',
    province: '',
    status: 'Active',
    affectedPopulation: 0,
  }
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

function toInputDateTimeValue(value) {
  if (!value) {
    return ''
  }

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return ''
  }

  const pad = (number) => String(number).padStart(2, '0')

  return `${parsed.getFullYear()}-${pad(parsed.getMonth() + 1)}-${pad(parsed.getDate())}T${pad(parsed.getHours())}:${pad(parsed.getMinutes())}`
}

function toIsoLocalDateTime(value) {
  if (!value) {
    return '-'
  }

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return '-'
  }

  return parsed.toLocaleString()
}

function getPayloadFromFormState(formState) {
  return {
    eventName: formState.eventName.trim(),
    disasterType: formState.disasterType.trim(),
    startTime: toIsoString(formState.startTime),
    endTime: formState.endTime ? toIsoString(formState.endTime) : null,
    street: formState.street.trim(),
    area: formState.area.trim(),
    city: formState.city.trim(),
    province: formState.province.trim(),
    status: formState.status,
    affectedPopulation: Number(formState.affectedPopulation || 0),
  }
}

function validatePayload(payload) {
  if (!payload.startTime) {
    return 'Start time must be a valid date-time value.'
  }

  if (payload.endTime && payload.endTime < payload.startTime) {
    return 'End time must be greater than or equal to start time.'
  }

  if (payload.affectedPopulation < 0) {
    return 'Affected population cannot be negative.'
  }

  return ''
}

function getEditStateFromEvent(eventRecord) {
  return {
    eventName: eventRecord.eventName,
    disasterType: eventRecord.disasterType,
    startTime: toInputDateTimeValue(eventRecord.startTime),
    endTime: toInputDateTimeValue(eventRecord.endTime),
    street: eventRecord.street,
    area: eventRecord.area,
    city: eventRecord.city,
    province: eventRecord.province,
    status: eventRecord.status,
    affectedPopulation: eventRecord.affectedPopulation,
    versionToken: eventRecord.versionToken,
  }
}

export function DisasterEventsPage() {
  const { notify } = useNotification()

  const [events, setEvents] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [activeStatusFilter, setActiveStatusFilter] = useState('')
  const [processingId, setProcessingId] = useState(null)
  const [formState, setFormState] = useState(() => getDefaultFormState())
  const [isCreating, setIsCreating] = useState(false)
  const [formError, setFormError] = useState('')
  const [editingEventId, setEditingEventId] = useState(null)
  const [editFormState, setEditFormState] = useState(null)
  const [isUpdating, setIsUpdating] = useState(false)
  const [editError, setEditError] = useState('')

  
  const filtersRef = useRef({ activeStatusFilter })
  filtersRef.current = { activeStatusFilter }

const loadEvents = useCallback(
    async ({ refreshOnly = false } = {}) => {
      if (refreshOnly) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }

      try {
        const data = await getDisasterEvents(filtersRef.current.activeStatusFilter ? { status: filtersRef.current.activeStatusFilter } : {})
        setEvents(Array.isArray(data) ? data : [])
      } finally {
        setIsLoading(false)
        setIsRefreshing(false)
      }
    },
    [],
  )

  useEffect(() => {
    loadEvents()
  }, [loadEvents])

  const handleStatusTransition = useCallback(
    async (eventId, nextStatus) => {
      setProcessingId(eventId)

      try {
        await updateDisasterEventStatus(eventId, { status: nextStatus })
        notify({
          title: 'Disaster event updated',
          message: `Event status changed to ${nextStatus}.`,
          variant: 'success',
        })
        await loadEvents({ refreshOnly: true })
      } finally {
        setProcessingId(null)
      }
    },
    [loadEvents, notify],
  )

  function handleFormChange(event) {
    const { name, value } = event.target
    setFormState((previous) => ({
      ...previous,
      [name]: name === 'affectedPopulation' ? Number(value) : value,
    }))
  }

  function handleEditFormChange(event) {
    const { name, value } = event.target
    setEditFormState((previous) => ({
      ...previous,
      [name]: name === 'affectedPopulation' ? Number(value) : value,
    }))
  }

  const openEditForm = useCallback((eventRecord) => {
    setEditingEventId(eventRecord.eventId)
    setEditFormState(getEditStateFromEvent(eventRecord))
    setEditError('')
  }, [])

  function closeEditForm() {
    setEditingEventId(null)
    setEditFormState(null)
    setEditError('')
  }

  async function handleCreateSubmit(event) {
    event.preventDefault()
    setFormError('')

    const payload = getPayloadFromFormState(formState)
    const validationError = validatePayload(payload)

    if (validationError) {
      setFormError(validationError)
      return
    }

    setIsCreating(true)

    try {
      await createDisasterEvent(payload)
      notify({
        title: 'Disaster event created',
        message: `New event ${payload.eventName} was added successfully.`,
        variant: 'success',
      })
      setFormState(getDefaultFormState())
      await loadEvents({ refreshOnly: true })
    } catch {
      setFormError('Unable to create disaster event. Review details and try again.')
    } finally {
      setIsCreating(false)
    }
  }

  async function handleEditSubmit(event) {
    event.preventDefault()

    if (!editingEventId || !editFormState) {
      return
    }

    setEditError('')

    const payload = getPayloadFromFormState(editFormState)
    const validationError = validatePayload(payload)

    if (validationError) {
      setEditError(validationError)
      return
    }

    setIsUpdating(true)

    try {
      await updateDisasterEvent(editingEventId, {
        ...payload,
        versionToken: editFormState.versionToken,
      })

      notify({
        title: 'Disaster event saved',
        message: `Event ${payload.eventName} was updated successfully.`,
        variant: 'success',
      })

      closeEditForm()
      await loadEvents({ refreshOnly: true })
    } catch (error) {
      const statusCode = error.response?.status

      if (statusCode === 409) {
        const conflictMessage =
          error.response?.data?.message ||
          'Concurrency conflict detected. Reload the event and apply your changes again.'

        setEditError(conflictMessage)
        notify({
          title: 'Update conflict',
          message: conflictMessage,
          variant: 'warning',
        })
        await loadEvents({ refreshOnly: true })
        return
      }

      setEditError('Unable to update disaster event. Review details and try again.')
    } finally {
      setIsUpdating(false)
    }
  }

  const columns = useMemo(
    () => [
      { key: 'eventName', header: 'Event' },
      { key: 'disasterType', header: 'Type' },
      {
        key: 'location',
        header: 'Location',
        render: (row) => `${row.area}, ${row.city}`,
      },
      {
        key: 'startTime',
        header: 'Started At',
        render: (row) => toIsoLocalDateTime(row.startTime),
      },
      {
        key: 'status',
        header: 'Status',
        align: 'center',
        render: (row) => <StatusBadge label={row.status} status={row.status} />,
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
              disabled={
                processingId === row.eventId ||
                isUpdating ||
                row.status === 'Contained'
              }
              onClick={() => handleStatusTransition(row.eventId, 'Contained')}
            >
              Set Contained
            </button>
            <button
              type="button"
              className="table-action-btn"
              disabled={
                processingId === row.eventId ||
                isUpdating ||
                row.status === 'Resolved'
              }
              onClick={() => handleStatusTransition(row.eventId, 'Resolved')}
            >
              Resolve
            </button>
            <button
              type="button"
              className="table-action-btn"
              disabled={processingId === row.eventId || isUpdating}
              onClick={() => openEditForm(row)}
            >
              {editingEventId === row.eventId ? 'Editing' : 'Edit'}
            </button>
          </div>
        ),
      },
    ],
    [processingId, isUpdating, openEditForm, editingEventId, handleStatusTransition],
  )

  if (isLoading) {
    return (
      <LoadingState
        title="Loading disaster events"
        message="Collecting event data and readiness status from the backend."
      />
    )
  }

  return (
    <div>
<AppCard
        title="Create Disaster Event"
        subtitle="Register new events with location, timing, and severity context."
      >
        <form className="event-create-form" onSubmit={handleCreateSubmit}>
          {formError ? (
            <AlertBanner
              variant="warning"
              title="Validation check"
              message={formError}
            />
          ) : null}

          <div className="event-form-grid">
            <label>
              Event Name
              <input
                type="text"
                name="eventName"
                value={formState.eventName}
                onChange={handleFormChange}
                required
                maxLength={150}
              />
            </label>
            <label>
              Disaster Type
              <input
                type="text"
                name="disasterType"
                value={formState.disasterType}
                onChange={handleFormChange}
                required
                maxLength={100}
              />
            </label>
            <label>
              Start Time
              <input
                type="datetime-local"
                name="startTime"
                value={formState.startTime}
                onChange={handleFormChange}
                required
              />
            </label>
            <label>
              End Time
              <input
                type="datetime-local"
                name="endTime"
                value={formState.endTime}
                onChange={handleFormChange}
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
              />
            </label>
            <label>
              Province
              <input
                type="text"
                name="province"
                value={formState.province}
                onChange={handleFormChange}
                required
                maxLength={120}
              />
            </label>
            <label>
              Status
              <select
                name="status"
                value={formState.status}
                onChange={handleFormChange}
                required
              >
                {STATUS_OPTIONS.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Affected Population
              <input
                type="number"
                name="affectedPopulation"
                value={formState.affectedPopulation}
                onChange={handleFormChange}
                min={0}
                required
              />
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isCreating}>
              {isCreating ? 'Creating event...' : 'Create Event'}
            </button>
          </div>
        </form>
      </AppCard>

      {editFormState ? (
        <AppCard
          title={`Edit Disaster Event #${editingEventId}`}
          subtitle="Update event details with optimistic concurrency protection."
        >
          <form className="event-create-form" onSubmit={handleEditSubmit}>
            {editError ? (
              <AlertBanner variant="warning" title="Update check" message={editError} />
            ) : null}

            <p className="event-form-meta">
              Version token: <span className="mono-cell">{editFormState.versionToken}</span>
            </p>

            <div className="event-form-grid">
              <label>
                Event Name
                <input
                  type="text"
                  name="eventName"
                  value={editFormState.eventName}
                  onChange={handleEditFormChange}
                  required
                  maxLength={150}
                />
              </label>
              <label>
                Disaster Type
                <input
                  type="text"
                  name="disasterType"
                  value={editFormState.disasterType}
                  onChange={handleEditFormChange}
                  required
                  maxLength={100}
                />
              </label>
              <label>
                Start Time
                <input
                  type="datetime-local"
                  name="startTime"
                  value={editFormState.startTime}
                  onChange={handleEditFormChange}
                  required
                />
              </label>
              <label>
                End Time
                <input
                  type="datetime-local"
                  name="endTime"
                  value={editFormState.endTime}
                  onChange={handleEditFormChange}
                />
              </label>
              <label>
                Street
                <input
                  type="text"
                  name="street"
                  value={editFormState.street}
                  onChange={handleEditFormChange}
                  required
                  maxLength={150}
                />
              </label>
              <label>
                Area
                <input
                  type="text"
                  name="area"
                  value={editFormState.area}
                  onChange={handleEditFormChange}
                  required
                  maxLength={120}
                />
              </label>
              <label>
                City
                <input
                  type="text"
                  name="city"
                  value={editFormState.city}
                  onChange={handleEditFormChange}
                  required
                  maxLength={120}
                />
              </label>
              <label>
                Province
                <input
                  type="text"
                  name="province"
                  value={editFormState.province}
                  onChange={handleEditFormChange}
                  required
                  maxLength={120}
                />
              </label>
              <label>
                Status
                <select
                  name="status"
                  value={editFormState.status}
                  onChange={handleEditFormChange}
                  required
                >
                  {STATUS_OPTIONS.map((status) => (
                    <option key={status} value={status}>
                      {status}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Affected Population
                <input
                  type="number"
                  name="affectedPopulation"
                  value={editFormState.affectedPopulation}
                  onChange={handleEditFormChange}
                  min={0}
                  required
                />
              </label>
            </div>

            <div className="event-form-actions">
              <button type="submit" className="table-action-btn" disabled={isUpdating}>
                {isUpdating ? 'Saving changes...' : 'Save Changes'}
              </button>
              <button type="button" className="table-action-btn" onClick={closeEditForm}>
                Cancel
              </button>
            </div>
          </form>
        </AppCard>
      ) : null}

      <AppCard
        title="Disaster Event Queue"
        subtitle="Filter active incidents and update event lifecycle status."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="statusFilter" className="toolbar-label">
              Status
            </label>
            <select
              id="statusFilter"
              value={activeStatusFilter}
              onChange={(event) => setActiveStatusFilter(event.target.value)}
            >
              <option value="">All</option>
              <option value="Active">Active</option>
              <option value="Contained">Contained</option>
              <option value="Resolved">Resolved</option>
            </select>
            <button
              type="button"
              className="table-action-btn"
              onClick={() => loadEvents({ refreshOnly: true })}
              disabled={isRefreshing}
            >
              {isRefreshing ? 'Refreshing...' : 'Refresh'}
            </button>
          </div>
        }
      >
        <DataTable
          caption="Live disaster event records"
          columns={columns}
          rows={events}
          getRowKey={(row) => row.eventId}
          emptyMessage="No disaster events matched the current filter."
        />
      </AppCard>
    </div>
  )
}
