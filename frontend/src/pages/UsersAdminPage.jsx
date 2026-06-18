import { useCallback, useEffect, useMemo, useState , useRef} from 'react'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useNotification } from '../context/NotificationContext'
import { hasTrimmedText } from '../utils/formGuards'
import {
  addUserPhone,
  createUser,
  deleteUserPhone,
  deactivateUser,
  getRoles,
  getUserById,
  getUserPhones,
  getUsers,
  updateUserPhone,
  updateUser,
} from '../services/api/userAdminApi'

function getDefaultCreateForm() {
  return {
    username: '',
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    roleIds: [],
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

function getDefaultUserPhoneForm() {
  return {
    currentPhone: '',
    newPhoneNumber: '',
  }
}

export function UsersAdminPage() {
  const { notify } = useNotification()

  const [users, setUsers] = useState([])
  const [roles, setRoles] = useState([])
  const [selectedUser, setSelectedUser] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [isCreatingUser, setIsCreatingUser] = useState(false)
  const [isSavingProfile, setIsSavingProfile] = useState(false)
  const [activationActionKey, setActivationActionKey] = useState('')
  const [phoneActionKey, setPhoneActionKey] = useState('')
  const [isSubmittingPhone, setIsSubmittingPhone] = useState(false)

  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [roleFilter, setRoleFilter] = useState('')

  const [createForm, setCreateForm] = useState(() => getDefaultCreateForm())
  const [userPhones, setUserPhones] = useState([])
  const [userPhoneForm, setUserPhoneForm] = useState(() => getDefaultUserPhoneForm())
  const [createFormError, setCreateFormError] = useState('')
  const [profileFormError, setProfileFormError] = useState('')
  const [userPhoneFormError, setUserPhoneFormError] = useState('')

  const [profileForm, setProfileForm] = useState({
    email: '',
    firstName: '',
    lastName: '',
    isActive: true,
  })

  const canCreateUser =
    hasTrimmedText(createForm.username, 3) &&
    hasTrimmedText(createForm.email) &&
    hasTrimmedText(createForm.password, 8) &&
    hasTrimmedText(createForm.firstName) &&
    hasTrimmedText(createForm.lastName)

  const canSaveProfile =
    selectedUser &&
    hasTrimmedText(profileForm.email) &&
    hasTrimmedText(profileForm.firstName) &&
    hasTrimmedText(profileForm.lastName)

  const canAddUserPhone = selectedUser && hasTrimmedText(userPhoneForm.newPhoneNumber)
  const canUpdateUserPhone =
    selectedUser && hasTrimmedText(userPhoneForm.currentPhone) && hasTrimmedText(userPhoneForm.newPhoneNumber)

  const [pagination, setPagination] = useState({
    totalCount: 0,
    totalPages: 0,
  })

  
  const filtersRef = useRef({ roleFilter })
  filtersRef.current = { roleFilter }

const loadData = useCallback(
    async ({ refreshOnly = false } = {}) => {
      if (refreshOnly) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }

      try {
        const [userList, roleList] = await Promise.all([
          getUsers({
            pageNumber,
            pageSize,
            ...(filtersRef.current.roleFilter ? { roleId: Number(filtersRef.current.roleFilter) } : {}),
          }),
          getRoles(),
        ])

        const userRows = Array.isArray(userList?.users) ? userList.users : []
        setUsers(userRows)
        setRoles(Array.isArray(roleList) ? roleList : [])
        setPagination({
          totalCount: userList?.totalCount || 0,
          totalPages: userList?.totalPages || 0,
        })

        if (!selectedUser && userRows.length > 0) {
          await openUser(userRows[0].userId)
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

  async function openUser(userId) {
    try {
      const [detail, phones] = await Promise.all([
        getUserById(userId),
        getUserPhones(userId),
      ])
      setSelectedUser(detail)
      setUserPhones(Array.isArray(phones) ? phones : [])
      setUserPhoneForm(getDefaultUserPhoneForm())
      setUserPhoneFormError('')
      setProfileForm({
        email: detail.email || '',
        firstName: detail.firstName || '',
        lastName: detail.lastName || '',
        isActive: detail.isActive,
      })
      setProfileFormError('')
    } catch {
      setSelectedUser(null)
      setUserPhones([])
    }
  }

  function handleCreateFormChange(event) {
    const { name, value } = event.target
    setCreateForm((previous) => ({
      ...previous,
      [name]: value,
    }))
  }

  function handleRoleSelection(roleId, checked) {
    setCreateForm((previous) => ({
      ...previous,
      roleIds: checked
        ? [...previous.roleIds, roleId]
        : previous.roleIds.filter((id) => id !== roleId),
    }))
  }

  function handleProfileFormChange(event) {
    const { name, value, type, checked } = event.target
    setProfileForm((previous) => ({
      ...previous,
      [name]: type === 'checkbox' ? checked : value,
    }))
  }

  function handleUserPhoneFormChange(event) {
    const { name, value } = event.target
    setUserPhoneForm((previous) => ({
      ...previous,
      [name]: value,
    }))
  }

  async function handleCreateUser(event) {
    event.preventDefault()
    setCreateFormError('')

    if (!canCreateUser) {
      setCreateFormError('Enter a valid username, email, password, and full name before submitting.')
      return
    }

    const payload = {
      username: createForm.username.trim(),
      email: createForm.email.trim(),
      password: createForm.password,
      firstName: createForm.firstName.trim(),
      lastName: createForm.lastName.trim(),
      roleIds: createForm.roleIds,
    }

    if (!payload.username || !payload.email || !payload.password || !payload.firstName || !payload.lastName) {
      setCreateFormError('Username, email, password, and full name are required.')
      return
    }

    if (payload.password.length < 8) {
      setCreateFormError('Password must be at least 8 characters.')
      return
    }

    setIsCreatingUser(true)

    try {
      const created = await createUser(payload)
      notify({
        title: 'User created',
        message: `${created.username} was created successfully.`,
        variant: 'success',
      })
      setCreateForm(getDefaultCreateForm())
      await loadData({ refreshOnly: true })
      await openUser(created.userId)
    } catch {
      setCreateFormError('Unable to create user. Verify unique username/email and role selections.')
    } finally {
      setIsCreatingUser(false)
    }
  }

  async function handleSaveProfile(event) {
    event.preventDefault()
    setProfileFormError('')

    if (!canSaveProfile) {
      setProfileFormError('Enter a valid email, first name, and last name before saving the profile.')
      return
    }

    if (!selectedUser) {
      setProfileFormError('Select a user first.')
      return
    }

    const payload = {
      email: profileForm.email.trim(),
      firstName: profileForm.firstName.trim(),
      lastName: profileForm.lastName.trim(),
      isActive: profileForm.isActive,
    }

    if (!payload.email || !payload.firstName || !payload.lastName) {
      setProfileFormError('Email, first name, and last name are required.')
      return
    }

    setIsSavingProfile(true)

    try {
      const updated = await updateUser(selectedUser.userId, payload)
      setSelectedUser(updated)
      notify({
        title: 'Profile updated',
        message: `User #${selectedUser.userId} profile was updated.`,
        variant: 'success',
      })
      await loadData({ refreshOnly: true })
    } catch {
      setProfileFormError('Unable to update user profile. Verify unique email and retry.')
    } finally {
      setIsSavingProfile(false)
    }
  }

  function isActivationBusy(userId, targetState) {
    return activationActionKey === `${userId}-${targetState}`
  }

  async function handleSetActiveState(row, isActive) {
    setActivationActionKey(`${row.userId}-${isActive}`)

    try {
      if (!isActive) {
        await deactivateUser(row.userId)
      } else {
        await updateUser(row.userId, { isActive: true })
      }

      notify({
        title: 'User status updated',
        message: `${row.username} is now ${isActive ? 'active' : 'inactive'}.`,
        variant: 'success',
      })
      await loadData({ refreshOnly: true })

      if (selectedUser?.userId === row.userId) {
        await openUser(row.userId)
      }
    } catch {
      // Global ProblemDetails notifications surface details.
    } finally {
      setActivationActionKey('')
    }
  }

  function isPhoneActionBusy(phoneNumber, action) {
    return phoneActionKey === `${phoneNumber}-${action}`
  }

  async function reloadSelectedUserPhones() {
    if (!selectedUser) {
      return
    }

    const phones = await getUserPhones(selectedUser.userId)
    setUserPhones(Array.isArray(phones) ? phones : [])
  }

  async function handleAddUserPhone(event) {
    event.preventDefault()
    setUserPhoneFormError('')

    if (!selectedUser) {
      setUserPhoneFormError('Select a user first.')
      return
    }

    if (!canAddUserPhone) {
      setUserPhoneFormError('Enter a phone number before adding it to the user.')
      return
    }

    const payload = {
      phoneNumber: userPhoneForm.newPhoneNumber.trim(),
    }

    if (!payload.phoneNumber) {
      setUserPhoneFormError('Phone number is required.')
      return
    }

    setIsSubmittingPhone(true)

    try {
      await addUserPhone(selectedUser.userId, payload)
      notify({
        title: 'User phone added',
        message: `Phone ${payload.phoneNumber} was added for user #${selectedUser.userId}.`,
        variant: 'success',
      })
      setUserPhoneForm(getDefaultUserPhoneForm())
      await reloadSelectedUserPhones()
    } catch {
      setUserPhoneFormError('Unable to add phone. Verify number format and uniqueness for this user.')
    } finally {
      setIsSubmittingPhone(false)
    }
  }

  async function handleUpdateUserPhone(event) {
    event.preventDefault()
    setUserPhoneFormError('')

    if (!selectedUser) {
      setUserPhoneFormError('Select a user first.')
      return
    }

    if (!canUpdateUserPhone) {
      setUserPhoneFormError('Select the existing phone and enter the replacement phone number.')
      return
    }

    const currentPhone = userPhoneForm.currentPhone.trim()
    const payload = {
      newPhoneNumber: userPhoneForm.newPhoneNumber.trim(),
    }

    if (!currentPhone || !payload.newPhoneNumber) {
      setUserPhoneFormError('Select an existing phone and enter the new phone number.')
      return
    }

    setPhoneActionKey(`${currentPhone}-update`)

    try {
      await updateUserPhone(selectedUser.userId, currentPhone, payload)
      notify({
        title: 'User phone updated',
        message: `Phone ${currentPhone} was updated for user #${selectedUser.userId}.`,
        variant: 'success',
      })
      setUserPhoneForm(getDefaultUserPhoneForm())
      await reloadSelectedUserPhones()
    } catch {
      setUserPhoneFormError('Unable to update phone. Verify number format and uniqueness for this user.')
    } finally {
      setPhoneActionKey('')
    }
  }

  async function handleDeleteUserPhone(phoneNumber) {
    if (!selectedUser) {
      return
    }

    setPhoneActionKey(`${phoneNumber}-delete`)

    try {
      await deleteUserPhone(selectedUser.userId, phoneNumber)
      notify({
        title: 'User phone removed',
        message: `Phone ${phoneNumber} was removed from user #${selectedUser.userId}.`,
        variant: 'success',
      })
      if (userPhoneForm.currentPhone === phoneNumber) {
        setUserPhoneForm(getDefaultUserPhoneForm())
      }
      await reloadSelectedUserPhones()
    } catch {
      // Global ProblemDetails notifications surface details.
    } finally {
      setPhoneActionKey('')
    }
  }

  const userPhoneColumns = useMemo(
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
              onClick={() => setUserPhoneForm({
                currentPhone: row.phoneNumber,
                newPhoneNumber: row.phoneNumber,
              })}
            >
              Select
            </button>
            <button
              type="button"
              className="table-action-btn"
              disabled={isPhoneActionBusy(row.phoneNumber, 'delete')}
              onClick={() => handleDeleteUserPhone(row.phoneNumber)}
            >
              Remove
            </button>
          </div>
        ),
      },
    ],
    [phoneActionKey, userPhoneForm.currentPhone],
  )

  const userColumns = useMemo(
    () => [
      {
        key: 'userId',
        header: 'User',
        render: (row) => <span className="mono-cell">#{row.userId}</span>,
      },
      {
        key: 'username',
        header: 'Username',
      },
      {
        key: 'fullName',
        header: 'Name',
        render: (row) => `${row.firstName} ${row.lastName}`,
      },
      {
        key: 'email',
        header: 'Email',
      },
      {
        key: 'roles',
        header: 'Roles',
        render: (row) => (Array.isArray(row.roles) && row.roles.length > 0
          ? row.roles.map((role) => role.roleName).join(', ')
          : '-'),
      },
      {
        key: 'isActive',
        header: 'Status',
        render: (row) => (
          <StatusBadge
            label={row.isActive ? 'Active' : 'Inactive'}
            status={row.isActive ? 'success' : 'blocked'}
          />
        ),
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
              onClick={() => openUser(row.userId)}
            >
              Open
            </button>
            <button
              type="button"
              className="table-action-btn"
              disabled={isActivationBusy(row.userId, true) || row.isActive}
              onClick={() => handleSetActiveState(row, true)}
            >
              Activate
            </button>
            <button
              type="button"
              className="table-action-btn"
              disabled={isActivationBusy(row.userId, false) || !row.isActive}
              onClick={() => handleSetActiveState(row, false)}
            >
              Deactivate
            </button>
          </div>
        ),
      },
    ],
    [activationActionKey, selectedUser],
  )

  if (isLoading) {
    return (
      <LoadingState
        title="Loading user administration"
        message="Preparing user directory, profile details, and status controls."
      />
    )
  }

  return (
    <div>
<AppCard
        title="User Directory"
        subtitle="Filter by role and paginate through users in the administration directory."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="roleFilter" className="toolbar-label">
              Role
            </label>
            <select
              id="roleFilter"
              value={roleFilter}
              onChange={(event) => {
                setRoleFilter(event.target.value)
                setPageNumber(1)
              }}
            >
              <option value="">All</option>
              {roles.map((role) => (
                <option key={role.roleId} value={role.roleId}>
                  {role.roleName}
                </option>
              ))}
            </select>

            <label htmlFor="pageSize" className="toolbar-label">
              Page Size
            </label>
            <select
              id="pageSize"
              value={pageSize}
              onChange={(event) => {
                setPageSize(Number(event.target.value))
                setPageNumber(1)
              }}
            >
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
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
          caption="User administration directory"
          columns={userColumns}
          rows={users}
          getRowKey={(row) => row.userId}
          emptyMessage="No users matched the current filters."
        />

        <div className="event-form-actions" style={{ marginTop: '0.8rem' }}>
          <button
            type="button"
            className="table-action-btn"
            disabled={pageNumber <= 1}
            onClick={() => setPageNumber((previous) => Math.max(1, previous - 1))}
          >
            Previous
          </button>
          <span className="event-form-meta">
            Page {pageNumber} of {pagination.totalPages || 1} ({pagination.totalCount} users)
          </span>
          <button
            type="button"
            className="table-action-btn"
            disabled={pagination.totalPages === 0 || pageNumber >= pagination.totalPages}
            onClick={() => setPageNumber((previous) => previous + 1)}
          >
            Next
          </button>
        </div>
      </AppCard>

      <AppCard title="Create User" subtitle="Create a new user profile and assign initial roles.">
        <form className="event-create-form" onSubmit={handleCreateUser} noValidate>
          {createFormError ? (
            <AlertBanner variant="warning" title="Create user validation" message={createFormError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              Username
              <input name="username" value={createForm.username} onChange={handleCreateFormChange} required />
            </label>
            <label>
              Email
              <input type="email" name="email" value={createForm.email} onChange={handleCreateFormChange} required />
            </label>
            <label>
              Password
              <input type="password" name="password" value={createForm.password} onChange={handleCreateFormChange} required />
            </label>
            <label>
              First Name
              <input name="firstName" value={createForm.firstName} onChange={handleCreateFormChange} required />
            </label>
            <label>
              Last Name
              <input name="lastName" value={createForm.lastName} onChange={handleCreateFormChange} required />
            </label>
          </div>

          <div className="event-form-grid">
            <label>
              Role Assignment
              <div className="table-actions" style={{ flexWrap: 'wrap' }}>
                {roles.map((role) => (
                  <label key={role.roleId} className="form-check-row" style={{ minHeight: 'auto' }}>
                    <input
                      type="checkbox"
                      checked={createForm.roleIds.includes(role.roleId)}
                      onChange={(event) => handleRoleSelection(role.roleId, event.target.checked)}
                    />
                    {role.roleName}
                  </label>
                ))}
              </div>
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isCreatingUser || !canCreateUser}>
              {isCreatingUser ? 'Creating...' : 'Create User'}
            </button>
          </div>
        </form>
      </AppCard>

      <AppCard
        title="User Profile Detail"
        subtitle="View and edit core user profile fields, including active/inactive status."
      >
        {selectedUser ? (
          <form className="event-create-form" onSubmit={handleSaveProfile} noValidate>
            {profileFormError ? (
              <AlertBanner variant="warning" title="Profile validation" message={profileFormError} />
            ) : null}

            <p className="event-form-meta">
              User #{selectedUser.userId} ({selectedUser.username}) created {formatDateTime(selectedUser.createdAt)}
            </p>

            <div className="event-form-grid">
              <label>
                Email
                <input
                  type="email"
                  name="email"
                  value={profileForm.email}
                  onChange={handleProfileFormChange}
                  required
                />
              </label>
              <label>
                First Name
                <input
                  name="firstName"
                  value={profileForm.firstName}
                  onChange={handleProfileFormChange}
                  required
                />
              </label>
              <label>
                Last Name
                <input
                  name="lastName"
                  value={profileForm.lastName}
                  onChange={handleProfileFormChange}
                  required
                />
              </label>
              <label className="form-check-row">
                <input
                  type="checkbox"
                  name="isActive"
                  checked={profileForm.isActive}
                  onChange={handleProfileFormChange}
                />
                Active
              </label>
            </div>

            <p className="event-form-meta">
              Last login: {formatDateTime(selectedUser.lastLoginAt)}
            </p>
            <p className="event-form-meta">
              Roles: {Array.isArray(selectedUser.roles) && selectedUser.roles.length > 0
                ? selectedUser.roles.map((role) => role.roleName).join(', ')
                : '-'}
            </p>

            <div className="event-form-actions">
              <button type="submit" className="table-action-btn" disabled={isSavingProfile || !canSaveProfile}>
                {isSavingProfile ? 'Saving...' : 'Save Profile'}
              </button>
            </div>
          </form>
        ) : (
          <AlertBanner
            variant="warning"
            title="No user selected"
            message="Open a user from the directory to view and edit profile details."
          />
        )}
      </AppCard>

      <AppCard
        title="User Phone Management"
        subtitle="Add, update, and remove multiple phone numbers for the selected user."
      >
        {selectedUser ? (
          <>
            {userPhoneFormError ? (
              <AlertBanner variant="warning" title="User phone validation" message={userPhoneFormError} />
            ) : null}

            <DataTable
              caption="Selected user phones"
              columns={userPhoneColumns}
              rows={userPhones}
              getRowKey={(row) => `${row.userId}-${row.phoneNumber}`}
              emptyMessage="No phones are registered for this user yet."
            />

            <form className="event-create-form" onSubmit={handleAddUserPhone} noValidate>
              <div className="event-form-grid">
                <label>
                  New Phone
                  <input
                    name="newPhoneNumber"
                    value={userPhoneForm.newPhoneNumber}
                    onChange={handleUserPhoneFormChange}
                    placeholder="+1 555 0100"
                    required
                  />
                </label>
              </div>
              <div className="event-form-actions">
                <button type="submit" className="table-action-btn" disabled={isSubmittingPhone || !canAddUserPhone}>
                  {isSubmittingPhone ? 'Adding...' : 'Add Phone'}
                </button>
              </div>
            </form>

            <form className="event-create-form" onSubmit={handleUpdateUserPhone} noValidate>
              <div className="event-form-grid">
                <label>
                  Existing Phone
                  <select
                    name="currentPhone"
                    value={userPhoneForm.currentPhone}
                    onChange={handleUserPhoneFormChange}
                  >
                    <option value="">Select phone</option>
                    {userPhones.map((row) => (
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
                    value={userPhoneForm.newPhoneNumber}
                    onChange={handleUserPhoneFormChange}
                    placeholder="+1 555 0101"
                    required
                  />
                </label>
              </div>
              <div className="event-form-actions">
                <button
                  type="submit"
                  className="table-action-btn"
                  disabled={!canUpdateUserPhone || isPhoneActionBusy(userPhoneForm.currentPhone, 'update')}
                >
                  Update Phone
                </button>
              </div>
            </form>
          </>
        ) : (
          <AlertBanner
            variant="warning"
            title="No user selected"
            message="Open a user from the directory to manage phone numbers."
          />
        )}
      </AppCard>
    </div>
  )
}
