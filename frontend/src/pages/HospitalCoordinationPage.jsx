import { useCallback, useEffect, useMemo, useState , useRef} from 'react'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useNotification } from '../context/NotificationContext'
import {
  createHospital,
  getHospitals,
  searchHospitalsBySpecialization,
  updateHospitalBeds,
} from '../services/api/hospitalPatientApi'

function getDefaultHospitalForm() {
  return {
    hospitalName: '',
    street: '',
    area: '',
    city: '',
    province: '',
    totalBeds: 0,
    availableBeds: 0,
    contactPhone: '',
    contactEmail: '',
  }
}

function formatOccupancy(value) {
  if (value === null || value === undefined) {
    return '-'
  }

  const numeric = Number(value)
  if (Number.isNaN(numeric)) {
    return '-'
  }

  return `${(numeric * 100).toFixed(1)}%`
}

function toNumericValue(value, fallback = 0) {
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : fallback
}

export function HospitalCoordinationPage() {
  const { notify } = useNotification()

  const [hospitals, setHospitals] = useState([])
  const [searchResults, setSearchResults] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [isSearching, setIsSearching] = useState(false)
  const [isCreating, setIsCreating] = useState(false)
  const [isUpdatingBeds, setIsUpdatingBeds] = useState(false)

  const [cityFilter, setCityFilter] = useState('')
  const [minAvailableBedsFilter, setMinAvailableBedsFilter] = useState('')
  const [searchSpecialization, setSearchSpecialization] = useState('Surgery')
  const [searchCity, setSearchCity] = useState('')
  const [searchBedRequirement, setSearchBedRequirement] = useState(1)

  const [hospitalForm, setHospitalForm] = useState(() => getDefaultHospitalForm())
  const [hospitalFormError, setHospitalFormError] = useState('')
  const [searchError, setSearchError] = useState('')
  const [bedUpdateError, setBedUpdateError] = useState('')

  const [bedEditForm, setBedEditForm] = useState(null)

  
  const filtersRef = useRef({ cityFilter, minAvailableBedsFilter })
  filtersRef.current = { cityFilter, minAvailableBedsFilter }

const loadHospitals = useCallback(
    async ({ refreshOnly = false } = {}) => {
      if (refreshOnly) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }

      try {
        const response = await getHospitals({
          ...(filtersRef.current.cityFilter.trim() ? { city: filtersRef.current.cityFilter.trim() } : {}),
          ...(filtersRef.current.minAvailableBedsFilter ? { minAvailableBeds: Number(filtersRef.current.minAvailableBedsFilter) } : {}),
        })

        setHospitals(Array.isArray(response) ? response : [])
      } finally {
        setIsLoading(false)
        setIsRefreshing(false)
      }
    },
    [],
  )

  useEffect(() => {
    loadHospitals()
  }, [loadHospitals])

  function handleHospitalFormChange(event) {
    const { name, value } = event.target
    setHospitalForm((previous) => ({
      ...previous,
      [name]: ['totalBeds', 'availableBeds'].includes(name) ? toNumericValue(value) : value,
    }))
  }

  async function handleCreateHospital(event) {
    event.preventDefault()
    setHospitalFormError('')

    const payload = {
      hospitalName: hospitalForm.hospitalName.trim(),
      street: hospitalForm.street.trim(),
      area: hospitalForm.area.trim(),
      city: hospitalForm.city.trim(),
      province: hospitalForm.province.trim(),
      totalBeds: toNumericValue(hospitalForm.totalBeds),
      availableBeds: toNumericValue(hospitalForm.availableBeds),
      contactPhone: hospitalForm.contactPhone.trim() || null,
      contactEmail: hospitalForm.contactEmail.trim() || null,
    }

    if (!payload.hospitalName || !payload.street || !payload.area || !payload.city || !payload.province) {
      setHospitalFormError('Hospital name and location fields are required.')
      return
    }

    if (payload.totalBeds < 1) {
      setHospitalFormError('Total beds must be at least 1.')
      return
    }

    if (payload.availableBeds < 0 || payload.availableBeds > payload.totalBeds) {
      setHospitalFormError('Available beds must be between 0 and total beds.')
      return
    }

    setIsCreating(true)

    try {
      await createHospital(payload)
      notify({
        title: 'Hospital created',
        message: `${payload.hospitalName} was added to the coordination registry.`,
        variant: 'success',
      })
      setHospitalForm(getDefaultHospitalForm())
      await loadHospitals({ refreshOnly: true })
    } catch {
      setHospitalFormError('Unable to create hospital. Verify the provided details and try again.')
    } finally {
      setIsCreating(false)
    }
  }

  function beginEditBeds(row) {
    setBedUpdateError('')
    setBedEditForm({
      hospitalId: row.hospitalId,
      hospitalName: row.hospitalName,
      totalBeds: toNumericValue(row.totalBeds),
      availableBeds: toNumericValue(row.availableBeds),
    })
  }

  async function handleUpdateBeds(event) {
    event.preventDefault()

    if (!bedEditForm) {
      return
    }

    setBedUpdateError('')

    const payload = {
      totalBeds: toNumericValue(bedEditForm.totalBeds),
      availableBeds: toNumericValue(bedEditForm.availableBeds),
    }

    if (payload.totalBeds < 1) {
      setBedUpdateError('Total beds must be at least 1.')
      return
    }

    if (payload.availableBeds < 0 || payload.availableBeds > payload.totalBeds) {
      setBedUpdateError('Available beds must be between 0 and total beds.')
      return
    }

    setIsUpdatingBeds(true)

    try {
      await updateHospitalBeds(bedEditForm.hospitalId, payload)
      notify({
        title: 'Hospital beds updated',
        message: `${bedEditForm.hospitalName} capacity was updated.`,
        variant: 'success',
      })
      setBedEditForm(null)
      await loadHospitals({ refreshOnly: true })
    } catch {
      setBedUpdateError('Unable to update bed counts. Try again after refreshing the hospital list.')
    } finally {
      setIsUpdatingBeds(false)
    }
  }

  async function handleSearchHospitals(event) {
    event.preventDefault()
    setSearchError('')

    if (!searchSpecialization.trim()) {
      setSearchError('Specialization is required to search hospitals.')
      return
    }

    setIsSearching(true)

    try {
      const response = await searchHospitalsBySpecialization({
        specialization: searchSpecialization.trim(),
        ...(searchCity.trim() ? { city: searchCity.trim() } : {}),
        bedRequirement: toNumericValue(searchBedRequirement, 1),
      })

      setSearchResults(Array.isArray(response) ? response : [])
      if (!Array.isArray(response) || response.length === 0) {
        notify({
          title: 'Search completed',
          message: 'No hospital matched the requested specialization and bed requirement.',
          variant: 'info',
        })
      }
    } catch {
      setSearchResults([])
      setSearchError('Unable to search hospitals. Verify specialization and bed requirement values.')
    } finally {
      setIsSearching(false)
    }
  }

  const hospitalColumns = useMemo(
    () => [
      {
        key: 'hospitalId',
        header: 'Hospital ID',
        render: (row) => <span className="mono-cell">#{row.hospitalId}</span>,
      },
      {
        key: 'hospitalName',
        header: 'Hospital',
        render: (row) => (
          <div>
            <strong>{row.hospitalName}</strong>
            <div className="table-subtext">{row.specializations?.join(', ') || 'No specializations'}</div>
          </div>
        ),
      },
      {
        key: 'location',
        header: 'Location',
        render: (row) => `${row.area}, ${row.city}`,
      },
      {
        key: 'beds',
        header: 'Beds',
        render: (row) => `${row.availableBeds}/${row.totalBeds}`,
      },
      {
        key: 'occupancyRate',
        header: 'Occupancy',
        render: (row) => formatOccupancy(row.occupancyRate),
      },
      {
        key: 'contact',
        header: 'Contact',
        render: (row) => row.contactPhone || row.contactEmail || '-',
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <button type="button" className="table-action-btn" onClick={() => beginEditBeds(row)}>
            Update Beds
          </button>
        ),
      },
    ],
    [],
  )

  const searchColumns = useMemo(
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
        header: 'Available Beds',
        render: (row) => <StatusBadge label={`${row.availableBeds} beds`} status="success" />,
      },
      {
        key: 'specializations',
        header: 'Specializations',
        render: (row) => row.specializations?.join(', ') || '-',
      },
    ],
    [],
  )

  if (isLoading) {
    return (
      <LoadingState
        title="Loading hospital coordination"
        message="Preparing hospital registry, search, and bed management data."
      />
    )
  }

  return (
    <div>
<AppCard
        title="Hospital Registry"
        subtitle="Review hospitals and filter by city or minimum available bed count."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="cityFilter" className="toolbar-label">
              City
            </label>
            <input
              id="cityFilter"
              placeholder="City"
              value={cityFilter}
              onChange={(event) => setCityFilter(event.target.value)}
            />

            <label htmlFor="minAvailableBedsFilter" className="toolbar-label">
              Min Beds
            </label>
            <input
              id="minAvailableBedsFilter"
              type="number"
              min={1}
              placeholder="Beds"
              value={minAvailableBedsFilter}
              onChange={(event) => setMinAvailableBedsFilter(event.target.value)}
            />

            <button
              type="button"
              className="table-action-btn"
              onClick={() => loadHospitals({ refreshOnly: true })}
              disabled={isRefreshing}
            >
              {isRefreshing ? 'Refreshing...' : 'Refresh'}
            </button>
          </div>
        }
      >
        <DataTable
          caption="Hospital registry"
          columns={hospitalColumns}
          rows={hospitals}
          getRowKey={(row) => row.hospitalId}
          emptyMessage="No hospitals matched the current filters."
        />
      </AppCard>

      <AppCard title="Create Hospital" subtitle="Register a new hospital with bed capacity and contact details.">
        <form className="event-create-form" onSubmit={handleCreateHospital}>
          {hospitalFormError ? (
            <AlertBanner variant="warning" title="Hospital validation" message={hospitalFormError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              Hospital Name
              <input name="hospitalName" value={hospitalForm.hospitalName} onChange={handleHospitalFormChange} required />
            </label>
            <label>
              Street
              <input name="street" value={hospitalForm.street} onChange={handleHospitalFormChange} required />
            </label>
            <label>
              Area
              <input name="area" value={hospitalForm.area} onChange={handleHospitalFormChange} required />
            </label>
            <label>
              City
              <input name="city" value={hospitalForm.city} onChange={handleHospitalFormChange} required />
            </label>
            <label>
              Province
              <input name="province" value={hospitalForm.province} onChange={handleHospitalFormChange} required />
            </label>
            <label>
              Total Beds
              <input type="number" min={1} name="totalBeds" value={hospitalForm.totalBeds} onChange={handleHospitalFormChange} required />
            </label>
            <label>
              Available Beds
              <input type="number" min={0} name="availableBeds" value={hospitalForm.availableBeds} onChange={handleHospitalFormChange} required />
            </label>
            <label>
              Contact Phone
              <input name="contactPhone" value={hospitalForm.contactPhone} onChange={handleHospitalFormChange} />
            </label>
            <label>
              Contact Email
              <input type="email" name="contactEmail" value={hospitalForm.contactEmail} onChange={handleHospitalFormChange} />
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isCreating}>
              {isCreating ? 'Creating...' : 'Create Hospital'}
            </button>
          </div>
        </form>
      </AppCard>

      {bedEditForm ? (
        <AppCard title={`Update Beds - ${bedEditForm.hospitalName}`} subtitle="Adjust hospital capacity and current availability.">
          <form className="event-create-form" onSubmit={handleUpdateBeds}>
            {bedUpdateError ? (
              <AlertBanner variant="warning" title="Bed update" message={bedUpdateError} />
            ) : null}

            <div className="event-form-grid">
              <label>
                Total Beds
                <input
                  type="number"
                  min={1}
                  value={bedEditForm.totalBeds}
                  onChange={(event) =>
                    setBedEditForm((previous) => ({
                      ...previous,
                      totalBeds: toNumericValue(event.target.value),
                    }))
                  }
                  required
                />
              </label>
              <label>
                Available Beds
                <input
                  type="number"
                  min={0}
                  value={bedEditForm.availableBeds}
                  onChange={(event) =>
                    setBedEditForm((previous) => ({
                      ...previous,
                      availableBeds: toNumericValue(event.target.value),
                    }))
                  }
                  required
                />
              </label>
            </div>

            <div className="event-form-actions">
              <button type="submit" className="table-action-btn" disabled={isUpdatingBeds}>
                {isUpdatingBeds ? 'Saving...' : 'Save Bed Updates'}
              </button>
              <button type="button" className="table-action-btn" onClick={() => setBedEditForm(null)}>
                Cancel
              </button>
            </div>
          </form>
        </AppCard>
      ) : null}

      <AppCard
        title="Hospital Search"
        subtitle="Find hospitals that match a specialization and bed requirement for routing decisions."
      >
        <form className="event-create-form" onSubmit={handleSearchHospitals}>
          {searchError ? (
            <AlertBanner variant="warning" title="Hospital search" message={searchError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              Specialization
              <input
                value={searchSpecialization}
                onChange={(event) => setSearchSpecialization(event.target.value)}
                placeholder="Surgery"
                required
              />
            </label>
            <label>
              City
              <input
                value={searchCity}
                onChange={(event) => setSearchCity(event.target.value)}
                placeholder="Optional city"
              />
            </label>
            <label>
              Bed Requirement
              <input
                type="number"
                min={1}
                value={searchBedRequirement}
                onChange={(event) => setSearchBedRequirement(event.target.value)}
                required
              />
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isSearching}>
              {isSearching ? 'Searching...' : 'Search Hospitals'}
            </button>
          </div>
        </form>

        <DataTable
          caption="Routing candidates"
          columns={searchColumns}
          rows={searchResults}
          getRowKey={(row) => row.hospitalId}
          emptyMessage="Run a specialization search to view routing candidates."
        />
      </AppCard>
    </div>
  )
}
