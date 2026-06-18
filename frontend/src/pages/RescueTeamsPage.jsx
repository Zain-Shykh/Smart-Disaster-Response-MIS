import { useCallback, useEffect, useMemo, useState , useRef} from 'react'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useAuth } from '../context/AuthContext'
import { useNotification } from '../context/NotificationContext'
import {
  createRescueTeam,
  createTeamAssignment,
  getRescueTeamRecommendations,
  getRescueTeams,
  getTeamAssignments,
  updateRescueTeamAvailability,
  updateTeamAssignmentStatus,
} from '../services/api/rescueTeamsApi'
import {
  createTeamActivity,
  getTeamActivities,
  getTeamActivitySummary,
} from '../services/api/teamActivitiesApi'
import { getEmergencyReports } from '../services/api/emergencyReportsApi'

const TEAM_TYPES = ['Medical', 'Fire', 'Rescue', 'Search']
const TEAM_AVAILABILITY = ['Available', 'Assigned', 'Busy', 'Completed']
const ASSIGNMENT_STATUSES = ['Assigned', 'EnRoute', 'OnSite', 'Completed']

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

function getDefaultTeamForm() {
  return {
    teamName: '',
    teamType: 'Rescue',
    street: '',
    area: '',
    city: '',
    province: '',
    latitude: '',
    longitude: '',
    availabilityStatus: 'Available',
    capacity: 5,
  }
}

function getDefaultAssignmentForm() {
  return {
    reportId: '',
    status: 'Assigned',
    requiresApproval: false,
    approvalRequestedBy: '',
  }
}

function getDefaultActivityForm() {
  const now = new Date()
  now.setSeconds(0)
  now.setMilliseconds(0)

  return {
    activityType: '',
    startTime: now.toISOString().slice(0, 16),
    endTime: '',
    notes: '',
    outcome: '',
  }
}

export function RescueTeamsPage() {
  const { user } = useAuth()
  const { notify } = useNotification()

  const [teams, setTeams] = useState([])
  const [isLoadingTeams, setIsLoadingTeams] = useState(true)
  const [isRefreshingTeams, setIsRefreshingTeams] = useState(false)
  const [teamTypeFilter, setTeamTypeFilter] = useState('')
  const [availabilityFilter, setAvailabilityFilter] = useState('')
  const [teamCityFilter, setTeamCityFilter] = useState('')

  const [teamForm, setTeamForm] = useState(() => getDefaultTeamForm())
  const [teamFormError, setTeamFormError] = useState('')
  const [isCreatingTeam, setIsCreatingTeam] = useState(false)

  const [selectedTeamId, setSelectedTeamId] = useState(null)
  const [processingActionKey, setProcessingActionKey] = useState('')

  const [recommendationReportId, setRecommendationReportId] = useState('')
  const [recommendationLimit, setRecommendationLimit] = useState(5)
  const [recommendations, setRecommendations] = useState([])
  const [isLoadingRecommendations, setIsLoadingRecommendations] = useState(false)
  const [availableReports, setAvailableReports] = useState([])

  const [assignmentForm, setAssignmentForm] = useState(() => getDefaultAssignmentForm())
  const [assignmentError, setAssignmentError] = useState('')
  const [assignments, setAssignments] = useState([])
  const [isLoadingAssignments, setIsLoadingAssignments] = useState(false)

  const [activityForm, setActivityForm] = useState(() => getDefaultActivityForm())
  const [activityError, setActivityError] = useState('')
  const [activities, setActivities] = useState([])
  const [activitySummary, setActivitySummary] = useState(null)
  const [isLoadingActivities, setIsLoadingActivities] = useState(false)

  const selectedTeam = useMemo(
    () => teams.find((team) => team.teamId === selectedTeamId) || null,
    [teams, selectedTeamId],
  )

  
  const filtersRef = useRef({ teamTypeFilter, availabilityFilter, teamCityFilter })
  filtersRef.current = { teamTypeFilter, availabilityFilter, teamCityFilter }

const loadTeams = useCallback(
    async ({ refreshOnly = false } = {}) => {
      if (refreshOnly) {
        setIsRefreshingTeams(true)
      } else {
        setIsLoadingTeams(true)
      }

      try {
        const query = {
          ...(filtersRef.current.teamTypeFilter ? { teamType: filtersRef.current.teamTypeFilter } : {}),
          ...(filtersRef.current.availabilityFilter ? { availabilityStatus: filtersRef.current.availabilityFilter } : {}),
          ...(filtersRef.current.teamCityFilter.trim() ? { city: filtersRef.current.teamCityFilter.trim() } : {}),
        }

        const data = await getRescueTeams(query)
        setTeams(Array.isArray(data) ? data : [])
      } catch {
        setTeams([])
      } finally {
        setIsLoadingTeams(false)
        setIsRefreshingTeams(false)
      }
    },
    [],
  )

  const loadAssignments = useCallback(async () => {
    if (!selectedTeamId) {
      setAssignments([])
      return
    }

    setIsLoadingAssignments(true)

    try {
      const data = await getTeamAssignments(selectedTeamId)
      setAssignments(Array.isArray(data) ? data : [])
    } catch {
      setAssignments([])
    } finally {
      setIsLoadingAssignments(false)
    }
  }, [selectedTeamId])

  const loadActivities = useCallback(async () => {
    if (!selectedTeamId) {
      setActivities([])
      setActivitySummary(null)
      return
    }

    setIsLoadingActivities(true)

    try {
      const [items, summary] = await Promise.all([
        getTeamActivities({ teamId: selectedTeamId }),
        getTeamActivitySummary(selectedTeamId),
      ])

      setActivities(Array.isArray(items) ? items : [])
      setActivitySummary(summary)
    } catch {
      setActivities([])
      setActivitySummary(null)
    } finally {
      setIsLoadingActivities(false)
    }
  }, [selectedTeamId])

  useEffect(() => {
    async function loadReports() {
      try {
        const data = await getEmergencyReports()
        setAvailableReports(Array.isArray(data) ? data : [])
      } catch {
        setAvailableReports([])
      }
    }
    loadReports()
  }, [])

  useEffect(() => {
    loadTeams()
  }, [loadTeams])

  useEffect(() => {
    loadAssignments()
    loadActivities()
  }, [loadAssignments, loadActivities])

  function handleTeamFormChange(event) {
    const { name, value } = event.target
    setTeamForm((previous) => ({
      ...previous,
      [name]: name === 'capacity' ? Number(value) : value,
    }))
  }

  async function handleCreateTeam(event) {
    event.preventDefault()
    setTeamFormError('')

    const payload = {
      teamName: teamForm.teamName.trim(),
      teamType: teamForm.teamType,
      street: teamForm.street.trim(),
      area: teamForm.area.trim(),
      city: teamForm.city.trim(),
      province: teamForm.province.trim(),
      latitude: teamForm.latitude === '' ? null : Number(teamForm.latitude),
      longitude: teamForm.longitude === '' ? null : Number(teamForm.longitude),
      availabilityStatus: teamForm.availabilityStatus,
      capacity: Number(teamForm.capacity),
    }

    if (!payload.teamName || !payload.street || !payload.area || !payload.city || !payload.province) {
      setTeamFormError('Team name and full location fields are required.')
      return
    }

    if (!payload.capacity || payload.capacity < 1) {
      setTeamFormError('Capacity must be at least 1.')
      return
    }

    setIsCreatingTeam(true)

    try {
      await createRescueTeam(payload)
      notify({
        title: 'Rescue team created',
        message: `${payload.teamName} has been registered.`,
        variant: 'success',
      })
      setTeamForm(getDefaultTeamForm())
      await loadTeams({ refreshOnly: true })
    } catch {
      setTeamFormError('Unable to create rescue team. Check required fields and try again.')
    } finally {
      setIsCreatingTeam(false)
    }
  }

  async function handleAvailabilityUpdate(team, availabilityStatus) {
    setProcessingActionKey(`team-${team.teamId}-${availabilityStatus}`)

    try {
      await updateRescueTeamAvailability(team.teamId, {
        availabilityStatus,
        versionToken: team.versionToken,
      })

      notify({
        title: 'Availability updated',
        message: `${team.teamName} is now ${availabilityStatus}.`,
        variant: 'success',
      })

      await loadTeams({ refreshOnly: true })
    } catch (error) {
      if (error.response?.status === 409) {
        notify({
          title: 'Concurrency conflict',
          message:
            error.response?.data?.message ||
            'Team availability changed elsewhere. Team list has been refreshed.',
          variant: 'warning',
        })
        await loadTeams({ refreshOnly: true })
      }
    } finally {
      setProcessingActionKey('')
    }
  }

  function isBusy(key) {
    return processingActionKey === key
  }

  async function handleLoadRecommendations() {
    const reportId = Number(recommendationReportId)

    if (!reportId || reportId <= 0) {
      notify({
        title: 'Recommendation input needed',
        message: 'Enter a valid report ID before requesting recommendations.',
        variant: 'warning',
      })
      return
    }

    setIsLoadingRecommendations(true)

    try {
      const data = await getRescueTeamRecommendations(reportId, Number(recommendationLimit) || 5)
      setRecommendations(Array.isArray(data) ? data : [])
    } catch {
      setRecommendations([])
    } finally {
      setIsLoadingRecommendations(false)
    }
  }

  function handleAssignmentFormChange(event) {
    const { name, value, type, checked } = event.target
    setAssignmentForm((previous) => ({
      ...previous,
      [name]: type === 'checkbox' ? checked : value,
    }))
  }

  async function handleCreateAssignment(event) {
    event.preventDefault()
    setAssignmentError('')

    if (!selectedTeamId) {
      setAssignmentError('Select a team before creating an assignment.')
      return
    }

    const reportId = Number(assignmentForm.reportId)
    if (!reportId || reportId <= 0) {
      setAssignmentError('Report ID must be greater than zero.')
      return
    }

    if (!user?.userId) {
      setAssignmentError('Current user context is missing. Re-login and try again.')
      return
    }

    const targetReport = availableReports.find((report) => report.reportId === reportId)
    if (!targetReport) {
      setAssignmentError(`Emergency report #${reportId} was not found in the current list.`)
      return
    }

    if (!targetReport.city?.trim()) {
      setAssignmentError('Selected emergency report is missing city, cannot validate assignment.')
      return
    }

    if (selectedTeam && targetReport.city.trim().toLowerCase() !== selectedTeam.city.trim().toLowerCase()) {
      setAssignmentError(
        `Same City assignment check failed: team city (${selectedTeam.city}) does not match report city (${targetReport.city}).`,
      )
      return
    }

    if (selectedTeam && selectedTeam.availabilityStatus === 'Busy') {
      setAssignmentError('This team is already marked as Busy. Complete current assignments first.')
      return
    }

    if (assignmentForm.requiresApproval) {
      const approvalRequestedBy = Number(assignmentForm.approvalRequestedBy || user.userId)
      if (!approvalRequestedBy || approvalRequestedBy <= 0) {
        setAssignmentError('Approval Requested By must be a valid user ID when approval is required.')
        return
      }
    }

    setProcessingActionKey(`assignment-create-${selectedTeamId}`)

    try {
      await createTeamAssignment(selectedTeamId, {
        reportId,
        assignedBy: user.userId,
        status: assignmentForm.status,
        requiresApproval: assignmentForm.requiresApproval,
        approvalRequestedBy: assignmentForm.requiresApproval
          ? Number(assignmentForm.approvalRequestedBy || user.userId)
          : null,
      })

      notify({
        title: 'Assignment created',
        message: `Team #${selectedTeamId} assigned to report #${reportId}.`,
        variant: 'success',
      })

      setAssignmentForm(getDefaultAssignmentForm())
      await loadAssignments()
      await loadTeams({ refreshOnly: true })
    } catch {
      setAssignmentError('Assignment creation failed. Verify team/report/user IDs.')
    } finally {
      setProcessingActionKey('')
    }
  }

  async function handleAssignmentStatusUpdate(assignmentId, status) {
    if (!selectedTeamId) {
      return
    }

    setProcessingActionKey(`assignment-${assignmentId}-${status}`)

    try {
      await updateTeamAssignmentStatus(selectedTeamId, assignmentId, { status })
      notify({
        title: 'Assignment status updated',
        message: `Assignment #${assignmentId} is now ${status}.`,
        variant: 'success',
      })
      await loadAssignments()
    } finally {
      setProcessingActionKey('')
    }
  }

  function handleActivityFormChange(event) {
    const { name, value } = event.target
    setActivityForm((previous) => ({
      ...previous,
      [name]: value,
    }))
  }

  async function handleCreateActivity(event) {
    event.preventDefault()
    setActivityError('')

    if (!selectedTeamId) {
      setActivityError('Select a team before logging activity.')
      return
    }

    const payload = {
      teamId: selectedTeamId,
      activityType: activityForm.activityType.trim(),
      startTime: toIsoString(activityForm.startTime),
      endTime: activityForm.endTime ? toIsoString(activityForm.endTime) : null,
      notes: activityForm.notes.trim() || null,
      outcome: activityForm.outcome.trim() || null,
    }

    if (!payload.activityType) {
      setActivityError('Activity type is required.')
      return
    }

    if (!payload.startTime) {
      setActivityError('Start time must be valid.')
      return
    }

    if (payload.endTime && payload.endTime < payload.startTime) {
      setActivityError('End time must be greater than or equal to start time.')
      return
    }

    setProcessingActionKey(`activity-create-${selectedTeamId}`)

    try {
      await createTeamActivity(payload)
      notify({
        title: 'Activity logged',
        message: `Team activity ${payload.activityType} was recorded.`,
        variant: 'success',
      })
      setActivityForm(getDefaultActivityForm())
      await loadActivities()
    } catch {
      setActivityError('Activity logging failed. Check time fields and try again.')
    } finally {
      setProcessingActionKey('')
    }
  }

  const teamColumns = useMemo(
    () => [
      {
        key: 'teamName',
        header: 'Team',
        render: (row) => (
          <div>
            <strong>{row.teamName}</strong>
            <div className="table-subtext">#{row.teamId}</div>
          </div>
        ),
      },
      { key: 'teamType', header: 'Type' },
      {
        key: 'availabilityStatus',
        header: 'Availability',
        render: (row) => <StatusBadge label={row.availabilityStatus} status={row.availabilityStatus} />,
      },
      {
        key: 'location',
        header: 'Location',
        render: (row) => `${row.area}, ${row.city}`,
      },
      { key: 'capacity', header: 'Capacity', align: 'center' },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <div className="table-actions">
            <button
              type="button"
              className="table-action-btn"
              onClick={() => setSelectedTeamId(row.teamId)}
            >
              {selectedTeamId === row.teamId ? 'Focused' : 'Focus'}
            </button>
            <button
              type="button"
              className="table-action-btn"
              disabled={isBusy(`team-${row.teamId}-Available`) || row.availabilityStatus === 'Available'}
              onClick={() => handleAvailabilityUpdate(row, 'Available')}
            >
              Available
            </button>
            <button
              type="button"
              className="table-action-btn"
              disabled={isBusy(`team-${row.teamId}-Busy`) || row.availabilityStatus === 'Busy'}
              onClick={() => handleAvailabilityUpdate(row, 'Busy')}
            >
              Busy
            </button>
          </div>
        ),
      },
    ],
    [selectedTeamId, processingActionKey],
  )

  const recommendationColumns = useMemo(
    () => [
      {
        key: 'teamName',
        header: 'Recommended Team',
        render: (row) => `${row.teamName} (#${row.teamId})`,
      },
      { key: 'teamType', header: 'Type' },
      { key: 'priorityScore', header: 'Score' },
      {
        key: 'reason',
        header: 'Reason',
        render: (row) => row.recommendationReason,
      },
      {
        key: 'pick',
        header: 'Action',
        align: 'right',
        render: (row) => (
          <button
            type="button"
            className="table-action-btn"
            onClick={() => {
              setSelectedTeamId(row.teamId)
              setAssignmentForm((previous) => ({
                ...previous,
                reportId: recommendationReportId,
              }))
            }}
          >
            Use Team
          </button>
        ),
      },
    ],
    [recommendationReportId],
  )

  const assignmentColumns = useMemo(
    () => [
      {
        key: 'assignmentId',
        header: 'Assignment',
        render: (row) => <span className="mono-cell">#{row.assignmentId}</span>,
      },
      {
        key: 'reportId',
        header: 'Report',
        render: (row) => `#${row.reportId} (${row.reportCity || '-'})`,
      },
      {
        key: 'status',
        header: 'Status',
        render: (row) => <StatusBadge label={row.status} status={row.status} />,
      },
      {
        key: 'assignmentTime',
        header: 'Assigned At',
        render: (row) => toDisplayDateTime(row.assignmentTime),
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <div className="table-actions">
            {ASSIGNMENT_STATUSES.map((status) => (
              <button
                key={status}
                type="button"
                className="table-action-btn"
                disabled={isBusy(`assignment-${row.assignmentId}-${status}`) || row.status === status}
                onClick={() => handleAssignmentStatusUpdate(row.assignmentId, status)}
              >
                {status}
              </button>
            ))}
          </div>
        ),
      },
    ],
    [processingActionKey, selectedTeamId],
  )

  const activityColumns = useMemo(
    () => [
      { key: 'activityType', header: 'Activity' },
      {
        key: 'startTime',
        header: 'Start',
        render: (row) => toDisplayDateTime(row.startTime),
      },
      {
        key: 'endTime',
        header: 'End',
        render: (row) => toDisplayDateTime(row.endTime),
      },
      {
        key: 'durationMinutes',
        header: 'Duration',
        render: (row) => (row.durationMinutes == null ? '-' : `${row.durationMinutes} min`),
      },
      {
        key: 'outcome',
        header: 'Outcome',
        render: (row) => row.outcome || '-',
      },
    ],
    [],
  )

  if (isLoadingTeams) {
    return (
      <LoadingState
        title="Loading rescue coordination"
        message="Preparing teams, assignments, and activity workspace."
      />
    )
  }

  return (
    <div>
<AppCard
        title="Rescue Team Registry"
        subtitle="Register teams, track availability, and focus a team for assignment workflow."
        actions={
          <div className="toolbar-inline">
            <label className="toolbar-label" htmlFor="teamTypeFilter">
              Type
            </label>
            <select
              id="teamTypeFilter"
              value={teamTypeFilter}
              onChange={(event) => setTeamTypeFilter(event.target.value)}
            >
              <option value="">All</option>
              {TEAM_TYPES.map((item) => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>

            <label className="toolbar-label" htmlFor="teamAvailabilityFilter">
              Availability
            </label>
            <select
              id="teamAvailabilityFilter"
              value={availabilityFilter}
              onChange={(event) => setAvailabilityFilter(event.target.value)}
            >
              <option value="">All</option>
              {TEAM_AVAILABILITY.map((item) => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>

            <label className="toolbar-label" htmlFor="teamCityFilter">
              City
            </label>
            <input
              id="teamCityFilter"
              value={teamCityFilter}
              onChange={(event) => setTeamCityFilter(event.target.value)}
              placeholder="City"
            />

            <button
              type="button"
              className="table-action-btn"
              onClick={() => loadTeams({ refreshOnly: true })}
              disabled={isRefreshingTeams}
            >
              {isRefreshingTeams ? 'Refreshing...' : 'Refresh'}
            </button>
          </div>
        }
      >
        <DataTable
          caption="Rescue team registry"
          columns={teamColumns}
          rows={teams}
          getRowKey={(row) => row.teamId}
          emptyMessage="No rescue teams matched filters."
        />
      </AppCard>

      <AppCard title="Register Rescue Team" subtitle="Create new operational teams for incident dispatch.">
        <form className="event-create-form" onSubmit={handleCreateTeam}>
          {teamFormError ? <AlertBanner variant="warning" title="Validation check" message={teamFormError} /> : null}

          <div className="event-form-grid">
            <label>
              Team Name
              <input name="teamName" value={teamForm.teamName} onChange={handleTeamFormChange} required />
            </label>
            <label>
              Team Type
              <select name="teamType" value={teamForm.teamType} onChange={handleTeamFormChange}>
                {TEAM_TYPES.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Street
              <input name="street" value={teamForm.street} onChange={handleTeamFormChange} required />
            </label>
            <label>
              Area
              <input name="area" value={teamForm.area} onChange={handleTeamFormChange} required />
            </label>
            <label>
              City
              <input name="city" value={teamForm.city} onChange={handleTeamFormChange} required />
            </label>
            <label>
              Province
              <input name="province" value={teamForm.province} onChange={handleTeamFormChange} required />
            </label>

            <label>
              Availability
              <select
                name="availabilityStatus"
                value={teamForm.availabilityStatus}
                onChange={handleTeamFormChange}
              >
                {TEAM_AVAILABILITY.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Capacity
              <input
                type="number"
                min={1}
                name="capacity"
                value={teamForm.capacity}
                onChange={handleTeamFormChange}
                required
              />
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isCreatingTeam}>
              {isCreatingTeam ? 'Registering...' : 'Register Team'}
            </button>
          </div>
        </form>
      </AppCard>

      <AppCard
        title="Assignment Recommendations"
        subtitle="Rank candidate teams for a report and pick one into the assignment workspace."
      >
        <div className="toolbar-inline">
            <label className="toolbar-label" htmlFor="recommendationReportId">
              Report ID
            </label>
            <select
              id="recommendationReportId"
              value={recommendationReportId}
              onChange={(event) => setRecommendationReportId(event.target.value)}
            >
              <option value="">Select a Report ID</option>
              {availableReports.map((report) => (
                <option key={report.reportId} value={report.reportId}>
                  #{report.reportId} - {report.disasterType || 'Incident'} ({report.city || 'Unknown'})
                </option>
              ))}
            </select>

          <label className="toolbar-label" htmlFor="recommendationLimit">
            Limit
          </label>
          <input
            id="recommendationLimit"
            type="number"
            min={1}
            max={20}
            value={recommendationLimit}
            onChange={(event) => setRecommendationLimit(event.target.value)}
          />

          <button
            type="button"
            className="table-action-btn"
            onClick={handleLoadRecommendations}
            disabled={isLoadingRecommendations}
          >
            {isLoadingRecommendations ? 'Loading...' : 'Get Recommendations'}
          </button>
        </div>

        <DataTable
          caption="Recommendation ranking"
          columns={recommendationColumns}
          rows={recommendations}
          getRowKey={(row) => row.teamId}
          emptyMessage="No recommendations loaded yet."
        />
      </AppCard>

      <AppCard
        title="Assignment Workflow"
        subtitle="Create assignments, then progress status from Assigned through Completed."
      >
        <p className="event-form-meta">
          Focused Team:{' '}
          <span className="mono-cell">{selectedTeam ? `#${selectedTeam.teamId} ${selectedTeam.teamName}` : 'None selected'}</span>
        </p>

        <form className="event-create-form" onSubmit={handleCreateAssignment}>
          {assignmentError ? (
            <AlertBanner variant="warning" title="Assignment check" message={assignmentError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              Report ID
              <select
                name="reportId"
                value={assignmentForm.reportId}
                onChange={handleAssignmentFormChange}
                required
              >
                <option value="">Select an Emergency Report</option>
                {availableReports.map((report) => (
                  <option key={report.reportId} value={report.reportId}>
                    #{report.reportId} - {report.disasterType} ({report.city})
                  </option>
                ))}
              </select>
            </label>
            <label>
              Initial Status
              <select
                name="status"
                value={assignmentForm.status}
                onChange={handleAssignmentFormChange}
              >
                {ASSIGNMENT_STATUSES.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </label>
            <label className="form-check-row">
              <input
                type="checkbox"
                name="requiresApproval"
                checked={assignmentForm.requiresApproval}
                onChange={handleAssignmentFormChange}
              />
              Requires approval
            </label>
            <label>
              Approval Requested By (optional)
              <input
                type="number"
                min={1}
                name="approvalRequestedBy"
                value={assignmentForm.approvalRequestedBy}
                onChange={handleAssignmentFormChange}
              />
            </label>
          </div>

          <div className="event-form-actions">
            <button
              type="submit"
              className="table-action-btn"
              disabled={isBusy(`assignment-create-${selectedTeamId}`)}
            >
              {isBusy(`assignment-create-${selectedTeamId}`) ? 'Assigning...' : 'Create Assignment'}
            </button>
          </div>
        </form>

        {isLoadingAssignments ? (
          <LoadingState title="Loading assignments" message="Fetching assignment history for focused team." />
        ) : (
          <DataTable
            caption="Team assignment timeline"
            columns={assignmentColumns}
            rows={assignments}
            getRowKey={(row) => row.assignmentId}
            emptyMessage="No assignments for selected team yet."
          />
        )}
      </AppCard>

      <AppCard
        title="Team Activity Timeline"
        subtitle="Log field activities and review summary metrics for the focused team."
      >
        <p className="event-form-meta">
          Summary:{' '}
          <span className="mono-cell">
            {activitySummary
              ? `Total ${activitySummary.totalActivities}, Completed ${activitySummary.completedActivities}, Pending ${activitySummary.pendingActivities}`
              : 'No summary yet'}
          </span>
        </p>

        <form className="event-create-form" onSubmit={handleCreateActivity}>
          {activityError ? (
            <AlertBanner variant="warning" title="Activity check" message={activityError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              Activity Type
              <input
                name="activityType"
                value={activityForm.activityType}
                onChange={handleActivityFormChange}
                required
                maxLength={100}
              />
            </label>
            <label>
              Start Time
              <input
                type="datetime-local"
                name="startTime"
                value={activityForm.startTime}
                onChange={handleActivityFormChange}
                required
              />
            </label>
            <label>
              End Time
              <input
                type="datetime-local"
                name="endTime"
                value={activityForm.endTime}
                onChange={handleActivityFormChange}
              />
            </label>
            <label>
              Notes
              <input
                name="notes"
                value={activityForm.notes}
                onChange={handleActivityFormChange}
                maxLength={500}
              />
            </label>
            <label>
              Outcome
              <input
                name="outcome"
                value={activityForm.outcome}
                onChange={handleActivityFormChange}
                maxLength={500}
              />
            </label>
          </div>

          <div className="event-form-actions">
            <button
              type="submit"
              className="table-action-btn"
              disabled={isBusy(`activity-create-${selectedTeamId}`)}
            >
              {isBusy(`activity-create-${selectedTeamId}`) ? 'Saving activity...' : 'Log Activity'}
            </button>
          </div>
        </form>

        {isLoadingActivities ? (
          <LoadingState title="Loading activities" message="Refreshing timeline and summary data." />
        ) : (
          <DataTable
            caption="Team activity timeline"
            columns={activityColumns}
            rows={activities}
            getRowKey={(row) => `${row.teamId}-${row.activityId}`}
            emptyMessage="No activity entries for selected team."
          />
        )}
      </AppCard>
    </div>
  )
}
