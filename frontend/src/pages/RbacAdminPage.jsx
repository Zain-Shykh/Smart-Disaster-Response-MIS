import { useCallback, useEffect, useMemo, useState , useRef} from 'react'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useNotification } from '../context/NotificationContext'
import {
  assignRoleToUser,
  createPermission,
  createRole,
  getPermissions,
  getRolePermissions,
  getRoles,
  getUserRoles,
  getUsers,
  mapRolePermission,
  removeRoleFromUser,
  unmapRolePermission,
  updateRole,
} from '../services/api/rbacAdminApi'

function getDefaultRoleForm() {
  return {
    roleName: '',
    description: '',
  }
}

function getDefaultPermissionForm() {
  return {
    permissionName: '',
    module: '',
    action: '',
  }
}

function getDefaultRoleUpdateForm() {
  return {
    roleName: '',
    description: '',
  }
}

export function RbacAdminPage() {
  const { notify } = useNotification()

  const [roles, setRoles] = useState([])
  const [permissions, setPermissions] = useState([])
  const [selectedRoleId, setSelectedRoleId] = useState('')
  const [rolePermissions, setRolePermissions] = useState([])
  const [users, setUsers] = useState([])
  const [selectedUserId, setSelectedUserId] = useState('')
  const [selectedUserRoles, setSelectedUserRoles] = useState([])
  const [selectedAssignRoleId, setSelectedAssignRoleId] = useState('')

  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [isCreatingRole, setIsCreatingRole] = useState(false)
  const [isUpdatingRole, setIsUpdatingRole] = useState(false)
  const [isCreatingPermission, setIsCreatingPermission] = useState(false)
  const [mappingActionKey, setMappingActionKey] = useState('')
  const [userRoleActionKey, setUserRoleActionKey] = useState('')

  const [rolePageNumber, setRolePageNumber] = useState(1)
  const [rolePageSize, setRolePageSize] = useState(10)
  const [permissionPageNumber, setPermissionPageNumber] = useState(1)
  const [permissionPageSize, setPermissionPageSize] = useState(10)
  const [permissionModuleFilter, setPermissionModuleFilter] = useState('')
  const [userPageNumber, setUserPageNumber] = useState(1)
  const [userPageSize, setUserPageSize] = useState(10)

  const [rolePagination, setRolePagination] = useState({ totalCount: 0, totalPages: 0 })
  const [permissionPagination, setPermissionPagination] = useState({ totalCount: 0, totalPages: 0 })
  const [userPagination, setUserPagination] = useState({ totalCount: 0, totalPages: 0 })

  const [roleForm, setRoleForm] = useState(() => getDefaultRoleForm())
  const [roleUpdateForm, setRoleUpdateForm] = useState(() => getDefaultRoleUpdateForm())
  const [permissionForm, setPermissionForm] = useState(() => getDefaultPermissionForm())

  const [selectedPermissionId, setSelectedPermissionId] = useState('')

  const [roleFormError, setRoleFormError] = useState('')
  const [roleUpdateError, setRoleUpdateError] = useState('')
  const [permissionFormError, setPermissionFormError] = useState('')
  const [mappingError, setMappingError] = useState('')
  const [userRoleError, setUserRoleError] = useState('')

  const selectedRole = useMemo(
    () => roles.find((item) => item.roleId === Number(selectedRoleId)) || null,
    [roles, selectedRoleId],
  )

  
  const filtersRef = useRef({ permissionModuleFilter })
  filtersRef.current = { permissionModuleFilter }

const loadData = useCallback(
    async ({ refreshOnly = false } = {}) => {
      if (refreshOnly) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }

      try {
        const [rolesData, permissionsData, usersData] = await Promise.all([
          getRoles({ pageNumber: rolePageNumber, pageSize: rolePageSize }),
          getPermissions({
            pageNumber: permissionPageNumber,
            pageSize: permissionPageSize,
            ...(filtersRef.current.permissionModuleFilter ? { module: filtersRef.current.permissionModuleFilter } : {}),
          }),
          getUsers({
            pageNumber: userPageNumber,
            pageSize: userPageSize,
          }),
        ])

        const roleRows = Array.isArray(rolesData?.roles) ? rolesData.roles : []
        const permissionRows = Array.isArray(permissionsData?.permissions) ? permissionsData.permissions : []
  const userRows = Array.isArray(usersData?.users) ? usersData.users : []

        setRoles(roleRows)
        setPermissions(permissionRows)
        setUsers(userRows)

        setRolePagination({
          totalCount: rolesData?.totalCount || 0,
          totalPages: rolesData?.totalPages || 0,
        })
        setPermissionPagination({
          totalCount: permissionsData?.totalCount || 0,
          totalPages: permissionsData?.totalPages || 0,
        })
        setUserPagination({
          totalCount: usersData?.totalCount || 0,
          totalPages: usersData?.totalPages || 0,
        })

        if (!selectedRoleId && roleRows.length > 0) {
          setSelectedRoleId(String(roleRows[0].roleId))
          setRoleUpdateForm({
            roleName: roleRows[0].roleName || '',
            description: roleRows[0].description || '',
          })
        }

        if (!selectedUserId && userRows.length > 0) {
          setSelectedUserId(String(userRows[0].userId))
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
    async function loadSelectedRolePermissions() {
      if (!selectedRoleId) {
        setRolePermissions([])
        return
      }

      const data = await getRolePermissions(Number(selectedRoleId))
      setRolePermissions(Array.isArray(data) ? data : [])

      if (selectedRole) {
        setRoleUpdateForm({
          roleName: selectedRole.roleName || '',
          description: selectedRole.description || '',
        })
      }
    }

    loadSelectedRolePermissions()
  }, [selectedRoleId, selectedRole])

  useEffect(() => {
    async function loadSelectedUserRoles() {
      if (!selectedUserId) {
        setSelectedUserRoles([])
        return
      }

      const data = await getUserRoles(Number(selectedUserId))
      setSelectedUserRoles(Array.isArray(data) ? data : [])
    }

    loadSelectedUserRoles()
  }, [selectedUserId])

  function handleRoleFormChange(event) {
    const { name, value } = event.target
    setRoleForm((previous) => ({ ...previous, [name]: value }))
  }

  function handleRoleUpdateChange(event) {
    const { name, value } = event.target
    setRoleUpdateForm((previous) => ({ ...previous, [name]: value }))
  }

  function handlePermissionFormChange(event) {
    const { name, value } = event.target
    setPermissionForm((previous) => ({ ...previous, [name]: value }))
  }

  async function handleCreateRole(event) {
    event.preventDefault()
    setRoleFormError('')

    const payload = {
      roleName: roleForm.roleName.trim(),
      description: roleForm.description.trim() || null,
    }

    if (!payload.roleName) {
      setRoleFormError('Role name is required.')
      return
    }

    setIsCreatingRole(true)

    try {
      const created = await createRole(payload)
      notify({
        title: 'Role created',
        message: `${created.roleName} was created successfully.`,
        variant: 'success',
      })
      setRoleForm(getDefaultRoleForm())
      await loadData({ refreshOnly: true })
      setSelectedRoleId(String(created.roleId))
    } catch {
      setRoleFormError('Unable to create role. Verify role name uniqueness and validation constraints.')
    } finally {
      setIsCreatingRole(false)
    }
  }

  async function handleUpdateRole(event) {
    event.preventDefault()
    setRoleUpdateError('')

    if (!selectedRoleId) {
      setRoleUpdateError('Select a role first.')
      return
    }

    const payload = {
      roleName: roleUpdateForm.roleName.trim(),
      description: roleUpdateForm.description.trim(),
    }

    if (!payload.roleName) {
      setRoleUpdateError('Role name is required.')
      return
    }

    setIsUpdatingRole(true)

    try {
      const updated = await updateRole(Number(selectedRoleId), payload)
      notify({
        title: 'Role updated',
        message: `${updated.roleName} was updated successfully.`,
        variant: 'success',
      })
      await loadData({ refreshOnly: true })
    } catch {
      setRoleUpdateError('Unable to update role. Verify role name uniqueness and validation constraints.')
    } finally {
      setIsUpdatingRole(false)
    }
  }

  async function handleCreatePermission(event) {
    event.preventDefault()
    setPermissionFormError('')

    const payload = {
      permissionName: permissionForm.permissionName.trim(),
      module: permissionForm.module.trim(),
      action: permissionForm.action.trim(),
    }

    if (!payload.permissionName || !payload.module || !payload.action) {
      setPermissionFormError('Permission name, module, and action are required.')
      return
    }

    setIsCreatingPermission(true)

    try {
      const created = await createPermission(payload)
      notify({
        title: 'Permission created',
        message: `${created.permissionName} was created successfully.`,
        variant: 'success',
      })
      setPermissionForm(getDefaultPermissionForm())
      await loadData({ refreshOnly: true })
      setSelectedPermissionId(String(created.permissionId))
    } catch {
      setPermissionFormError('Unable to create permission. Verify uniqueness and field constraints.')
    } finally {
      setIsCreatingPermission(false)
    }
  }

  function isMappingBusy(type, permissionId) {
    return mappingActionKey === `${type}-${permissionId}`
  }

  async function handleMapPermission() {
    setMappingError('')

    if (!selectedRoleId || !selectedPermissionId) {
      setMappingError('Select both role and permission before mapping.')
      return
    }

    setMappingActionKey(`map-${selectedPermissionId}`)

    try {
      await mapRolePermission({
        roleId: Number(selectedRoleId),
        permissionId: Number(selectedPermissionId),
      })
      notify({
        title: 'Permission mapped',
        message: `Permission #${selectedPermissionId} mapped to role #${selectedRoleId}.`,
        variant: 'success',
      })
      const data = await getRolePermissions(Number(selectedRoleId))
      setRolePermissions(Array.isArray(data) ? data : [])
      await loadData({ refreshOnly: true })
    } catch {
      setMappingError('Unable to map permission to role. It may already be mapped.')
    } finally {
      setMappingActionKey('')
    }
  }

  async function handleUnmapPermission(permissionId) {
    if (!selectedRoleId) {
      return
    }

    setMappingActionKey(`unmap-${permissionId}`)

    try {
      await unmapRolePermission(Number(selectedRoleId), permissionId)
      notify({
        title: 'Permission unmapped',
        message: `Permission #${permissionId} removed from role #${selectedRoleId}.`,
        variant: 'success',
      })
      const data = await getRolePermissions(Number(selectedRoleId))
      setRolePermissions(Array.isArray(data) ? data : [])
      await loadData({ refreshOnly: true })
    } catch {
      // Global ProblemDetails notifications surface details.
    } finally {
      setMappingActionKey('')
    }
  }

  function isUserRoleBusy(type, roleId) {
    return userRoleActionKey === `${type}-${roleId}`
  }

  async function handleAssignRoleToUser() {
    setUserRoleError('')

    if (!selectedUserId || !selectedAssignRoleId) {
      setUserRoleError('Select both user and role before assignment.')
      return
    }

    setUserRoleActionKey(`assign-${selectedAssignRoleId}`)

    try {
      await assignRoleToUser(Number(selectedUserId), {
        roleId: Number(selectedAssignRoleId),
      })
      notify({
        title: 'Role assigned',
        message: `Role #${selectedAssignRoleId} assigned to user #${selectedUserId}.`,
        variant: 'success',
      })
      const data = await getUserRoles(Number(selectedUserId))
      setSelectedUserRoles(Array.isArray(data) ? data : [])
      await loadData({ refreshOnly: true })
    } catch {
      setUserRoleError('Unable to assign role. The user may already have this role.')
    } finally {
      setUserRoleActionKey('')
    }
  }

  async function handleRemoveRoleFromUser(roleId) {
    if (!selectedUserId) {
      return
    }

    setUserRoleActionKey(`remove-${roleId}`)

    try {
      await removeRoleFromUser(Number(selectedUserId), roleId)
      notify({
        title: 'Role removed',
        message: `Role #${roleId} removed from user #${selectedUserId}.`,
        variant: 'success',
      })
      const data = await getUserRoles(Number(selectedUserId))
      setSelectedUserRoles(Array.isArray(data) ? data : [])
      await loadData({ refreshOnly: true })
    } catch {
      // Global ProblemDetails notifications surface details.
    } finally {
      setUserRoleActionKey('')
    }
  }

  const moduleOptions = useMemo(() => {
    const modules = permissions.map((item) => item.module).filter(Boolean)
    return [...new Set(modules)].sort((left, right) => left.localeCompare(right))
  }, [permissions])

  const roleColumns = useMemo(
    () => [
      {
        key: 'roleId',
        header: 'Role',
        render: (row) => <span className="mono-cell">#{row.roleId}</span>,
      },
      {
        key: 'roleName',
        header: 'Name',
      },
      {
        key: 'description',
        header: 'Description',
        render: (row) => row.description || '-',
      },
      {
        key: 'permissionCount',
        header: 'Permissions',
        render: (row) => <StatusBadge label={String(row.permissionCount)} status="active" />,
      },
      {
        key: 'userCount',
        header: 'Users',
        render: (row) => <StatusBadge label={String(row.userCount)} status="info" />,
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <button
            type="button"
            className="table-action-btn"
            onClick={() => {
              setSelectedRoleId(String(row.roleId))
              setRoleUpdateForm({
                roleName: row.roleName || '',
                description: row.description || '',
              })
            }}
          >
            Select
          </button>
        ),
      },
    ],
    [],
  )

  const permissionColumns = useMemo(
    () => [
      {
        key: 'permissionId',
        header: 'Permission',
        render: (row) => <span className="mono-cell">#{row.permissionId}</span>,
      },
      {
        key: 'permissionName',
        header: 'Name',
      },
      {
        key: 'module',
        header: 'Module',
      },
      {
        key: 'action',
        header: 'Action',
      },
      {
        key: 'roleCount',
        header: 'Mapped Roles',
        render: (row) => <StatusBadge label={String(row.roleCount)} status="active" />,
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <button
            type="button"
            className="table-action-btn"
            onClick={() => setSelectedPermissionId(String(row.permissionId))}
          >
            Select
          </button>
        ),
      },
    ],
    [],
  )

  const rolePermissionColumns = useMemo(
    () => [
      {
        key: 'permissionId',
        header: 'Permission',
        render: (row) => <span className="mono-cell">#{row.permissionId}</span>,
      },
      {
        key: 'permissionName',
        header: 'Name',
      },
      {
        key: 'module',
        header: 'Module',
      },
      {
        key: 'action',
        header: 'Action',
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <button
            type="button"
            className="table-action-btn"
            disabled={isMappingBusy('unmap', row.permissionId)}
            onClick={() => handleUnmapPermission(row.permissionId)}
          >
            Unmap
          </button>
        ),
      },
    ],
    [mappingActionKey, selectedRoleId],
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
        key: 'name',
        header: 'Name',
        render: (row) => `${row.firstName} ${row.lastName}`,
      },
      {
        key: 'email',
        header: 'Email',
      },
      {
        key: 'status',
        header: 'Status',
        render: (row) => <StatusBadge label={row.isActive ? 'Active' : 'Inactive'} status={row.isActive ? 'success' : 'blocked'} />,
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <button
            type="button"
            className="table-action-btn"
            onClick={() => {
              setSelectedUserId(String(row.userId))
              setUserRoleError('')
            }}
          >
            Select
          </button>
        ),
      },
    ],
    [],
  )

  const selectedUserRoleColumns = useMemo(
    () => [
      {
        key: 'roleId',
        header: 'Role',
        render: (row) => <span className="mono-cell">#{row.roleId}</span>,
      },
      {
        key: 'roleName',
        header: 'Role Name',
      },
      {
        key: 'assignedAt',
        header: 'Assigned At',
        render: (row) => row.assignedAt ? new Date(row.assignedAt).toLocaleString() : '-',
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <button
            type="button"
            className="table-action-btn"
            disabled={isUserRoleBusy('remove', row.roleId)}
            onClick={() => handleRemoveRoleFromUser(row.roleId)}
          >
            Remove
          </button>
        ),
      },
    ],
    [userRoleActionKey, selectedUserId],
  )

  if (isLoading) {
    return (
      <LoadingState
        title="Loading RBAC administration"
        message="Preparing role and permission management workflows."
      />
    )
  }

  return (
    <div>
<AppCard
        title="Roles"
        subtitle="Browse and select roles with pagination and directory metrics."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="rolePageSize" className="toolbar-label">
              Page Size
            </label>
            <select
              id="rolePageSize"
              value={rolePageSize}
              onChange={(event) => {
                setRolePageSize(Number(event.target.value))
                setRolePageNumber(1)
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
          caption="Role administration listing"
          columns={roleColumns}
          rows={roles}
          getRowKey={(row) => row.roleId}
          emptyMessage="No roles found."
        />

        <div className="event-form-actions" style={{ marginTop: '0.8rem' }}>
          <button
            type="button"
            className="table-action-btn"
            disabled={rolePageNumber <= 1}
            onClick={() => setRolePageNumber((previous) => Math.max(1, previous - 1))}
          >
            Previous
          </button>
          <span className="event-form-meta">
            Page {rolePageNumber} of {rolePagination.totalPages || 1} ({rolePagination.totalCount} roles)
          </span>
          <button
            type="button"
            className="table-action-btn"
            disabled={rolePagination.totalPages === 0 || rolePageNumber >= rolePagination.totalPages}
            onClick={() => setRolePageNumber((previous) => previous + 1)}
          >
            Next
          </button>
        </div>
      </AppCard>

      <AppCard title="Create Role" subtitle="Add a new role with a unique name.">
        <form className="event-create-form" onSubmit={handleCreateRole}>
          {roleFormError ? (
            <AlertBanner variant="warning" title="Role validation" message={roleFormError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              Role Name
              <input name="roleName" value={roleForm.roleName} onChange={handleRoleFormChange} required />
            </label>
            <label>
              Description
              <input name="description" value={roleForm.description} onChange={handleRoleFormChange} />
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isCreatingRole}>
              {isCreatingRole ? 'Creating...' : 'Create Role'}
            </button>
          </div>
        </form>
      </AppCard>

      <AppCard
        title="Update Selected Role"
        subtitle="Edit role name and description for the selected role."
      >
        {selectedRoleId ? (
          <form className="event-create-form" onSubmit={handleUpdateRole}>
            {roleUpdateError ? (
              <AlertBanner variant="warning" title="Role update validation" message={roleUpdateError} />
            ) : null}

            <p className="event-form-meta">Editing role #{selectedRoleId}</p>

            <div className="event-form-grid">
              <label>
                Role Name
                <input
                  name="roleName"
                  value={roleUpdateForm.roleName}
                  onChange={handleRoleUpdateChange}
                  required
                />
              </label>
              <label>
                Description
                <input
                  name="description"
                  value={roleUpdateForm.description}
                  onChange={handleRoleUpdateChange}
                />
              </label>
            </div>

            <div className="event-form-actions">
              <button type="submit" className="table-action-btn" disabled={isUpdatingRole}>
                {isUpdatingRole ? 'Saving...' : 'Save Role'}
              </button>
            </div>
          </form>
        ) : (
          <AlertBanner
            variant="warning"
            title="No role selected"
            message="Select a role from the table to update it."
          />
        )}
      </AppCard>

      <AppCard
        title="Permissions"
        subtitle="Browse and filter permissions by module with pagination."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="permissionModuleFilter" className="toolbar-label">
              Module
            </label>
            <select
              id="permissionModuleFilter"
              value={permissionModuleFilter}
              onChange={(event) => {
                setPermissionModuleFilter(event.target.value)
                setPermissionPageNumber(1)
              }}
            >
              <option value="">All</option>
              {moduleOptions.map((module) => (
                <option key={module} value={module}>
                  {module}
                </option>
              ))}
            </select>

            <label htmlFor="permissionPageSize" className="toolbar-label">
              Page Size
            </label>
            <select
              id="permissionPageSize"
              value={permissionPageSize}
              onChange={(event) => {
                setPermissionPageSize(Number(event.target.value))
                setPermissionPageNumber(1)
              }}
            >
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
            </select>
          </div>
        }
      >
        <DataTable
          caption="Permission administration listing"
          columns={permissionColumns}
          rows={permissions}
          getRowKey={(row) => row.permissionId}
          emptyMessage="No permissions found."
        />

        <div className="event-form-actions" style={{ marginTop: '0.8rem' }}>
          <button
            type="button"
            className="table-action-btn"
            disabled={permissionPageNumber <= 1}
            onClick={() => setPermissionPageNumber((previous) => Math.max(1, previous - 1))}
          >
            Previous
          </button>
          <span className="event-form-meta">
            Page {permissionPageNumber} of {permissionPagination.totalPages || 1} ({permissionPagination.totalCount} permissions)
          </span>
          <button
            type="button"
            className="table-action-btn"
            disabled={permissionPagination.totalPages === 0 || permissionPageNumber >= permissionPagination.totalPages}
            onClick={() => setPermissionPageNumber((previous) => previous + 1)}
          >
            Next
          </button>
        </div>
      </AppCard>

      <AppCard title="Create Permission" subtitle="Add module-action permissions for RBAC mapping.">
        <form className="event-create-form" onSubmit={handleCreatePermission}>
          {permissionFormError ? (
            <AlertBanner variant="warning" title="Permission validation" message={permissionFormError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              Permission Name
              <input
                name="permissionName"
                value={permissionForm.permissionName}
                onChange={handlePermissionFormChange}
                required
              />
            </label>
            <label>
              Module
              <input name="module" value={permissionForm.module} onChange={handlePermissionFormChange} required />
            </label>
            <label>
              Action
              <input name="action" value={permissionForm.action} onChange={handlePermissionFormChange} required />
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isCreatingPermission}>
              {isCreatingPermission ? 'Creating...' : 'Create Permission'}
            </button>
          </div>
        </form>
      </AppCard>

      <AppCard
        title="Role-Permission Mapping"
        subtitle="Map selected permissions to selected roles and unmap existing links."
      >
        {mappingError ? (
          <AlertBanner variant="warning" title="Mapping validation" message={mappingError} />
        ) : null}

        <div className="toolbar-inline" style={{ marginBottom: '0.8rem' }}>
          <label htmlFor="selectedRoleId" className="toolbar-label">
            Role
          </label>
          <select
            id="selectedRoleId"
            value={selectedRoleId}
            onChange={(event) => {
              setSelectedRoleId(event.target.value)
              setMappingError('')
            }}
          >
            <option value="">Select role</option>
            {roles.map((role) => (
              <option key={role.roleId} value={role.roleId}>
                #{role.roleId} - {role.roleName}
              </option>
            ))}
          </select>

          <label htmlFor="selectedPermissionId" className="toolbar-label">
            Permission
          </label>
          <select
            id="selectedPermissionId"
            value={selectedPermissionId}
            onChange={(event) => setSelectedPermissionId(event.target.value)}
          >
            <option value="">Select permission</option>
            {permissions.map((permission) => (
              <option key={permission.permissionId} value={permission.permissionId}>
                #{permission.permissionId} - {permission.permissionName}
              </option>
            ))}
          </select>

          <button
            type="button"
            className="table-action-btn"
            disabled={!selectedRoleId || !selectedPermissionId || isMappingBusy('map', selectedPermissionId)}
            onClick={handleMapPermission}
          >
            Map Permission
          </button>
        </div>

        <DataTable
          caption="Selected role permissions"
          columns={rolePermissionColumns}
          rows={rolePermissions}
          getRowKey={(row) => `${row.permissionId}-${row.permissionName}`}
          emptyMessage={selectedRoleId ? 'No permissions mapped to the selected role.' : 'Select a role to view mappings.'}
        />
      </AppCard>

      <AppCard
        title="User Directory for Role Assignment"
        subtitle="Select users and manage their role memberships."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="userPageSize" className="toolbar-label">
              Page Size
            </label>
            <select
              id="userPageSize"
              value={userPageSize}
              onChange={(event) => {
                setUserPageSize(Number(event.target.value))
                setUserPageNumber(1)
              }}
            >
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
            </select>
          </div>
        }
      >
        <DataTable
          caption="User listing for role assignments"
          columns={userColumns}
          rows={users}
          getRowKey={(row) => row.userId}
          emptyMessage="No users found."
        />

        <div className="event-form-actions" style={{ marginTop: '0.8rem' }}>
          <button
            type="button"
            className="table-action-btn"
            disabled={userPageNumber <= 1}
            onClick={() => setUserPageNumber((previous) => Math.max(1, previous - 1))}
          >
            Previous
          </button>
          <span className="event-form-meta">
            Page {userPageNumber} of {userPagination.totalPages || 1} ({userPagination.totalCount} users)
          </span>
          <button
            type="button"
            className="table-action-btn"
            disabled={userPagination.totalPages === 0 || userPageNumber >= userPagination.totalPages}
            onClick={() => setUserPageNumber((previous) => previous + 1)}
          >
            Next
          </button>
        </div>
      </AppCard>

      <AppCard
        title="User-Role Mapping"
        subtitle="Assign roles to the selected user and remove current role memberships."
      >
        {userRoleError ? (
          <AlertBanner variant="warning" title="User-role validation" message={userRoleError} />
        ) : null}

        <div className="toolbar-inline" style={{ marginBottom: '0.8rem' }}>
          <label htmlFor="selectedUserId" className="toolbar-label">
            User
          </label>
          <select
            id="selectedUserId"
            value={selectedUserId}
            onChange={(event) => {
              setSelectedUserId(event.target.value)
              setUserRoleError('')
            }}
          >
            <option value="">Select user</option>
            {users.map((user) => (
              <option key={user.userId} value={user.userId}>
                #{user.userId} - {user.username}
              </option>
            ))}
          </select>

          <label htmlFor="selectedAssignRoleId" className="toolbar-label">
            Role
          </label>
          <select
            id="selectedAssignRoleId"
            value={selectedAssignRoleId}
            onChange={(event) => setSelectedAssignRoleId(event.target.value)}
          >
            <option value="">Select role</option>
            {roles.map((role) => (
              <option key={role.roleId} value={role.roleId}>
                #{role.roleId} - {role.roleName}
              </option>
            ))}
          </select>

          <button
            type="button"
            className="table-action-btn"
            disabled={!selectedUserId || !selectedAssignRoleId || isUserRoleBusy('assign', selectedAssignRoleId)}
            onClick={handleAssignRoleToUser}
          >
            Assign Role
          </button>
        </div>

        <DataTable
          caption="Selected user role memberships"
          columns={selectedUserRoleColumns}
          rows={selectedUserRoles}
          getRowKey={(row) => `${row.userId}-${row.roleId}`}
          emptyMessage={selectedUserId ? 'No roles assigned to the selected user.' : 'Select a user to view role memberships.'}
        />
      </AppCard>
    </div>
  )
}
