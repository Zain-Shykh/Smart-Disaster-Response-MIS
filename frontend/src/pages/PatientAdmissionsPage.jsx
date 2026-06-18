import { useCallback, useEffect, useMemo, useState , useRef} from 'react'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useNotification } from '../context/NotificationContext'
import {
  createAdmission,
  createPatient,
  getAdmissions,
  getPatients,
  updateAdmissionStatus,
} from '../services/api/hospitalPatientWorkflowsApi'
import { getHospitals } from '../services/api/hospitalPatientApi'

const ADMISSION_CONDITIONS = ['Critical', 'Serious', 'Stable']
const ADMISSION_STATUSES = ['Admitted', 'Discharged', 'Transferred']
const BLOOD_TYPES = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-']

function getDefaultPatientForm() {
  return {
    firstName: '',
    lastName: '',
    age: '',
    gender: '',
    nationalId: '',
    bloodType: '',
    contactPhone: '',
  }
}

function getDefaultAdmissionForm() {
  return {
    patientId: '',
    hospitalId: '',
    reportId: '',
    admissionTime: new Date().toISOString().slice(0, 16),
    dischargeTime: '',
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

export function PatientAdmissionsPage() {
  const { notify } = useNotification()

  const [patients, setPatients] = useState([])
  const [hospitals, setHospitals] = useState([])
  const [admissions, setAdmissions] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [isCreatingPatient, setIsCreatingPatient] = useState(false)
  const [isCreatingAdmission, setIsCreatingAdmission] = useState(false)
  const [admissionActionKey, setAdmissionActionKey] = useState('')

  const [patientFilter, setPatientFilter] = useState('')
  const [admissionHospitalFilter, setAdmissionHospitalFilter] = useState('')
  const [admissionPatientFilter, setAdmissionPatientFilter] = useState('')
  const [admissionStatusFilter, setAdmissionStatusFilter] = useState('')

  const [patientForm, setPatientForm] = useState(() => getDefaultPatientForm())
  const [admissionForm, setAdmissionForm] = useState(() => getDefaultAdmissionForm())

  const [patientFormError, setPatientFormError] = useState('')
  const [admissionFormError, setAdmissionFormError] = useState('')

  
  const filtersRef = useRef({ patientFilter, admissionHospitalFilter, admissionPatientFilter, admissionStatusFilter })
  filtersRef.current = { patientFilter, admissionHospitalFilter, admissionPatientFilter, admissionStatusFilter }

const loadData = useCallback(
    async ({ refreshOnly = false } = {}) => {
      if (refreshOnly) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }

      try {
        const [patientData, admissionData, hospitalData] = await Promise.all([
          getPatients(filtersRef.current.patientFilter.trim() ? { nationalId: filtersRef.current.patientFilter.trim() } : {}),
          getAdmissions({
            ...(filtersRef.current.admissionHospitalFilter ? { hospitalId: Number(filtersRef.current.admissionHospitalFilter) } : {}),
            ...(filtersRef.current.admissionPatientFilter ? { patientId: Number(filtersRef.current.admissionPatientFilter) } : {}),
            ...(filtersRef.current.admissionStatusFilter ? { status: filtersRef.current.admissionStatusFilter } : {}),
          }),
          getHospitals(),
        ])

        setPatients(Array.isArray(patientData) ? patientData : [])
        setAdmissions(Array.isArray(admissionData) ? admissionData : [])
        setHospitals(Array.isArray(hospitalData) ? hospitalData : [])
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

  function handlePatientFormChange(event) {
    const { name, value } = event.target
    setPatientForm((previous) => ({
      ...previous,
      [name]: name === 'age' ? value : value,
    }))
  }

  function handleAdmissionFormChange(event) {
    const { name, value } = event.target
    setAdmissionForm((previous) => ({
      ...previous,
      [name]: name === 'patientId' || name === 'hospitalId' || name === 'reportId' ? value : value,
    }))
  }

  async function handleCreatePatient(event) {
    event.preventDefault()
    setPatientFormError('')

    const payload = {
      firstName: patientForm.firstName.trim(),
      lastName: patientForm.lastName.trim(),
      age: patientForm.age === '' ? null : toNumericValue(patientForm.age),
      gender: patientForm.gender.trim() || null,
      nationalId: patientForm.nationalId.trim() || null,
      bloodType: patientForm.bloodType || null,
      contactPhone: patientForm.contactPhone.trim() || null,
    }

    if (!payload.firstName || !payload.lastName) {
      setPatientFormError('First and last name are required.')
      return
    }

    if (payload.age !== null && (payload.age < 0 || payload.age > 150)) {
      setPatientFormError('Age must be between 0 and 150.')
      return
    }

    setIsCreatingPatient(true)

    try {
      await createPatient(payload)
      notify({
        title: 'Patient created',
        message: `${payload.firstName} ${payload.lastName} was added to the patient registry.`,
        variant: 'success',
      })
      setPatientForm(getDefaultPatientForm())
      await loadData({ refreshOnly: true })
    } catch {
      setPatientFormError('Unable to create patient. Verify the provided details and try again.')
    } finally {
      setIsCreatingPatient(false)
    }
  }

  async function handleCreateAdmission(event) {
    event.preventDefault()
    setAdmissionFormError('')

    const payload = {
      patientId: Number(admissionForm.patientId),
      hospitalId: Number(admissionForm.hospitalId),
      reportId: admissionForm.reportId ? Number(admissionForm.reportId) : null,
      admissionTime: admissionForm.admissionTime ? new Date(admissionForm.admissionTime).toISOString() : new Date().toISOString(),
      dischargeTime: admissionForm.dischargeTime ? new Date(admissionForm.dischargeTime).toISOString() : null,
      condition: admissionForm.condition,
      status: admissionForm.status,
    }

    if (!payload.patientId || !payload.hospitalId) {
      setAdmissionFormError('Patient ID and Hospital ID are required.')
      return
    }

    setIsCreatingAdmission(true)

    try {
      await createAdmission(payload)
      notify({
        title: 'Admission created',
        message: `Admission request for patient #${payload.patientId} was saved.`,
        variant: 'success',
      })
      setAdmissionForm(getDefaultAdmissionForm())
      await loadData({ refreshOnly: true })
    } catch {
      setAdmissionFormError('Unable to create admission. Verify patient, hospital, and date values.')
    } finally {
      setIsCreatingAdmission(false)
    }
  }

  function isAdmissionActionBusy(admissionId, status) {
    return admissionActionKey === `${admissionId}-${status}`
  }

  async function handleUpdateAdmissionStatus(row, status) {
    setAdmissionActionKey(`${row.admissionId}-${status}`)

    try {
      await updateAdmissionStatus(row.admissionId, { status })
      notify({
        title: 'Admission status updated',
        message: `Admission #${row.admissionId} is now ${status}.`,
        variant: 'success',
      })
      await loadData({ refreshOnly: true })
    } catch {
      // Global ProblemDetails notifications surface the error.
    } finally {
      setAdmissionActionKey('')
    }
  }

  const patientColumns = useMemo(
    () => [
      {
        key: 'patientId',
        header: 'Patient ID',
        render: (row) => <span className="mono-cell">#{row.patientId}</span>,
      },
      {
        key: 'patientName',
        header: 'Patient',
        render: (row) => (
          <div>
            <strong>{row.firstName} {row.lastName}</strong>
            <div className="table-subtext">{row.gender || 'Gender not set'}</div>
          </div>
        ),
      },
      {
        key: 'age',
        header: 'Age',
        render: (row) => row.age ?? '-',
      },
      {
        key: 'bloodType',
        header: 'Blood Type',
        render: (row) => row.bloodType || '-',
      },
      {
        key: 'nationalId',
        header: 'National ID',
        render: (row) => row.nationalId || '-',
      },
      {
        key: 'contactPhone',
        header: 'Contact',
        render: (row) => row.contactPhone || '-',
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
        render: (row) => (
          <div>
            <strong>{row.patientName}</strong>
            <div className="table-subtext">#{row.patientId}</div>
          </div>
        ),
      },
      {
        key: 'hospitalName',
        header: 'Hospital',
        render: (row) => (
          <div>
            <strong>{row.hospitalName}</strong>
            <div className="table-subtext">#{row.hospitalId}</div>
          </div>
        ),
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
        header: 'Admission Time',
        render: (row) => formatDateTime(row.admissionTime),
      },
      {
        key: 'lengthOfStayHours',
        header: 'Stay (hrs)',
        render: (row) => row.lengthOfStayHours ?? '-',
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <div className="table-actions">
            {ADMISSION_STATUSES.map((status) => (
              <button
                key={status}
                type="button"
                className="table-action-btn"
                disabled={isAdmissionActionBusy(row.admissionId, status) || row.status === status}
                onClick={() => handleUpdateAdmissionStatus(row, status)}
              >
                {status}
              </button>
            ))}
          </div>
        ),
      },
    ],
    [admissionActionKey],
  )

  if (isLoading) {
    return (
      <LoadingState
        title="Loading patient admissions"
        message="Preparing patient and admission workflows from backend services."
      />
    )
  }

  return (
    <div>
<AppCard
        title="Patient Registry"
        subtitle="Create and search patients used in hospital admissions workflows."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="patientFilter" className="toolbar-label">
              National ID
            </label>
            <input
              id="patientFilter"
              placeholder="Search by national ID"
              value={patientFilter}
              onChange={(event) => setPatientFilter(event.target.value)}
            />
            <button
              type="button"
              className="table-action-btn"
              onClick={() => loadData({ refreshOnly: true })}
              disabled={isRefreshing}
            >
              {isRefreshing ? 'Refreshing...' : 'Refresh'}
            </button>
          </div>
        }
      >
        <DataTable
          caption="Patient registry"
          columns={patientColumns}
          rows={patients}
          getRowKey={(row) => row.patientId}
          emptyMessage="No patients matched the current filter."
        />
      </AppCard>

      <AppCard title="Create Patient" subtitle="Register a patient before creating admissions or routing requests.">
        <form className="event-create-form" onSubmit={handleCreatePatient}>
          {patientFormError ? (
            <AlertBanner variant="warning" title="Patient validation" message={patientFormError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              First Name
              <input name="firstName" value={patientForm.firstName} onChange={handlePatientFormChange} required />
            </label>
            <label>
              Last Name
              <input name="lastName" value={patientForm.lastName} onChange={handlePatientFormChange} required />
            </label>
            <label>
              Age
              <input type="number" min={0} max={150} name="age" value={patientForm.age} onChange={handlePatientFormChange} />
            </label>
            <label>
              Gender
              <input name="gender" value={patientForm.gender} onChange={handlePatientFormChange} />
            </label>
            <label>
              National ID
              <input name="nationalId" value={patientForm.nationalId} onChange={handlePatientFormChange} />
            </label>
            <label>
              Blood Type
              <select name="bloodType" value={patientForm.bloodType} onChange={handlePatientFormChange}>
                <option value="">Not set</option>
                {BLOOD_TYPES.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Contact Phone
              <input name="contactPhone" value={patientForm.contactPhone} onChange={handlePatientFormChange} />
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isCreatingPatient}>
              {isCreatingPatient ? 'Creating...' : 'Create Patient'}
            </button>
          </div>
        </form>
      </AppCard>

      <AppCard
        title="Admissions Board"
        subtitle="Filter admissions by hospital, patient, or status, then progress the admission lifecycle."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="admissionHospitalFilter" className="toolbar-label">
              Hospital
            </label>
            <input
              id="admissionHospitalFilter"
              type="number"
              min={1}
              placeholder="Hospital ID"
              value={admissionHospitalFilter}
              onChange={(event) => setAdmissionHospitalFilter(event.target.value)}
            />

            <label htmlFor="admissionPatientFilter" className="toolbar-label">
              Patient
            </label>
            <input
              id="admissionPatientFilter"
              type="number"
              min={1}
              placeholder="Patient ID"
              value={admissionPatientFilter}
              onChange={(event) => setAdmissionPatientFilter(event.target.value)}
            />

            <label htmlFor="admissionStatusFilter" className="toolbar-label">
              Status
            </label>
            <select
              id="admissionStatusFilter"
              value={admissionStatusFilter}
              onChange={(event) => setAdmissionStatusFilter(event.target.value)}
            >
              <option value="">All</option>
              {ADMISSION_STATUSES.map((item) => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>

            <button
              type="button"
              className="table-action-btn"
              onClick={() => loadData({ refreshOnly: true })}
              disabled={isRefreshing}
            >
              {isRefreshing ? 'Refreshing...' : 'Refresh'}
            </button>
          </div>
        }
      >
        <DataTable
          caption="Admission listing"
          columns={admissionColumns}
          rows={admissions}
          getRowKey={(row) => row.admissionId}
          emptyMessage="No admissions matched the current filters."
        />
      </AppCard>

      <AppCard title="Create Admission" subtitle="Register a patient admission against a hospital and optional report.">
        <form className="event-create-form" onSubmit={handleCreateAdmission}>
          {admissionFormError ? (
            <AlertBanner variant="warning" title="Admission validation" message={admissionFormError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              Patient
              <select name="patientId" value={admissionForm.patientId} onChange={handleAdmissionFormChange} required>
                <option value="">Select a patient</option>
                {patients.map((p) => (
                  <option key={p.patientId} value={p.patientId}>
                    #{p.patientId} - {p.firstName} {p.lastName}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Hospital
              <select name="hospitalId" value={admissionForm.hospitalId} onChange={handleAdmissionFormChange} required>
                <option value="">Select a hospital</option>
                {hospitals.map((h) => (
                  <option key={h.hospitalId} value={h.hospitalId}>
                    #{h.hospitalId} - {h.hospitalName} ({h.city})
                  </option>
                ))}
              </select>
            </label>
            <label>
              Report ID (optional)
              <input type="number" min={1} name="reportId" value={admissionForm.reportId} onChange={handleAdmissionFormChange} />
            </label>
            <label>
              Admission Time
              <input type="datetime-local" name="admissionTime" value={admissionForm.admissionTime} onChange={handleAdmissionFormChange} required />
            </label>
            <label>
              Discharge Time (optional)
              <input type="datetime-local" name="dischargeTime" value={admissionForm.dischargeTime} onChange={handleAdmissionFormChange} />
            </label>
            <label>
              Condition
              <select name="condition" value={admissionForm.condition} onChange={handleAdmissionFormChange}>
                {ADMISSION_CONDITIONS.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Status
              <select name="status" value={admissionForm.status} onChange={handleAdmissionFormChange}>
                {ADMISSION_STATUSES.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isCreatingAdmission}>
              {isCreatingAdmission ? 'Creating...' : 'Create Admission'}
            </button>
          </div>
        </form>
      </AppCard>
    </div>
  )
}
