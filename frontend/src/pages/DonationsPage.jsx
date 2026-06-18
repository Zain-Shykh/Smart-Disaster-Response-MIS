import { useCallback, useEffect, useMemo, useState , useRef} from 'react'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useNotification } from '../context/NotificationContext'
import {
  hasTrimmedText,
  isPositiveDecimalString,
  isPositiveIntegerString,
} from '../utils/formGuards'
import {
  addDonorPhone,
  createDonation,
  createDonor,
  deleteDonorPhone,
  getDonations,
  getDonors,
  getDonorPhones,
  updateDonorPhone,
  updateDonationStatus,
} from '../services/api/donationFinanceApi'

const DONOR_TYPES = ['Individual', 'Organization']
const DONATION_STATUSES = ['Pending', 'Confirmed', 'Rejected']
const PAYMENT_METHODS = ['Cash', 'BankTransfer', 'Online']

function getDefaultDonorForm() {
  return {
    firstName: '',
    lastName: '',
    donorType: 'Individual',
    organizationName: '',
    email: '',
    street: '',
    area: '',
    city: '',
    province: '',
  }
}

function getDefaultDonationForm() {
  return {
    donorId: '',
    eventId: '',
    amount: '',
    donationDate: new Date().toISOString().slice(0, 16),
    paymentMethod: 'Cash',
    status: 'Pending',
    receiptNumber: '',
  }
}

function getDefaultDonorPhoneForm() {
  return {
    currentPhone: '',
    newPhoneNumber: '',
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

export function DonationsPage() {
  const { notify } = useNotification()

  const [donors, setDonors] = useState([])
  const [donations, setDonations] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [isCreatingDonor, setIsCreatingDonor] = useState(false)
  const [isCreatingDonation, setIsCreatingDonation] = useState(false)
  const [donationActionKey, setDonationActionKey] = useState('')
  const [donorPhoneActionKey, setDonorPhoneActionKey] = useState('')
  const [isSubmittingDonorPhone, setIsSubmittingDonorPhone] = useState(false)

  const [donorTypeFilter, setDonorTypeFilter] = useState('')
  const [donationDonorFilter, setDonationDonorFilter] = useState('')
  const [donationEventFilter, setDonationEventFilter] = useState('')
  const [donationStatusFilter, setDonationStatusFilter] = useState('')

  const [donorForm, setDonorForm] = useState(() => getDefaultDonorForm())
  const [donationForm, setDonationForm] = useState(() => getDefaultDonationForm())
  const [donorPhones, setDonorPhones] = useState([])
  const [selectedDonorId, setSelectedDonorId] = useState('')
  const [donorPhoneForm, setDonorPhoneForm] = useState(() => getDefaultDonorPhoneForm())

  const [donorFormError, setDonorFormError] = useState('')
  const [donationFormError, setDonationFormError] = useState('')
  const [donorPhoneFormError, setDonorPhoneFormError] = useState('')

  const canCreateDonor =
    hasTrimmedText(donorForm.firstName) &&
    hasTrimmedText(donorForm.lastName) &&
    hasTrimmedText(donorForm.street) &&
    hasTrimmedText(donorForm.area) &&
    hasTrimmedText(donorForm.city) &&
    hasTrimmedText(donorForm.province) &&
    (donorForm.donorType !== 'Organization' || hasTrimmedText(donorForm.organizationName))

  const canCreateDonation =
    isPositiveIntegerString(donationForm.donorId) &&
    isPositiveIntegerString(donationForm.eventId) &&
    isPositiveDecimalString(donationForm.amount)

  const canAddDonorPhone = selectedDonorId && hasTrimmedText(donorPhoneForm.newPhoneNumber)
  const canUpdateDonorPhone =
    selectedDonorId && hasTrimmedText(donorPhoneForm.currentPhone) && hasTrimmedText(donorPhoneForm.newPhoneNumber)

  
  const filtersRef = useRef({ donorTypeFilter, donationDonorFilter, donationEventFilter, donationStatusFilter })
  filtersRef.current = { donorTypeFilter, donationDonorFilter, donationEventFilter, donationStatusFilter }

const loadData = useCallback(
    async ({ refreshOnly = false } = {}) => {
      if (refreshOnly) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }

      try {
        const [donorData, donationData] = await Promise.all([
          getDonors(filtersRef.current.donorTypeFilter ? { donorType: filtersRef.current.donorTypeFilter } : {}),
          getDonations({
            ...(filtersRef.current.donationDonorFilter ? { donorId: Number(filtersRef.current.donationDonorFilter) } : {}),
            ...(filtersRef.current.donationEventFilter ? { eventId: Number(filtersRef.current.donationEventFilter) } : {}),
            ...(filtersRef.current.donationStatusFilter ? { status: filtersRef.current.donationStatusFilter } : {}),
          }),
        ])

        setDonors(Array.isArray(donorData) ? donorData : [])
        setDonations(Array.isArray(donationData) ? donationData : [])

        if (!selectedDonorId && Array.isArray(donorData) && donorData.length > 0) {
          setSelectedDonorId(String(donorData[0].donorId))
        }
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

  useEffect(() => {
    async function loadDonorPhones() {
      if (!selectedDonorId) {
        setDonorPhones([])
        return
      }

      const phones = await getDonorPhones(Number(selectedDonorId))
      setDonorPhones(Array.isArray(phones) ? phones : [])
    }

    loadDonorPhones()
  }, [selectedDonorId])

  function handleDonorFormChange(event) {
    const { name, value } = event.target
    setDonorForm((previous) => ({
      ...previous,
      [name]: value,
    }))
  }

  function handleDonationFormChange(event) {
    const { name, value } = event.target
    setDonationForm((previous) => ({
      ...previous,
      [name]: value,
    }))
  }

  function handleDonorPhoneFormChange(event) {
    const { name, value } = event.target
    setDonorPhoneForm((previous) => ({
      ...previous,
      [name]: value,
    }))
  }

  async function handleCreateDonor(event) {
    event.preventDefault()
    setDonorFormError('')

    if (!canCreateDonor) {
      setDonorFormError('Complete the donor name and address fields before submitting.')
      return
    }

    const payload = {
      firstName: donorForm.firstName.trim(),
      lastName: donorForm.lastName.trim(),
      donorType: donorForm.donorType,
      organizationName: donorForm.organizationName.trim() || null,
      email: donorForm.email.trim() || null,
      street: donorForm.street.trim(),
      area: donorForm.area.trim(),
      city: donorForm.city.trim(),
      province: donorForm.province.trim(),
    }

    if (!payload.firstName || !payload.lastName || !payload.street || !payload.area || !payload.city || !payload.province) {
      setDonorFormError('Donor name and address fields are required.')
      return
    }

    if (payload.donorType === 'Organization' && !payload.organizationName) {
      setDonorFormError('Organization name is required for organization donors.')
      return
    }

    setIsCreatingDonor(true)

    try {
      await createDonor(payload)
      notify({
        title: 'Donor created',
        message: `${payload.firstName} ${payload.lastName} was added to the donor registry.`,
        variant: 'success',
      })
      setDonorForm(getDefaultDonorForm())
      await loadData({ refreshOnly: true })
      if (!selectedDonorId && donors.length === 0) {
        setSelectedDonorId('')
      }
    } catch {
      setDonorFormError('Unable to create donor. Verify donor type and contact details.')
    } finally {
      setIsCreatingDonor(false)
    }
  }

  async function handleCreateDonation(event) {
    event.preventDefault()
    setDonationFormError('')

    if (!canCreateDonation) {
      setDonationFormError('Enter a valid donor ID, event ID, and donation amount before submitting.')
      return
    }

    const payload = {
      donorId: Number(donationForm.donorId),
      eventId: Number(donationForm.eventId),
      amount: toNumericValue(donationForm.amount),
      donationDate: donationForm.donationDate ? new Date(donationForm.donationDate).toISOString() : null,
      paymentMethod: donationForm.paymentMethod,
      status: donationForm.status,
      receiptNumber: donationForm.receiptNumber.trim() || null,
    }

    if (!payload.donorId || !payload.eventId) {
      setDonationFormError('Donor ID and Event ID are required.')
      return
    }

    if (payload.amount <= 0) {
      setDonationFormError('Donation amount must be greater than zero.')
      return
    }

    setIsCreatingDonation(true)

    try {
      await createDonation(payload)
      notify({
        title: 'Donation created',
        message: `Donation for donor #${payload.donorId} was recorded.`,
        variant: 'success',
      })
      setDonationForm(getDefaultDonationForm())
      await loadData({ refreshOnly: true })
    } catch {
      setDonationFormError('Unable to create donation. Verify donor, event, and amount values.')
    } finally {
      setIsCreatingDonation(false)
    }
  }

  function isDonationActionBusy(donationId, status) {
    return donationActionKey === `${donationId}-${status}`
  }

  async function handleUpdateDonationStatus(row, status) {
    setDonationActionKey(`${row.donationId}-${status}`)

    try {
      await updateDonationStatus(row.donationId, { status })
      notify({
        title: 'Donation status updated',
        message: `Donation #${row.donationId} is now ${status}.`,
        variant: 'success',
      })
      await loadData({ refreshOnly: true })
    } catch {
      // Global ProblemDetails notifications surface the error.
    } finally {
      setDonationActionKey('')
    }
  }

  function isDonorPhoneActionBusy(phoneNumber, action) {
    return donorPhoneActionKey === `${phoneNumber}-${action}`
  }

  async function reloadSelectedDonorPhones() {
    if (!selectedDonorId) {
      setDonorPhones([])
      return
    }

    const phones = await getDonorPhones(Number(selectedDonorId))
    setDonorPhones(Array.isArray(phones) ? phones : [])
  }

  async function handleAddDonorPhone(event) {
    event.preventDefault()
    setDonorPhoneFormError('')

    if (!selectedDonorId) {
      setDonorPhoneFormError('Select a donor first.')
      return
    }

    const payload = {
      phoneNumber: donorPhoneForm.newPhoneNumber.trim(),
    }

    if (!payload.phoneNumber) {
      setDonorPhoneFormError('Phone number is required.')
      return
    }

    setIsSubmittingDonorPhone(true)

    try {
      await addDonorPhone(Number(selectedDonorId), payload)
      notify({
        title: 'Donor phone added',
        message: `Phone ${payload.phoneNumber} was added for donor #${selectedDonorId}.`,
        variant: 'success',
      })
      setDonorPhoneForm(getDefaultDonorPhoneForm())
      await reloadSelectedDonorPhones()
    } catch {
      setDonorPhoneFormError('Unable to add donor phone. Verify format and uniqueness for this donor.')
    } finally {
      setIsSubmittingDonorPhone(false)
    }
  }

  async function handleUpdateDonorPhone(event) {
    event.preventDefault()
    setDonorPhoneFormError('')

    if (!selectedDonorId) {
      setDonorPhoneFormError('Select a donor first.')
      return
    }

    const currentPhone = donorPhoneForm.currentPhone.trim()
    const payload = {
      newPhoneNumber: donorPhoneForm.newPhoneNumber.trim(),
    }

    if (!currentPhone || !payload.newPhoneNumber) {
      setDonorPhoneFormError('Select an existing phone and provide the replacement phone number.')
      return
    }

    setDonorPhoneActionKey(`${currentPhone}-update`)

    try {
      await updateDonorPhone(Number(selectedDonorId), currentPhone, payload)
      notify({
        title: 'Donor phone updated',
        message: `Phone ${currentPhone} was updated for donor #${selectedDonorId}.`,
        variant: 'success',
      })
      setDonorPhoneForm(getDefaultDonorPhoneForm())
      await reloadSelectedDonorPhones()
    } catch {
      setDonorPhoneFormError('Unable to update donor phone. Verify format and uniqueness for this donor.')
    } finally {
      setDonorPhoneActionKey('')
    }
  }

  async function handleDeleteDonorPhone(phoneNumber) {
    if (!selectedDonorId) {
      return
    }

    setDonorPhoneActionKey(`${phoneNumber}-delete`)

    try {
      await deleteDonorPhone(Number(selectedDonorId), phoneNumber)
      notify({
        title: 'Donor phone removed',
        message: `Phone ${phoneNumber} was removed from donor #${selectedDonorId}.`,
        variant: 'success',
      })
      if (donorPhoneForm.currentPhone === phoneNumber) {
        setDonorPhoneForm(getDefaultDonorPhoneForm())
      }
      await reloadSelectedDonorPhones()
    } catch {
      // Global ProblemDetails notifications surface details.
    } finally {
      setDonorPhoneActionKey('')
    }
  }

  const donorColumns = useMemo(
    () => [
      {
        key: 'donorName',
        header: 'Donor',
        render: (row) => (
          <div>
            <strong>{row.firstName} {row.lastName}</strong>
            <div className="table-subtext">#{row.donorId}</div>
          </div>
        ),
      },
      {
        key: 'donorType',
        header: 'Type',
        render: (row) => <StatusBadge label={row.donorType} status="active" />,
      },
      {
        key: 'organizationName',
        header: 'Organization',
        render: (row) => row.organizationName || '-',
      },
      {
        key: 'email',
        header: 'Email',
        render: (row) => row.email || '-',
      },
      {
        key: 'location',
        header: 'Location',
        render: (row) => `${row.city}, ${row.province}`,
      },
    ],
    [],
  )

  const donationColumns = useMemo(
    () => [
      {
        key: 'donationId',
        header: 'Donation',
        render: (row) => <span className="mono-cell">#{row.donationId}</span>,
      },
      {
        key: 'donorName',
        header: 'Donor',
        render: (row) => (
          <div>
            <strong>{row.donorName}</strong>
            <div className="table-subtext">#{row.donorId}</div>
          </div>
        ),
      },
      {
        key: 'eventName',
        header: 'Event',
        render: (row) => (
          <div>
            <strong>{row.eventName}</strong>
            <div className="table-subtext">#{row.eventId}</div>
          </div>
        ),
      },
      {
        key: 'amount',
        header: 'Amount',
        render: (row) => row.amount,
      },
      {
        key: 'paymentMethod',
        header: 'Payment Method',
        render: (row) => row.paymentMethod,
      },
      {
        key: 'status',
        header: 'Status',
        render: (row) => <StatusBadge label={row.status} status={row.status} />,
      },
      {
        key: 'donationDate',
        header: 'Date',
        render: (row) => formatDateTime(row.donationDate),
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <div className="table-actions">
            {DONATION_STATUSES.map((status) => (
              <button
                key={status}
                type="button"
                className="table-action-btn"
                disabled={isDonationActionBusy(row.donationId, status) || row.status === status}
                onClick={() => handleUpdateDonationStatus(row, status)}
              >
                {status}
              </button>
            ))}
          </div>
        ),
      },
    ],
    [donationActionKey],
  )

  const donorPhoneColumns = useMemo(
    () => [
      {
        key: 'phoneNumber',
        header: 'Phone Number',
        render: (row) => <span className="mono-cell">{row.phoneNumber}</span>,
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
              onClick={() => setDonorPhoneForm({
                currentPhone: row.phoneNumber,
                newPhoneNumber: row.phoneNumber,
              })}
            >
              Select
            </button>
            <button
              type="button"
              className="table-action-btn"
              disabled={isDonorPhoneActionBusy(row.phoneNumber, 'delete')}
              onClick={() => handleDeleteDonorPhone(row.phoneNumber)}
            >
              Remove
            </button>
          </div>
        ),
      },
    ],
    [donorPhoneActionKey, donorPhoneForm.currentPhone],
  )

  if (isLoading) {
    return (
      <LoadingState
        title="Loading donations and donors"
        message="Preparing donor and donation workflows from backend services."
      />
    )
  }

  return (
    <div>
<AppCard
        title="Donor Registry"
        subtitle="Review donors and filter by donor type."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="donorTypeFilter" className="toolbar-label">
              Donor Type
            </label>
            <select
              id="donorTypeFilter"
              value={donorTypeFilter}
              onChange={(event) => setDonorTypeFilter(event.target.value)}
            >
              <option value="">All</option>
              {DONOR_TYPES.map((item) => (
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
          caption="Donor listing"
          columns={donorColumns}
          rows={donors}
          getRowKey={(row) => row.donorId}
          emptyMessage="No donors matched the selected filter."
        />
      </AppCard>

      <AppCard title="Create Donor" subtitle="Register an individual or organization donor.">
        <form className="event-create-form" onSubmit={handleCreateDonor} noValidate>
          {donorFormError ? (
            <AlertBanner variant="warning" title="Donor validation" message={donorFormError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              First Name
              <input name="firstName" value={donorForm.firstName} onChange={handleDonorFormChange} required />
            </label>
            <label>
              Last Name
              <input name="lastName" value={donorForm.lastName} onChange={handleDonorFormChange} required />
            </label>
            <label>
              Donor Type
              <select name="donorType" value={donorForm.donorType} onChange={handleDonorFormChange}>
                {DONOR_TYPES.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Organization Name
              <input name="organizationName" value={donorForm.organizationName} onChange={handleDonorFormChange} />
            </label>
            <label>
              Email
              <input type="email" name="email" value={donorForm.email} onChange={handleDonorFormChange} />
            </label>
            <label>
              Street
              <input name="street" value={donorForm.street} onChange={handleDonorFormChange} required />
            </label>
            <label>
              Area
              <input name="area" value={donorForm.area} onChange={handleDonorFormChange} required />
            </label>
            <label>
              City
              <input name="city" value={donorForm.city} onChange={handleDonorFormChange} required />
            </label>
            <label>
              Province
              <input name="province" value={donorForm.province} onChange={handleDonorFormChange} required />
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isCreatingDonor || !canCreateDonor}>
              {isCreatingDonor ? 'Creating...' : 'Create Donor'}
            </button>
          </div>
        </form>
      </AppCard>

      <AppCard
        title="Donor Phone Management"
        subtitle="Maintain multiple phone numbers for donor communication and follow-ups."
      >
        {donorPhoneFormError ? (
          <AlertBanner variant="warning" title="Donor phone validation" message={donorPhoneFormError} />
        ) : null}

        <div className="toolbar-inline" style={{ marginBottom: '0.8rem' }}>
          <label htmlFor="selectedDonorId" className="toolbar-label">
            Donor
          </label>
          <select
            id="selectedDonorId"
            value={selectedDonorId}
            onChange={(event) => {
              setSelectedDonorId(event.target.value)
              setDonorPhoneForm(getDefaultDonorPhoneForm())
              setDonorPhoneFormError('')
            }}
          >
            <option value="">Select donor</option>
            {donors.map((row) => (
              <option key={row.donorId} value={row.donorId}>
                #{row.donorId} - {row.firstName} {row.lastName}
              </option>
            ))}
          </select>
        </div>

        <DataTable
          caption="Selected donor phones"
          columns={donorPhoneColumns}
          rows={donorPhones}
          getRowKey={(row) => `${row.donorId}-${row.phoneNumber}`}
          emptyMessage="No phones are registered for the selected donor."
        />

        <form className="event-create-form" onSubmit={handleAddDonorPhone} noValidate>
          <div className="event-form-grid">
            <label>
              New Phone
              <input
                name="newPhoneNumber"
                value={donorPhoneForm.newPhoneNumber}
                onChange={handleDonorPhoneFormChange}
                placeholder="+1 555 0200"
                required
              />
            </label>
          </div>
          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={!canAddDonorPhone || isSubmittingDonorPhone}>
              {isSubmittingDonorPhone ? 'Adding...' : 'Add Donor Phone'}
            </button>
          </div>
        </form>

        <form className="event-create-form" onSubmit={handleUpdateDonorPhone} noValidate>
          <div className="event-form-grid">
            <label>
              Existing Phone
              <select
                name="currentPhone"
                value={donorPhoneForm.currentPhone}
                onChange={handleDonorPhoneFormChange}
              >
                <option value="">Select phone</option>
                {donorPhones.map((row) => (
                  <option key={row.phoneNumber} value={row.phoneNumber}>
                    {row.phoneNumber}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Replacement Phone
              <input
                name="newPhoneNumber"
                value={donorPhoneForm.newPhoneNumber}
                onChange={handleDonorPhoneFormChange}
                placeholder="+1 555 0201"
                required
              />
            </label>
          </div>
          <div className="event-form-actions">
            <button
              type="submit"
              className="table-action-btn"
              disabled={!canUpdateDonorPhone || isDonorPhoneActionBusy(donorPhoneForm.currentPhone, 'update')}
            >
              Update Donor Phone
            </button>
          </div>
        </form>
      </AppCard>

      <AppCard
        title="Donation Board"
        subtitle="Filter donations and progress their confirmation status."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="donationDonorFilter" className="toolbar-label">
              Donor
            </label>
            <input
              id="donationDonorFilter"
              type="number"
              min={1}
              placeholder="Donor ID"
              value={donationDonorFilter}
              onChange={(event) => setDonationDonorFilter(event.target.value)}
            />

            <label htmlFor="donationEventFilter" className="toolbar-label">
              Event
            </label>
            <input
              id="donationEventFilter"
              type="number"
              min={1}
              placeholder="Event ID"
              value={donationEventFilter}
              onChange={(event) => setDonationEventFilter(event.target.value)}
            />

            <label htmlFor="donationStatusFilter" className="toolbar-label">
              Status
            </label>
            <select
              id="donationStatusFilter"
              value={donationStatusFilter}
              onChange={(event) => setDonationStatusFilter(event.target.value)}
            >
              <option value="">All</option>
              {DONATION_STATUSES.map((item) => (
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
          caption="Donation listing"
          columns={donationColumns}
          rows={donations}
          getRowKey={(row) => row.donationId}
          emptyMessage="No donations matched the selected filters."
        />
      </AppCard>

      <AppCard title="Create Donation" subtitle="Record a donor contribution against a disaster event.">
        <form className="event-create-form" onSubmit={handleCreateDonation} noValidate>
          {donationFormError ? (
            <AlertBanner variant="warning" title="Donation validation" message={donationFormError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              Donor ID
              <input type="number" min={1} name="donorId" value={donationForm.donorId} onChange={handleDonationFormChange} required />
            </label>
            <label>
              Event ID
              <input type="number" min={1} name="eventId" value={donationForm.eventId} onChange={handleDonationFormChange} required />
            </label>
            <label>
              Amount
              <input type="number" min={0.01} step="0.01" name="amount" value={donationForm.amount} onChange={handleDonationFormChange} required />
            </label>
            <label>
              Donation Date
              <input type="datetime-local" name="donationDate" value={donationForm.donationDate} onChange={handleDonationFormChange} />
            </label>
            <label>
              Payment Method
              <select name="paymentMethod" value={donationForm.paymentMethod} onChange={handleDonationFormChange}>
                {PAYMENT_METHODS.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Status
              <select name="status" value={donationForm.status} onChange={handleDonationFormChange}>
                {DONATION_STATUSES.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Receipt Number
              <input name="receiptNumber" value={donationForm.receiptNumber} onChange={handleDonationFormChange} />
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isCreatingDonation || !canCreateDonation}>
              {isCreatingDonation ? 'Creating...' : 'Create Donation'}
            </button>
          </div>
        </form>
      </AppCard>
    </div>
  )
}
