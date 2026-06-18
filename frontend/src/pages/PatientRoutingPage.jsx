import { useCallback, useEffect, useMemo, useState , useRef} from 'react'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useNotification } from '../context/NotificationContext'
import {
  autoRoutePatient,
  getAdmissions,
  getHospitals,
  getPatients,
  routePatientToHospital,
} from '../services/api/hospitalRoutingApi'

const ADMISSION_CONDITIONS = ['Critical', 'Serious', 'Stable']
const ADMISSION_STATUSES = ['Admitted', 'Discharged', 'Transferred']
function getDefaultManualRouteForm() {
  return {
    patientId: '',
    hospitalId: '',
    reportId: '',
    requiredSpecialization: '',
    admissionTime: getLocalDatetimeValue(),
    condition: 'Serious',
    status: 'Admitted',
  }
}

function getDefaultAutoRouteForm() {
  return {
    patientId: '',
    reportId: '',
    requiredSpecialization: '',
    bedRequirement: 1,
    preferredCity: '',
    preferredProvince: '',
    admissionTime: getLocalDatetimeValue(),
    condition: 'Serious',
    status: 'Admitted',
  }
}

function formatDateTime(value) {
  if (!value) {
    return '-'
  }

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return '-'
  }

  return parsed.toLocaleString()
}

function toNumericValue(value, fallback = 0) {
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : fallback
}

function getLocalDatetimeValue(date = new Date()) {
  const offsetMs = date.getTimezoneOffset() * 60 * 1000
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16)
}

export function PatientRoutingPage() {
  const { notify } = useNotification()

  const [patients, setPatients] = useState([])
  const [hospitals, setHospitals] = useState([])
  const [admissions, setAdmissions] = useState([])
  const [routingResult, setRoutingResult] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [isRoutingManual, setIsRoutingManual] = useState(false)
  const [isRoutingAuto, setIsRoutingAuto] = useState(false)

  const [patientFilter, setPatientFilter] = useState('')
  const [hospitalCityFilter, setHospitalCityFilter] = useState('')
  const [admissionPatientFilter, setAdmissionPatientFilter] = useState('')

  const [manualRouteForm, setManualRouteForm] = useState(() => getDefaultManualRouteForm())
  const [autoRouteForm, setAutoRouteForm] = useState(() => getDefaultAutoRouteForm())

  const [manualRouteError, setManualRouteError] = useState('')
  const [autoRouteError, setAutoRouteError] = useState('')

  function setManualRoutingResult(result) {
    setRoutingResult({
      kind: 'manual',
      admission: result,
    })
  }

  function setAutoRoutingResult(result) {
    setRoutingResult({
      kind: 'auto',
      result,
    })
  }

  
  const filtersRef = useRef({ patientFilter, hospitalCityFilter, admissionPatientFilter })
  filtersRef.current = { patientFilter, hospitalCityFilter, admissionPatientFilter }

const loadData = useCallback(
    async ({ refreshOnly = false } = {}) => {
      if (refreshOnly) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }

      try {
        const [patientData, hospitalData, admissionData] = await Promise.all([
          getPatients(filtersRef.current.patientFilter.trim() ? { nationalId: filtersRef.current.patientFilter.trim() } : {}),
          getHospitals(filtersRef.current.hospitalCityFilter.trim() ? { city: filtersRef.current.hospitalCityFilter.trim() } : {}),
          getAdmissions(filtersRef.current.admissionPatientFilter ? { patientId: Number(filtersRef.current.admissionPatientFilter) } : {}),
        ])

        setPatients(Array.isArray(patientData) ? patientData : [])
        setHospitals(Array.isArray(hospitalData) ? hospitalData : [])
        setAdmissions(Array.isArray(admissionData) ? admissionData : [])
      } finally {
        setIsLoading(false)
        setIsRefreshing(false)
      }
    },
    [],
  )

  useEffect(() => {
    loadData()
  }, [loadData])

  function handleManualRouteChange(event) {
    const { name, value } = event.target
    setManualRouteForm((previous) => ({
      ...previous,
      [name]: value,
    }))
  }

  function handleAutoRouteChange(event) {
    const { name, value } = event.target
    setAutoRouteForm((previous) => ({
      ...previous,
      [name]: name === 'bedRequirement' ? toNumericValue(value, 1) : value,
    }))
  }

  async function handleManualRoute(event) {
    event.preventDefault()
    setManualRouteError('')

    const payload = {
      patientId: Number(manualRouteForm.patientId),
      reportId: manualRouteForm.reportId ? Number(manualRouteForm.reportId) : null,
      requiredSpecialization: manualRouteForm.requiredSpecialization.trim(),
      admissionTime: manualRouteForm.admissionTime ? new Date(manualRouteForm.admissionTime).toISOString() : null,
      condition: manualRouteForm.condition,
      status: manualRouteForm.status,
    }

    const hospitalId = Number(manualRouteForm.hospitalId)

    if (!payload.patientId || !hospitalId || !payload.requiredSpecialization) {
      setManualRouteError('Patient ID, Hospital ID, and specialization are required.')
      return
    }

    setIsRoutingManual(true)

    try {
      const result = await routePatientToHospital(hospitalId, payload)
      setManualRoutingResult(result)
      notify({
        title: 'Patient routed',
        message: `Patient #${payload.patientId} was routed to hospital #${hospitalId}.`,
        variant: 'success',
      })
      setManualRouteForm((previous) => ({
        ...getDefaultManualRouteForm(),
        admissionTime: previous.admissionTime,
      }))
      await loadData({ refreshOnly: true })
    } catch {
      setManualRouteError('Unable to route patient manually. Verify hospital, patient, and specialization values.')
    } finally {
      setIsRoutingManual(false)
    }
  }

  async function handleAutoRoute(event) {
    event.preventDefault()
    setAutoRouteError('')

    const payload = {
      patientId: Number(autoRouteForm.patientId),
      reportId: autoRouteForm.reportId ? Number(autoRouteForm.reportId) : null,
      requiredSpecialization: autoRouteForm.requiredSpecialization.trim(),
      bedRequirement: toNumericValue(autoRouteForm.bedRequirement, 1),
      preferredCity: autoRouteForm.preferredCity.trim() || null,
      preferredProvince: autoRouteForm.preferredProvince.trim() || null,
      admissionTime: autoRouteForm.admissionTime ? new Date(autoRouteForm.admissionTime).toISOString() : null,
      condition: autoRouteForm.condition,
      status: autoRouteForm.status,
    }

    if (!payload.patientId || !payload.requiredSpecialization) {
      setAutoRouteError('Patient ID and specialization are required.')
      return
    }

    setIsRoutingAuto(true)

    try {
      const result = await autoRoutePatient(payload)
      setAutoRoutingResult(result)
      notify({
        title: result.routed ? 'Auto routing completed' : 'Routing escalation raised',
        message: result.message,
        variant: result.routed ? 'success' : 'warning',
      })
      setAutoRouteForm((previous) => ({
        ...getDefaultAutoRouteForm(),
        admissionTime: previous.admissionTime,
      }))
      await loadData({ refreshOnly: true })
    } catch {
      setAutoRouteError('Unable to auto-route patient. Verify the requested specialization and bed requirement.')
    } finally {
      setIsRoutingAuto(false)
    }
  }

  const patientColumns = useMemo(
    () => [
      {
        key: 'patientName',
        header: 'Patient',
        render: (row) => (
          <div>
            <strong>{row.firstName} {row.lastName}</strong>
            <div className="table-subtext">#{row.patientId}</div>
          </div>
        ),
      },
      {
        key: 'bloodType',
        header: 'Blood Type',
        render: (row) => row.bloodType || '-',
      },
      {
        key: 'age',
        header: 'Age',
        render: (row) => row.age ?? '-',
      },
      {
        key: 'contactPhone',
        header: 'Contact',
        render: (row) => row.contactPhone || '-',
      },
    ],
    [],
  )

  const hospitalColumns = useMemo(
    () => [
      {
        key: 'hospitalName',
        header: 'Hospital',
        render: (row) => (
          <div>
            <strong>{row.hospitalName}</strong>
            <div className="table-subtext">{row.city}, {row.province}</div>
          </div>
        ),
      },
      {
        key: 'availableBeds',
        header: 'Beds',
        render: (row) => `${row.availableBeds}/${row.totalBeds}`,
      },
      {
        key: 'specializations',
        header: 'Specializations',
        render: (row) => row.specializations?.join(', ') || '-',
      },
      {
        key: 'contact',
        header: 'Contact',
        render: (row) => row.contactPhone || row.contactEmail || '-',
      },
    ],
    [],
  )

  const admissionColumns = useMemo(
    () => [
      {
        key: 'admissionId',
        header: 'Admission',
        render: (row) => <span className="mono-cell">#{row.admissionId}</span>,
      },
      {
        key: 'patientName',
        header: 'Patient',
        render: (row) => row.patientName,
      },
      {
        key: 'hospitalName',
        header: 'Hospital',
        render: (row) => row.hospitalName,
      },
      {
        key: 'condition',
        header: 'Condition',
        render: (row) => <StatusBadge label={row.condition} status={row.condition} />,
      },
      {
        key: 'status',
        header: 'Status',
        render: (row) => <StatusBadge label={row.status} status={row.status} />,
      },
      {
        key: 'admissionTime',
        header: 'Time',
        render: (row) => formatDateTime(row.admissionTime),
      },
    ],
    [],
  )

  const routingResultBadge = !routingResult
    ? { label: 'Waiting', status: 'planned' }
    : routingResult.kind === 'manual'
      ? { label: 'Manual Routed', status: 'success' }
      : routingResult.result.routed
        ? { label: 'Routed', status: 'success' }
        : { label: 'Escalation Required', status: 'warning' }

  const routingMessage = !routingResult
    ? 'No routing operation has been executed yet.'
    : routingResult.kind === 'manual'
      ? `Admission #${routingResult.admission.admissionId} created for ${routingResult.admission.patientName} at ${routingResult.admission.hospitalName}.`
      : routingResult.result.message

  if (isLoading) {
    return (
      <LoadingState
        title="Loading patient routing"
        message="Preparing routing candidates, patients, and admissions data."
      />
    )
  }

  return (
    <div>
<AppCard
        title="Routing Result"
        subtitle="Latest routing outcome or escalation status from manual or auto routing operations."
      >
        <div className="routing-result-panel">
          <StatusBadge label={routingResultBadge.label} status={routingResultBadge.status} />
          <p className="routing-result-message">{routingMessage}</p>
          {routingResult?.kind === 'manual' ? (
            <div className="routing-result-grid">
              <div><strong>Admission</strong><span>#{routingResult.admission.admissionId}</span></div>
              <div><strong>Patient</strong><span>{routingResult.admission.patientName}</span></div>
              <div><strong>Hospital</strong><span>{routingResult.admission.hospitalName}</span></div>
              <div><strong>Condition</strong><span>{routingResult.admission.condition}</span></div>
              <div><strong>Status</strong><span>{routingResult.admission.status}</span></div>
              <div><strong>Admission Time</strong><span>{formatDateTime(routingResult.admission.admissionTime)}</span></div>
            </div>
          ) : routingResult?.kind === 'auto' ? (
            <div className="routing-result-grid">
              <div><strong>Tier</strong><span>{routingResult.result.routingTierUsed || 'N/A'}</span></div>
              <div><strong>Fallback</strong><span>{routingResult.result.fallbackApplied ? 'Yes' : 'No'}</span></div>
              <div><strong>Escalation Level</strong><span>{routingResult.result.escalationLevel || '-'}</span></div>
              <div><strong>Candidate Count</strong><span>{routingResult.result.candidateCount ?? '-'}</span></div>
              <div><strong>Selected Hospital</strong><span>{routingResult.result.selectedHospitalName || '-'}</span></div>
              <div><strong>Selected Beds</strong><span>{routingResult.result.selectedHospitalAvailableBeds ?? '-'}</span></div>
            </div>
          ) : null}
          {routingResult?.kind === 'auto' && routingResult.result.suggestedActions?.length ? (
            <ul className="routing-suggestion-list">
              {routingResult.result.suggestedActions.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          ) : null}
        </div>
      </AppCard>

      <AppCard
        title="Manual Routing"
        subtitle="Route a known patient directly to a chosen hospital and admission status."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="patientFilter" className="toolbar-label">
              National ID
            </label>
            <input
              id="patientFilter"
              value={patientFilter}
              onChange={(event) => setPatientFilter(event.target.value)}
              placeholder="Search patients"
            />
            <label htmlFor="hospitalCityFilter" className="toolbar-label">
              City
            </label>
            <input
              id="hospitalCityFilter"
              value={hospitalCityFilter}
              onChange={(event) => setHospitalCityFilter(event.target.value)}
              placeholder="Filter hospitals"
            />
            <button type="button" className="table-action-btn" onClick={() => loadData({ refreshOnly: true })} disabled={isRefreshing}>
              {isRefreshing ? 'Refreshing...' : 'Refresh'}
            </button>
          </div>
        }
      >
        <div className="routing-grid">
          <DataTable
            caption="Patient roster"
            columns={patientColumns}
            rows={patients}
            getRowKey={(row) => row.patientId}
            emptyMessage="No patients matched the filter."
          />
          <DataTable
            caption="Hospital roster"
            columns={hospitalColumns}
            rows={hospitals}
            getRowKey={(row) => row.hospitalId}
            emptyMessage="No hospitals matched the filter."
          />
        </div>

        <form className="event-create-form" onSubmit={handleManualRoute}>
          {manualRouteError ? (
            <AlertBanner variant="warning" title="Manual routing" message={manualRouteError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              Patient ID
              <input type="number" min={1} name="patientId" value={manualRouteForm.patientId} onChange={handleManualRouteChange} required />
            </label>
            <label>
              Hospital ID
              <input type="number" min={1} name="hospitalId" value={manualRouteForm.hospitalId} onChange={handleManualRouteChange} required />
            </label>
            <label>
              Report ID (optional)
              <input type="number" min={1} name="reportId" value={manualRouteForm.reportId} onChange={handleManualRouteChange} />
            </label>
            <label>
              Required Specialization
              <input name="requiredSpecialization" value={manualRouteForm.requiredSpecialization} onChange={handleManualRouteChange} placeholder="Surgery" required />
            </label>
            <label>
              Admission Time
              <input type="datetime-local" name="admissionTime" value={manualRouteForm.admissionTime} onChange={handleManualRouteChange} required />
            </label>
            <label>
              Condition
              <select name="condition" value={manualRouteForm.condition} onChange={handleManualRouteChange}>
                {ADMISSION_CONDITIONS.map((item) => (
                  <option key={item} value={item}>{item}</option>
                ))}
              </select>
            </label>
            <label>
              Status
              <select name="status" value={manualRouteForm.status} onChange={handleManualRouteChange}>
                {ADMISSION_STATUSES.map((item) => (
                  <option key={item} value={item}>{item}</option>
                ))}
              </select>
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isRoutingManual}>
              {isRoutingManual ? 'Routing...' : 'Route Patient'}
            </button>
          </div>
        </form>
      </AppCard>

      <AppCard
        title="Auto Routing"
        subtitle="Use routing rules to choose the best hospital and surface escalation details when needed."
      >
        <form className="event-create-form" onSubmit={handleAutoRoute}>
          {autoRouteError ? (
            <AlertBanner variant="warning" title="Auto routing" message={autoRouteError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              Patient ID
              <input type="number" min={1} name="patientId" value={autoRouteForm.patientId} onChange={handleAutoRouteChange} required />
            </label>
            <label>
              Report ID (optional)
              <input type="number" min={1} name="reportId" value={autoRouteForm.reportId} onChange={handleAutoRouteChange} />
            </label>
            <label>
              Required Specialization
              <input name="requiredSpecialization" value={autoRouteForm.requiredSpecialization} onChange={handleAutoRouteChange} placeholder="Surgery" required />
            </label>
            <label>
              Bed Requirement
              <input type="number" min={1} name="bedRequirement" value={autoRouteForm.bedRequirement} onChange={handleAutoRouteChange} required />
            </label>
            <label>
              Preferred City
              <input name="preferredCity" value={autoRouteForm.preferredCity} onChange={handleAutoRouteChange} />
            </label>
            <label>
              Preferred Province
              <input name="preferredProvince" value={autoRouteForm.preferredProvince} onChange={handleAutoRouteChange} />
            </label>
            <label>
              Admission Time
              <input type="datetime-local" name="admissionTime" value={autoRouteForm.admissionTime} onChange={handleAutoRouteChange} />
            </label>
            <label>
              Condition
              <select name="condition" value={autoRouteForm.condition} onChange={handleAutoRouteChange}>
                {ADMISSION_CONDITIONS.map((item) => (
                  <option key={item} value={item}>{item}</option>
                ))}
              </select>
            </label>
            <label>
              Status
              <select name="status" value={autoRouteForm.status} onChange={handleAutoRouteChange}>
                {ADMISSION_STATUSES.map((item) => (
                  <option key={item} value={item}>{item}</option>
                ))}
              </select>
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isRoutingAuto}>
              {isRoutingAuto ? 'Routing...' : 'Auto Route Patient'}
            </button>
          </div>
        </form>

        <AppCard title="Recent Admissions" subtitle="Snapshot of admissions that can be used to verify routed outcomes.">
          <DataTable
            caption="Recent admissions"
            columns={admissionColumns}
            rows={admissions}
            getRowKey={(row) => row.admissionId}
            emptyMessage="No admissions matched the current filter."
          />
        </AppCard>
      </AppCard>
    </div>
  )
}
