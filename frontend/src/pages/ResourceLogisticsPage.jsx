import { useCallback, useEffect, useMemo, useState , useRef} from 'react'
import { useLocation } from 'react-router-dom'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useAuth } from '../context/AuthContext'
import { useNotification } from '../context/NotificationContext'
import {
  createAllocation,
  createInventory,
  createResource,
  createWarehouse,
  downloadInventoryHistoryCsv,
  getAllocations,
  getInventoryAlerts,
  getInventoryHistory,
  getInventories,
  getResources,
  getWarehouseInventoryHistory,
  getWarehouses,
  updateAllocationStatus,
  updateInventoryLevels,
} from '../services/api/resourceLogisticsApi'
import { getDisasterEvents } from '../services/api/disasterEventsApi'

const RESOURCE_TYPES = ['Food', 'Water', 'Medicine', 'Shelter']
const ALERT_STATUSES = ['Active', 'Resolved']
const ALLOCATION_STATUSES = ['Pending', 'Approved', 'Dispatched', 'Consumed', 'Rejected']

function getDefaultResourceForm() {
  return {
    resourceId: '',
    resourceName: '',
    resourceType: 'Food',
    unit: '',
    description: '',
  }
}

function getDefaultWarehouseForm() {
  return {
    warehouseId: '',
    warehouseName: '',
    street: '',
    area: '',
    city: '',
    province: '',
    latitude: '',
    longitude: '',
    capacity: 0,
    managerId: '',
    contactPhone: '',
    contactEmail: '',
  }
}

function getDefaultInventoryForm() {
  return {
    inventoryId: '',
    warehouseId: '',
    resourceId: '',
    quantity: 0,
    minThreshold: 0,
    maxCapacity: 100,
  }
}

function getDefaultAllocationForm() {
  return {
    inventoryId: '',
    eventId: '',
    quantity: 1,
    status: 'Pending',
    requiresApproval: true,
    approvalRequestedBy: '',
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

export function ResourceLogisticsPage() {
  const { user } = useAuth()
  const { notify } = useNotification()
  const location = useLocation()
  
  const isResources = location.pathname.includes('resources')
  const isInventory = location.pathname.includes('inventory')
  const isAllocations = location.pathname.includes('allocations')

  const [resources, setResources] = useState([])
  const [warehouses, setWarehouses] = useState([])
  const [inventories, setInventories] = useState([])
  const [alerts, setAlerts] = useState([])
  const [allocations, setAllocations] = useState([])
  const [disasterEvents, setDisasterEvents] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)

  const [resourceTypeFilter, setResourceTypeFilter] = useState('')
  const [warehouseCityFilter, setWarehouseCityFilter] = useState('')
  const [inventoryWarehouseFilter, setInventoryWarehouseFilter] = useState('')
  const [inventoryResourceFilter, setInventoryResourceFilter] = useState('')
  const [lowStockOnly, setLowStockOnly] = useState(false)
  const [alertStatusFilter, setAlertStatusFilter] = useState('')
  const [alertInventoryFilter, setAlertInventoryFilter] = useState('')
  const [alertWarehouseFilter, setAlertWarehouseFilter] = useState('')
  const [allocationEventFilter, setAllocationEventFilter] = useState('')
  const [allocationInventoryFilter, setAllocationInventoryFilter] = useState('')
  const [allocationStatusFilter, setAllocationStatusFilter] = useState('')
  const [historyInventoryFilter, setHistoryInventoryFilter] = useState('')
  const [historyStartDate, setHistoryStartDate] = useState('')
  const [historyEndDate, setHistoryEndDate] = useState('')
  const [warehouseHistoryFilter, setWarehouseHistoryFilter] = useState('')

  const [resourceForm, setResourceForm] = useState(() => getDefaultResourceForm())
  const [warehouseForm, setWarehouseForm] = useState(() => getDefaultWarehouseForm())
  const [inventoryForm, setInventoryForm] = useState(() => getDefaultInventoryForm())
  const [inventoryEditForm, setInventoryEditForm] = useState(null)
  const [allocationForm, setAllocationForm] = useState(() => getDefaultAllocationForm())
  const [inventoryHistoryRows, setInventoryHistoryRows] = useState([])
  const [warehouseHistoryRows, setWarehouseHistoryRows] = useState([])

  const [resourceFormError, setResourceFormError] = useState('')
  const [warehouseFormError, setWarehouseFormError] = useState('')
  const [inventoryFormError, setInventoryFormError] = useState('')
  const [inventoryEditError, setInventoryEditError] = useState('')
  const [allocationFormError, setAllocationFormError] = useState('')
  const [inventoryHistoryError, setInventoryHistoryError] = useState('')
  const [warehouseHistoryError, setWarehouseHistoryError] = useState('')

  const [isCreatingResource, setIsCreatingResource] = useState(false)
  const [isCreatingWarehouse, setIsCreatingWarehouse] = useState(false)
  const [isCreatingInventory, setIsCreatingInventory] = useState(false)
  const [isUpdatingInventory, setIsUpdatingInventory] = useState(false)
  const [isCreatingAllocation, setIsCreatingAllocation] = useState(false)
  const [allocationActionKey, setAllocationActionKey] = useState('')
  const [isLoadingInventoryHistory, setIsLoadingInventoryHistory] = useState(false)
  const [isLoadingWarehouseHistory, setIsLoadingWarehouseHistory] = useState(false)
  const [isExportingInventoryHistory, setIsExportingInventoryHistory] = useState(false)

  
  const filtersRef = useRef({ resourceTypeFilter, warehouseCityFilter, inventoryWarehouseFilter, inventoryResourceFilter, alertStatusFilter, alertInventoryFilter, alertWarehouseFilter, allocationEventFilter, allocationInventoryFilter, allocationStatusFilter, historyInventoryFilter, warehouseHistoryFilter })
  filtersRef.current = { resourceTypeFilter, warehouseCityFilter, inventoryWarehouseFilter, inventoryResourceFilter, alertStatusFilter, alertInventoryFilter, alertWarehouseFilter, allocationEventFilter, allocationInventoryFilter, allocationStatusFilter, historyInventoryFilter, warehouseHistoryFilter }

const loadData = useCallback(
    async ({ refreshOnly = false } = {}) => {
      if (refreshOnly) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }

      try {
        const [resourceData, warehouseData, inventoryData, alertData, allocationData, eventData] = await Promise.all([
          getResources(filtersRef.current.resourceTypeFilter ? { resourceType: filtersRef.current.resourceTypeFilter } : {}),
          getWarehouses(filtersRef.current.warehouseCityFilter.trim() ? { city: filtersRef.current.warehouseCityFilter.trim() } : {}),
          getInventories({
            ...(filtersRef.current.inventoryWarehouseFilter ? { warehouseId: Number(filtersRef.current.inventoryWarehouseFilter) } : {}),
            ...(filtersRef.current.inventoryResourceFilter ? { resourceId: Number(filtersRef.current.inventoryResourceFilter) } : {}),
            ...(lowStockOnly ? { lowStockOnly: true } : {}),
          }),
          getInventoryAlerts({
            ...(filtersRef.current.alertStatusFilter ? { status: filtersRef.current.alertStatusFilter } : {}),
            ...(filtersRef.current.alertInventoryFilter ? { inventoryId: Number(filtersRef.current.alertInventoryFilter) } : {}),
          }),
          getAllocations({
            ...(filtersRef.current.allocationEventFilter ? { eventId: Number(filtersRef.current.allocationEventFilter) } : {}),
            ...(filtersRef.current.allocationInventoryFilter
              ? { inventoryId: Number(filtersRef.current.allocationInventoryFilter) }
              : {}),
            ...(filtersRef.current.allocationStatusFilter ? { status: filtersRef.current.allocationStatusFilter } : {}),
          }),
          getDisasterEvents({ status: 'Active' }),
        ])

        setResources(Array.isArray(resourceData) ? resourceData : [])
        setWarehouses(Array.isArray(warehouseData) ? warehouseData : [])
        setInventories(Array.isArray(inventoryData) ? inventoryData : [])
        setAlerts(Array.isArray(alertData) ? alertData : [])
        setAllocations(Array.isArray(allocationData) ? allocationData : [])
        setDisasterEvents(Array.isArray(eventData) ? eventData : [])
      } finally {
        setIsLoading(false)
        setIsRefreshing(false)
      }
    },
    [],
  )

  const filteredAlerts = useMemo(() => {
    if (!alertWarehouseFilter.trim()) {
      return alerts
    }

    const warehouseQuery = alertWarehouseFilter.trim().toLowerCase()
    return alerts.filter((item) => item.warehouseName.toLowerCase().includes(warehouseQuery))
  }, [alerts, alertWarehouseFilter])

  useEffect(() => {
    loadData()
  }, [loadData])

  function handleResourceFormChange(event) {
    const { name, value } = event.target
    setResourceForm((previous) => ({
      ...previous,
      [name]: value,
    }))
  }

  function handleWarehouseFormChange(event) {
    const { name, value } = event.target
    setWarehouseForm((previous) => ({
      ...previous,
      [name]: name === 'capacity' ? Number(value) : value,
    }))
  }

  function handleInventoryFormChange(event) {
    const { name, value } = event.target
    setInventoryForm((previous) => ({
      ...previous,
      [name]: ['quantity', 'minThreshold', 'maxCapacity'].includes(name) ? toNumericValue(value) : value,
    }))
  }

  function handleInventoryEditFormChange(event) {
    const { name, value } = event.target
    setInventoryEditForm((previous) => ({
      ...previous,
      [name]: ['quantity', 'minThreshold', 'maxCapacity'].includes(name) ? toNumericValue(value) : value,
    }))
  }

  function handleAllocationFormChange(event) {
    const { name, value, type, checked } = event.target
    setAllocationForm((previous) => ({
      ...previous,
      [name]:
        type === 'checkbox'
          ? checked
          : name === 'quantity'
            ? toNumericValue(value)
            : value,
    }))
  }

  async function handleCreateResource(event) {
    event.preventDefault()
    setResourceFormError('')

    const id = Number(resourceForm.resourceId)
    if (!id || id <= 0) {
      setResourceFormError('Resource ID is required and must be greater than zero.')
      return
    }
    if (resources.some((r) => r.resourceId === id)) {
      setResourceFormError(`Resource ID ${id} is already in use.`)
      return
    }

    const payload = {
      resourceName: resourceForm.resourceName.trim(),
      resourceType: resourceForm.resourceType,
      unit: resourceForm.unit.trim(),
      description: resourceForm.description.trim() || null,
    }

    if (!payload.resourceName || !payload.unit) {
      setResourceFormError('Resource name and unit are required.')
      return
    }

    setIsCreatingResource(true)

    try {
      await createResource(payload)
      notify({
        title: 'Resource created',
        message: `${payload.resourceName} was added to the catalog.`,
        variant: 'success',
      })
      setResourceForm(getDefaultResourceForm())
      await loadData({ refreshOnly: true })
    } catch {
      setResourceFormError('Unable to create resource. Verify values and try again.')
    } finally {
      setIsCreatingResource(false)
    }
  }

  async function handleCreateWarehouse(event) {
    event.preventDefault()
    setWarehouseFormError('')

    const id = Number(warehouseForm.warehouseId)
    if (!id || id <= 0) {
      setWarehouseFormError('Warehouse ID is required and must be greater than zero.')
      return
    }
    if (warehouses.some((w) => w.warehouseId === id)) {
      setWarehouseFormError(`Warehouse ID ${id} is already in use.`)
      return
    }

    const payload = {
      warehouseName: warehouseForm.warehouseName.trim(),
      street: warehouseForm.street.trim(),
      area: warehouseForm.area.trim(),
      city: warehouseForm.city.trim(),
      province: warehouseForm.province.trim(),
      latitude: warehouseForm.latitude === '' ? null : Number(warehouseForm.latitude),
      longitude: warehouseForm.longitude === '' ? null : Number(warehouseForm.longitude),
      capacity: Number(warehouseForm.capacity),
      managerId: Number(warehouseForm.managerId),
      contactPhone: warehouseForm.contactPhone.trim() || null,
      contactEmail: warehouseForm.contactEmail.trim() || null,
    }

    if (
      !payload.warehouseName ||
      !payload.street ||
      !payload.area ||
      !payload.city ||
      !payload.province
    ) {
      setWarehouseFormError('Warehouse name and location fields are required.')
      return
    }

    if (!payload.managerId || payload.managerId <= 0) {
      setWarehouseFormError('Manager ID must be greater than zero.')
      return
    }

    if (payload.capacity < 0) {
      setWarehouseFormError('Capacity cannot be negative.')
      return
    }

    setIsCreatingWarehouse(true)

    try {
      await createWarehouse(payload)
      notify({
        title: 'Warehouse created',
        message: `${payload.warehouseName} was added successfully.`,
        variant: 'success',
      })
      setWarehouseForm(getDefaultWarehouseForm())
      await loadData({ refreshOnly: true })
    } catch {
      setWarehouseFormError(
        'Unable to create warehouse. Verify manager ID and location details.',
      )
    } finally {
      setIsCreatingWarehouse(false)
    }
  }

  async function handleCreateInventory(event) {
    event.preventDefault()
    setInventoryFormError('')

    const id = Number(inventoryForm.inventoryId)
    if (!id || id <= 0) {
      setInventoryFormError('Inventory ID is required and must be greater than zero.')
      return
    }
    if (inventories.some((i) => i.inventoryId === id)) {
      setInventoryFormError(`Inventory ID ${id} is already in use.`)
      return
    }

    const payload = {
      warehouseId: Number(inventoryForm.warehouseId),
      resourceId: Number(inventoryForm.resourceId),
      quantity: toNumericValue(inventoryForm.quantity),
      minThreshold: toNumericValue(inventoryForm.minThreshold),
      maxCapacity: toNumericValue(inventoryForm.maxCapacity),
    }

    if (!payload.warehouseId || !payload.resourceId) {
      setInventoryFormError('Warehouse and resource IDs are required.')
      return
    }

    if (payload.quantity < 0 || payload.minThreshold < 0 || payload.maxCapacity <= 0) {
      setInventoryFormError('Quantity/threshold must be non-negative and max capacity must be positive.')
      return
    }

    if (payload.quantity > payload.maxCapacity) {
      setInventoryFormError('Quantity cannot exceed max capacity.')
      return
    }

    setIsCreatingInventory(true)

    try {
      await createInventory(payload)
      notify({
        title: 'Inventory created',
        message: `Inventory entry for warehouse #${payload.warehouseId} and resource #${payload.resourceId} was added.`,
        variant: 'success',
      })
      setInventoryForm(getDefaultInventoryForm())
      await loadData({ refreshOnly: true })
    } catch {
      setInventoryFormError('Unable to create inventory. Ensure warehouse-resource pair is unique and valid.')
    } finally {
      setIsCreatingInventory(false)
    }
  }

  function beginEditInventory(row) {
    setInventoryEditError('')
    setInventoryEditForm({
      inventoryId: row.inventoryId,
      warehouseName: row.warehouseName,
      resourceName: row.resourceName,
      quantity: toNumericValue(row.quantity),
      minThreshold: toNumericValue(row.minThreshold),
      maxCapacity: toNumericValue(row.maxCapacity),
      versionToken: row.versionToken,
    })
  }

  async function handleUpdateInventory(event) {
    event.preventDefault()

    if (!inventoryEditForm) {
      return
    }

    setInventoryEditError('')

    const payload = {
      quantity: toNumericValue(inventoryEditForm.quantity),
      minThreshold: toNumericValue(inventoryEditForm.minThreshold),
      maxCapacity: toNumericValue(inventoryEditForm.maxCapacity),
      versionToken: inventoryEditForm.versionToken,
    }

    if (payload.quantity < 0 || payload.minThreshold < 0 || payload.maxCapacity <= 0) {
      setInventoryEditError('Quantity/threshold must be non-negative and max capacity must be positive.')
      return
    }

    if (payload.quantity > payload.maxCapacity) {
      setInventoryEditError('Quantity cannot exceed max capacity.')
      return
    }

    setIsUpdatingInventory(true)

    try {
      await updateInventoryLevels(inventoryEditForm.inventoryId, payload)
      notify({
        title: 'Inventory updated',
        message: `Inventory #${inventoryEditForm.inventoryId} levels were updated.`,
        variant: 'success',
      })
      setInventoryEditForm(null)
      await loadData({ refreshOnly: true })
    } catch (error) {
      if (error.response?.status === 409) {
        const conflictMessage =
          error.response?.data?.message ||
          'Inventory was changed by another user. Please reload and retry.'
        setInventoryEditError(conflictMessage)
        notify({
          title: 'Inventory concurrency conflict',
          message: conflictMessage,
          variant: 'warning',
        })
        await loadData({ refreshOnly: true })
        return
      }

      setInventoryEditError('Unable to update inventory values. Please try again.')
    } finally {
      setIsUpdatingInventory(false)
    }
  }

  async function handleCreateAllocation(event) {
    event.preventDefault()
    setAllocationFormError('')

    if (!user?.userId) {
      setAllocationFormError('Current user context is unavailable. Re-login and try again.')
      return
    }

    const payload = {
      inventoryId: Number(allocationForm.inventoryId),
      eventId: Number(allocationForm.eventId),
      requestedBy: user.userId,
      quantity: toNumericValue(allocationForm.quantity),
      status: allocationForm.status,
      requiresApproval: allocationForm.requiresApproval,
      approvalRequestedBy: allocationForm.requiresApproval
        ? Number(allocationForm.approvalRequestedBy || user.userId)
        : null,
    }

    if (!payload.inventoryId || !payload.eventId) {
      setAllocationFormError('Inventory ID and Event ID are required.')
      return
    }

    if (payload.quantity <= 0) {
      setAllocationFormError('Allocation quantity must be greater than zero.')
      return
    }

    setIsCreatingAllocation(true)

    try {
      await createAllocation(payload)
      notify({
        title: 'Allocation created',
        message: `Allocation request created for inventory #${payload.inventoryId}.`,
        variant: 'success',
      })
      setAllocationForm(getDefaultAllocationForm())
      await loadData({ refreshOnly: true })
    } catch {
      setAllocationFormError('Unable to create allocation. Verify IDs and stock constraints.')
    } finally {
      setIsCreatingAllocation(false)
    }
  }

  function isAllocationActionBusy(allocationId, status) {
    return allocationActionKey === `${allocationId}-${status}`
  }

  async function handleUpdateAllocationStatus(row, status) {
    setAllocationActionKey(`${row.allocationId}-${status}`)

    try {
      await updateAllocationStatus(row.allocationId, {
        status,
        versionToken: row.versionToken,
      })

      notify({
        title: 'Allocation status updated',
        message: `Allocation #${row.allocationId} is now ${status}.`,
        variant: 'success',
      })
      await loadData({ refreshOnly: true })
    } catch {
      // Errors are surfaced by global ProblemDetails notifications.
    } finally {
      setAllocationActionKey('')
    }
  }

  async function loadInventoryHistory() {
    setInventoryHistoryError('')

    const inventoryId = Number(historyInventoryFilter)
    if (!inventoryId) {
      setInventoryHistoryError('Inventory ID is required to load movement history.')
      return
    }

    const query = {}
    if (historyStartDate) {
      query.startDate = historyStartDate
    }
    if (historyEndDate) {
      query.endDate = historyEndDate
    }

    setIsLoadingInventoryHistory(true)

    try {
      const response = await getInventoryHistory(inventoryId, query)
      setInventoryHistoryRows(Array.isArray(response) ? response : [])
      if (!Array.isArray(response) || response.length === 0) {
        notify({
          title: 'Inventory history loaded',
          message: `No movement records were returned for inventory #${inventoryId}.`,
          variant: 'info',
        })
      }
    } catch {
      setInventoryHistoryRows([])
      setInventoryHistoryError('Unable to load inventory history. Verify the inventory ID and date range.')
    } finally {
      setIsLoadingInventoryHistory(false)
    }
  }

  async function loadWarehouseHistory() {
    setWarehouseHistoryError('')

    const warehouseId = Number(warehouseHistoryFilter)
    if (!warehouseId) {
      setWarehouseHistoryError('Warehouse ID is required to load history summaries.')
      return
    }

    setIsLoadingWarehouseHistory(true)

    try {
      const response = await getWarehouseInventoryHistory(warehouseId)
      setWarehouseHistoryRows(Array.isArray(response) ? response : [])
      if (!Array.isArray(response) || response.length === 0) {
        notify({
          title: 'Warehouse history loaded',
          message: `No inventory summaries were returned for warehouse #${warehouseId}.`,
          variant: 'info',
        })
      }
    } catch {
      setWarehouseHistoryRows([])
      setWarehouseHistoryError('Unable to load warehouse history. Verify the warehouse ID and retry.')
    } finally {
      setIsLoadingWarehouseHistory(false)
    }
  }

  async function handleExportInventoryHistory() {
    setInventoryHistoryError('')

    const inventoryId = Number(historyInventoryFilter)
    if (!inventoryId) {
      setInventoryHistoryError('Inventory ID is required before exporting history.')
      return
    }

    const query = {}
    if (historyStartDate) {
      query.startDate = historyStartDate
    }
    if (historyEndDate) {
      query.endDate = historyEndDate
    }

    setIsExportingInventoryHistory(true)

    try {
      const { blob, fileName } = await downloadInventoryHistoryCsv(inventoryId, query)
      const downloadUrl = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = downloadUrl
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(downloadUrl)

      notify({
        title: 'Inventory history exported',
        message: `CSV download started for inventory #${inventoryId}.`,
        variant: 'success',
      })
    } catch {
      setInventoryHistoryError('Unable to export inventory history. Try again after reloading the filters.')
    } finally {
      setIsExportingInventoryHistory(false)
    }
  }

  const resourceColumns = useMemo(
    () => [
      {
        key: 'resourceId',
        header: 'Resource ID',
        render: (row) => <span className="mono-cell">#{row.resourceId}</span>,
      },
      {
        key: 'resourceName',
        header: 'Resource',
      },
      {
        key: 'resourceType',
        header: 'Type',
        render: (row) => <StatusBadge label={row.resourceType} status="active" />,
      },
      {
        key: 'unit',
        header: 'Unit',
      },
      {
        key: 'description',
        header: 'Description',
        render: (row) => row.description || '-',
      },
    ],
    [],
  )

  const warehouseColumns = useMemo(
    () => [
      {
        key: 'warehouseId',
        header: 'Warehouse ID',
        render: (row) => <span className="mono-cell">#{row.warehouseId}</span>,
      },
      {
        key: 'warehouseName',
        header: 'Warehouse',
      },
      {
        key: 'location',
        header: 'Location',
        render: (row) => `${row.area}, ${row.city}`,
      },
      {
        key: 'capacity',
        header: 'Capacity',
        align: 'center',
      },
      {
        key: 'managerId',
        header: 'Manager',
        render: (row) => <span className="mono-cell">#{row.managerId}</span>,
      },
      {
        key: 'contacts',
        header: 'Contact',
        render: (row) => row.contactPhone || row.contactEmail || '-',
      },
    ],
    [],
  )

  const inventoryColumns = useMemo(
    () => [
      {
        key: 'inventoryId',
        header: 'Inventory',
        render: (row) => <span className="mono-cell">#{row.inventoryId}</span>,
      },
      {
        key: 'warehouseName',
        header: 'Warehouse',
        render: (row) => (
          <div>
            <strong>{row.warehouseName}</strong>
            <div className="table-subtext">#{row.warehouseId}</div>
          </div>
        ),
      },
      {
        key: 'resourceName',
        header: 'Resource',
        render: (row) => (
          <div>
            <strong>{row.resourceName}</strong>
            <div className="table-subtext">#{row.resourceId} - {row.resourceType}</div>
          </div>
        ),
      },
      {
        key: 'quantity',
        header: 'Qty',
      },
      {
        key: 'thresholds',
        header: 'Thresholds',
        render: (row) => `Min ${row.minThreshold} / Max ${row.maxCapacity}`,
      },
      {
        key: 'stockStatus',
        header: 'Stock',
        render: (row) => (
          <StatusBadge
            label={Number(row.quantity) <= Number(row.minThreshold) ? 'Low Stock' : 'Stable'}
            status={Number(row.quantity) <= Number(row.minThreshold) ? 'warning' : 'success'}
          />
        ),
      },
      {
        key: 'lastUpdated',
        header: 'Updated',
        render: (row) => formatDateTime(row.lastUpdated),
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <button type="button" className="table-action-btn" onClick={() => beginEditInventory(row)}>
            Edit Levels
          </button>
        ),
      },
    ],
    [],
  )

  const alertColumns = useMemo(
    () => [
      {
        key: 'alertId',
        header: 'Alert',
        render: (row) => <span className="mono-cell">#{row.alertId}</span>,
      },
      {
        key: 'inventoryId',
        header: 'Inventory',
        render: (row) => <span className="mono-cell">#{row.inventoryId}</span>,
      },
      {
        key: 'resourceName',
        header: 'Resource',
        render: (row) => (
          <div>
            <strong>{row.resourceName}</strong>
            {row.resourceId ? <div className="table-subtext">#{row.resourceId}</div> : null}
          </div>
        ),
      },
      {
        key: 'warehouseName',
        header: 'Warehouse',
        render: (row) => (
          <div>
            <strong>{row.warehouseName}</strong>
            {row.warehouseId ? <div className="table-subtext">#{row.warehouseId}</div> : null}
          </div>
        ),
      },
      {
        key: 'alertType',
        header: 'Type',
      },
      {
        key: 'status',
        header: 'Status',
        render: (row) => <StatusBadge label={row.status} status={row.status} />,
      },
      {
        key: 'alertTime',
        header: 'Alert Time',
        render: (row) => formatDateTime(row.alertTime),
      },
    ],
    [],
  )

  const allocationColumns = useMemo(
    () => [
      {
        key: 'allocationId',
        header: 'Allocation',
        render: (row) => <span className="mono-cell">#{row.allocationId}</span>,
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
        key: 'resourceName',
        header: 'Resource',
        render: (row) => (
          <div>
            <strong>{row.resourceName}</strong>
            <div className="table-subtext">Inv #{row.inventoryId} - {row.warehouseName}</div>
          </div>
        ),
      },
      {
        key: 'quantity',
        header: 'Qty',
      },
      {
        key: 'status',
        header: 'Status',
        render: (row) => <StatusBadge label={row.status} status={row.status} />,
      },
      {
        key: 'requestTime',
        header: 'Requested',
        render: (row) => formatDateTime(row.requestTime),
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <div className="table-actions">
            {ALLOCATION_STATUSES.map((status) => (
              <button
                key={status}
                type="button"
                className="table-action-btn"
                disabled={isAllocationActionBusy(row.allocationId, status) || row.status === status}
                onClick={() => handleUpdateAllocationStatus(row, status)}
              >
                {status}
              </button>
            ))}
          </div>
        ),
      },
    ],
    [allocationActionKey],
  )

  const inventoryHistoryColumns = useMemo(
    () => [
      {
        key: 'movementTime',
        header: 'Time',
        render: (row) => formatDateTime(row.movementTime),
      },
      {
        key: 'movementType',
        header: 'Movement',
        render: (row) => <StatusBadge label={row.movementType} status={row.movementType} />,
      },
      {
        key: 'allocationId',
        header: 'Allocation',
        render: (row) => <span className="mono-cell">#{row.allocationId}</span>,
      },
      {
        key: 'eventName',
        header: 'Event',
        render: (row) => (
          <div>
            <strong>{row.eventName}</strong>
            <div className="table-subtext">Requested by {row.requestedByName}</div>
          </div>
        ),
      },
      {
        key: 'resourceName',
        header: 'Resource',
        render: (row) => row.resourceName,
      },
      {
        key: 'quantity',
        header: 'Qty',
      },
      {
        key: 'status',
        header: 'Status',
        render: (row) => <StatusBadge label={row.status} status={row.status} />,
      },
    ],
    [],
  )

  const warehouseHistoryColumns = useMemo(
    () => [
      {
        key: 'inventoryId',
        header: 'Inventory',
        render: (row) => <span className="mono-cell">#{row.inventoryId}</span>,
      },
      {
        key: 'resourceName',
        header: 'Resource',
      },
      {
        key: 'currentQuantity',
        header: 'Current Qty',
      },
      {
        key: 'totalAllocations',
        header: 'Allocations',
      },
      {
        key: 'totalRequestedQuantity',
        header: 'Requested',
      },
      {
        key: 'totalDispatchedQuantity',
        header: 'Dispatched',
      },
      {
        key: 'totalConsumedQuantity',
        header: 'Consumed',
      },
    ],
    [],
  )

  if (isLoading) {
    return (
      <LoadingState
        title="Loading logistics resources"
        message="Preparing resources and warehouse catalogs from backend services."
      />
    )
  }

  return (
    <div>
{isResources && (
        <>
          <AppCard
            title="Resource Catalog"
            subtitle="Create and filter resource master records used in inventory and allocations."
            actions={
              <div className="toolbar-inline">
                <label htmlFor="resourceTypeFilter" className="toolbar-label">
                  Type
                </label>
                <select
                  id="resourceTypeFilter"
                  value={resourceTypeFilter}
                  onChange={(event) => setResourceTypeFilter(event.target.value)}
                >
                  <option value="">All</option>
                  {RESOURCE_TYPES.map((item) => (
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
              caption="Resource master listing"
              columns={resourceColumns}
              rows={resources}
              getRowKey={(row) => row.resourceId}
              emptyMessage="No resources matched the selected filter."
            />
          </AppCard>

          <AppCard title="Create Resource" subtitle="Add new resource definitions for warehouse stock tracking.">
            <form className="event-create-form" onSubmit={handleCreateResource}>
              {resourceFormError ? (
                <AlertBanner variant="warning" title="Resource validation" message={resourceFormError} />
              ) : null}

              <div className="event-form-grid">
                <label>
                  Resource ID
                  <input
                    type="number"
                    name="resourceId"
                    value={resourceForm.resourceId}
                    onChange={handleResourceFormChange}
                    required
                  />
                </label>
                <label>
                  Resource Name
                  <input
                    name="resourceName"
                    value={resourceForm.resourceName}
                    onChange={handleResourceFormChange}
                    required
                  />
                </label>
                <label>
                  Resource Type
                  <select
                    name="resourceType"
                    value={resourceForm.resourceType}
                    onChange={handleResourceFormChange}
                  >
                    {RESOURCE_TYPES.map((item) => (
                      <option key={item} value={item}>
                        {item}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Unit
                  <input
                    name="unit"
                    value={resourceForm.unit}
                    onChange={handleResourceFormChange}
                    required
                  />
                </label>
                <label>
                  Description
                  <input
                    name="description"
                    value={resourceForm.description}
                    onChange={handleResourceFormChange}
                  />
                </label>
              </div>

              <div className="event-form-actions">
                <button type="submit" className="table-action-btn" disabled={isCreatingResource}>
                  {isCreatingResource ? 'Creating...' : 'Create Resource'}
                </button>
              </div>
            </form>
          </AppCard>

          <AppCard
            title="Warehouse Registry"
            subtitle="Filter and review warehouses with location, manager, and capacity details."
            actions={
              <div className="toolbar-inline">
                <label htmlFor="warehouseCityFilter" className="toolbar-label">
                  City
                </label>
                <input
                  id="warehouseCityFilter"
                  value={warehouseCityFilter}
                  onChange={(event) => setWarehouseCityFilter(event.target.value)}
                  placeholder="City"
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
              caption="Warehouse listing"
              columns={warehouseColumns}
              rows={warehouses}
              getRowKey={(row) => row.warehouseId}
              emptyMessage="No warehouses matched the city filter."
            />
          </AppCard>

          <AppCard title="Create Warehouse" subtitle="Register warehouse locations and responsible manager ID.">
            <form className="event-create-form" onSubmit={handleCreateWarehouse}>
              {warehouseFormError ? (
                <AlertBanner
                  variant="warning"
                  title="Warehouse validation"
                  message={warehouseFormError}
                />
              ) : null}

              <div className="event-form-grid">
                <label>
                  Warehouse ID
                  <input
                    type="number"
                    name="warehouseId"
                    value={warehouseForm.warehouseId}
                    onChange={handleWarehouseFormChange}
                    required
                  />
                </label>
                <label>
                  Warehouse Name
                  <input
                    name="warehouseName"
                    value={warehouseForm.warehouseName}
                    onChange={handleWarehouseFormChange}
                    required
                  />
                </label>
                <label>
                  Street
                  <input
                    name="street"
                    value={warehouseForm.street}
                    onChange={handleWarehouseFormChange}
                    required
                  />
                </label>
                <label>
                  Area
                  <input
                    name="area"
                    value={warehouseForm.area}
                    onChange={handleWarehouseFormChange}
                    required
                  />
                </label>
                <label>
                  City
                  <input
                    name="city"
                    value={warehouseForm.city}
                    onChange={handleWarehouseFormChange}
                    required
                  />
                </label>
                <label>
                  Province
                  <input
                    name="province"
                    value={warehouseForm.province}
                    onChange={handleWarehouseFormChange}
                    required
                  />
                </label>

                <label>
                  Capacity
                  <input
                    type="number"
                    min={0}
                    name="capacity"
                    value={warehouseForm.capacity}
                    onChange={handleWarehouseFormChange}
                    required
                  />
                </label>
                <label>
                  Manager ID
                  <input
                    type="number"
                    min={1}
                    name="managerId"
                    value={warehouseForm.managerId}
                    onChange={handleWarehouseFormChange}
                    required
                  />
                </label>
                <label>
                  Contact Phone
                  <input
                    name="contactPhone"
                    value={warehouseForm.contactPhone}
                    onChange={handleWarehouseFormChange}
                  />
                </label>
                <label>
                  Contact Email
                  <input
                    type="email"
                    name="contactEmail"
                    value={warehouseForm.contactEmail}
                    onChange={handleWarehouseFormChange}
                  />
                </label>
              </div>

              <div className="event-form-actions">
                <button type="submit" className="table-action-btn" disabled={isCreatingWarehouse}>
                  {isCreatingWarehouse ? 'Creating...' : 'Create Warehouse'}
                </button>
              </div>
            </form>
          </AppCard>
        </>
      )}

      {isInventory && (
        <>
          <AppCard
            title="Inventory Levels"
            subtitle="Track and filter warehouse-resource stock levels with low-stock indicators."
            actions={
              <div className="toolbar-inline">
                <label htmlFor="inventoryWarehouseFilter" className="toolbar-label">
                  Warehouse
                </label>
                <input
                  id="inventoryWarehouseFilter"
                  type="number"
                  min={1}
                  placeholder="Warehouse ID"
                  value={inventoryWarehouseFilter}
                  onChange={(event) => setInventoryWarehouseFilter(event.target.value)}
                />

                <label htmlFor="inventoryResourceFilter" className="toolbar-label">
                  Resource
                </label>
                <input
                  id="inventoryResourceFilter"
                  type="number"
                  min={1}
                  placeholder="Resource ID"
                  value={inventoryResourceFilter}
                  onChange={(event) => setInventoryResourceFilter(event.target.value)}
                />

                <label className="form-check-row">
                  <input
                    type="checkbox"
                    checked={lowStockOnly}
                    onChange={(event) => setLowStockOnly(event.target.checked)}
                  />
                  Low stock only
                </label>

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
              caption="Inventory listing"
              columns={inventoryColumns}
              rows={inventories}
              getRowKey={(row) => row.inventoryId}
              emptyMessage="No inventory records matched current filters."
            />
          </AppCard>

          <AppCard title="Create Inventory" subtitle="Create warehouse-resource inventory records with thresholds.">
            <form className="event-create-form" onSubmit={handleCreateInventory}>
              {inventoryFormError ? (
                <AlertBanner variant="warning" title="Inventory validation" message={inventoryFormError} />
              ) : null}

              <div className="event-form-grid">
                <label>
                  Inventory ID
                  <input
                    type="number"
                    min={1}
                    name="inventoryId"
                    value={inventoryForm.inventoryId}
                    onChange={handleInventoryFormChange}
                    required
                  />
                </label>
                <label>
                  Warehouse ID
                  <select
                    name="warehouseId"
                    value={inventoryForm.warehouseId}
                    onChange={handleInventoryFormChange}
                    required
                  >
                    <option value="">Select a warehouse</option>
                    {warehouses.map((w) => (
                      <option key={w.warehouseId} value={w.warehouseId}>
                        #{w.warehouseId} - {w.warehouseName} ({w.city})
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Resource ID
                  <select
                    name="resourceId"
                    value={inventoryForm.resourceId}
                    onChange={handleInventoryFormChange}
                    required
                  >
                    <option value="">Select a resource</option>
                    {resources.map((r) => (
                      <option key={r.resourceId} value={r.resourceId}>
                        #{r.resourceId} - {r.resourceName} ({r.resourceType})
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Quantity
                  <input
                    type="number"
                    min={0}
                    step="0.01"
                    name="quantity"
                    value={inventoryForm.quantity}
                    onChange={handleInventoryFormChange}
                    required
                  />
                </label>
                <label>
                  Min Threshold
                  <input
                    type="number"
                    min={0}
                    step="0.01"
                    name="minThreshold"
                    value={inventoryForm.minThreshold}
                    onChange={handleInventoryFormChange}
                    required
                  />
                </label>
                <label>
                  Max Capacity
                  <input
                    type="number"
                    min={0.0001}
                    step="0.01"
                    name="maxCapacity"
                    value={inventoryForm.maxCapacity}
                    onChange={handleInventoryFormChange}
                    required
                  />
                </label>
              </div>

              <div className="event-form-actions">
                <button type="submit" className="table-action-btn" disabled={isCreatingInventory}>
                  {isCreatingInventory ? 'Creating...' : 'Create Inventory'}
                </button>
              </div>
            </form>
          </AppCard>

          {inventoryEditForm ? (
            <AppCard
              title={`Edit Inventory #${inventoryEditForm.inventoryId}`}
              subtitle={`${inventoryEditForm.warehouseName} / ${inventoryEditForm.resourceName}`}
            >
              <form className="event-create-form" onSubmit={handleUpdateInventory}>
                {inventoryEditError ? (
                  <AlertBanner variant="warning" title="Inventory update" message={inventoryEditError} />
                ) : null}

                <p className="event-form-meta">
                  Version token:{' '}
                  <span className="mono-cell">{inventoryEditForm.versionToken}</span>
                </p>

                <div className="event-form-grid">
                  <label>
                    Quantity
                    <input
                      type="number"
                      min={0}
                      step="0.01"
                      name="quantity"
                      value={inventoryEditForm.quantity}
                      onChange={handleInventoryEditFormChange}
                      required
                    />
                  </label>
                  <label>
                    Min Threshold
                    <input
                      type="number"
                      min={0}
                      step="0.01"
                      name="minThreshold"
                      value={inventoryEditForm.minThreshold}
                      onChange={handleInventoryEditFormChange}
                      required
                    />
                  </label>
                  <label>
                    Max Capacity
                    <input
                      type="number"
                      min={0.0001}
                      step="0.01"
                      name="maxCapacity"
                      value={inventoryEditForm.maxCapacity}
                      onChange={handleInventoryEditFormChange}
                      required
                    />
                  </label>
                </div>

                <div className="event-form-actions">
                  <button type="submit" className="table-action-btn" disabled={isUpdatingInventory}>
                    {isUpdatingInventory ? 'Saving...' : 'Save Levels'}
                  </button>
                  <button
                    type="button"
                    className="table-action-btn"
                    onClick={() => setInventoryEditForm(null)}
                  >
                    Cancel
                  </button>
                </div>
              </form>
            </AppCard>
          ) : null}

          <AppCard
            title="Low-Stock Alerts"
            subtitle="Monitor active and resolved inventory alerts from automation triggers."
            actions={
              <div className="toolbar-inline">
                <label htmlFor="alertStatusFilter" className="toolbar-label">
                  Status
                </label>
                <select
                  id="alertStatusFilter"
                  value={alertStatusFilter}
                  onChange={(event) => setAlertStatusFilter(event.target.value)}
                >
                  <option value="">All</option>
                  {ALERT_STATUSES.map((item) => (
                    <option key={item} value={item}>
                      {item}
                    </option>
                  ))}
                </select>

                <label htmlFor="alertInventoryFilter" className="toolbar-label">
                  Inventory
                </label>
                <input
                  id="alertInventoryFilter"
                  type="number"
                  min={1}
                  placeholder="Inventory ID"
                  value={alertInventoryFilter}
                  onChange={(event) => setAlertInventoryFilter(event.target.value)}
                />

                <label htmlFor="alertWarehouseFilter" className="toolbar-label">
                  Warehouse
                </label>
                <input
                  id="alertWarehouseFilter"
                  placeholder="Warehouse name"
                  value={alertWarehouseFilter}
                  onChange={(event) => setAlertWarehouseFilter(event.target.value)}
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
              caption="Inventory alert board"
              columns={alertColumns}
              rows={filteredAlerts}
              getRowKey={(row) => `${row.inventoryId}-${row.alertId}`}
              emptyMessage="No alerts matched the selected status filter."
            />
          </AppCard>

          <AppCard
            title="Inventory History"
            subtitle="Inspect inventory movements and export the timeline as CSV for reporting."
            actions={
              <div className="toolbar-inline">
                <label htmlFor="historyInventoryFilter" className="toolbar-label">
                  Inventory
                </label>
                <select
                  id="historyInventoryFilter"
                  value={historyInventoryFilter}
                  onChange={(event) => setHistoryInventoryFilter(event.target.value)}
                >
                  <option value="">Select inventory</option>
                  {inventories.map((i) => (
                    <option key={i.inventoryId} value={i.inventoryId}>
                      #{i.inventoryId} - {i.resourceName}
                    </option>
                  ))}
                </select>

                <label htmlFor="historyStartDate" className="toolbar-label">
                  Start
                </label>
                <input
                  id="historyStartDate"
                  type="date"
                  value={historyStartDate}
                  onChange={(event) => setHistoryStartDate(event.target.value)}
                />

                <label htmlFor="historyEndDate" className="toolbar-label">
                  End
                </label>
                <input
                  id="historyEndDate"
                  type="date"
                  value={historyEndDate}
                  onChange={(event) => setHistoryEndDate(event.target.value)}
                />

                <button
                  type="button"
                  className="table-action-btn"
                  onClick={loadInventoryHistory}
                  disabled={isLoadingInventoryHistory}
                >
                  {isLoadingInventoryHistory ? 'Loading...' : 'Load History'}
                </button>
                <button
                  type="button"
                  className="table-action-btn"
                  onClick={handleExportInventoryHistory}
                  disabled={isExportingInventoryHistory}
                >
                  {isExportingInventoryHistory ? 'Exporting...' : 'Export CSV'}
                </button>
              </div>
            }
          >
            {inventoryHistoryError ? (
              <AlertBanner variant="warning" title="Inventory history" message={inventoryHistoryError} />
            ) : null}

            <DataTable
              caption="Inventory movement history"
              columns={inventoryHistoryColumns}
              rows={inventoryHistoryRows}
              getRowKey={(row) => `${row.allocationId}-${row.movementType}-${row.movementTime}`}
              emptyMessage="Load a specific inventory ID to view movement history."
            />
          </AppCard>

          <AppCard
            title="Warehouse Inventory History"
            subtitle="Summarize stock and allocation activity across all inventories in a warehouse."
            actions={
              <div className="toolbar-inline">
                <label htmlFor="warehouseHistoryFilter" className="toolbar-label">
                  Warehouse
                </label>
                <select
                  id="warehouseHistoryFilter"
                  value={warehouseHistoryFilter}
                  onChange={(event) => setWarehouseHistoryFilter(event.target.value)}
                >
                  <option value="">Select warehouse</option>
                  {warehouses.map((w) => (
                    <option key={w.warehouseId} value={w.warehouseId}>
                      #{w.warehouseId} - {w.warehouseName}
                    </option>
                  ))}
                </select>

                <button
                  type="button"
                  className="table-action-btn"
                  onClick={loadWarehouseHistory}
                  disabled={isLoadingWarehouseHistory}
                >
                  {isLoadingWarehouseHistory ? 'Loading...' : 'Load Summary'}
                </button>
              </div>
            }
          >
            {warehouseHistoryError ? (
              <AlertBanner
                variant="warning"
                title="Warehouse history"
                message={warehouseHistoryError}
              />
            ) : null}

            <DataTable
              caption="Warehouse inventory summary"
              columns={warehouseHistoryColumns}
              rows={warehouseHistoryRows}
              getRowKey={(row) => row.inventoryId}
              emptyMessage="Load a warehouse ID to review aggregate inventory history."
            />
          </AppCard>
        </>
      )}

      {isAllocations && (
        <>
          <AppCard
            title="Resource Allocations"
            subtitle="Create allocation requests and progress status through approval and dispatch lifecycle."
            actions={
              <div className="toolbar-inline">
                <label htmlFor="allocationEventFilter" className="toolbar-label">
                  Event
                </label>
                <input
                  id="allocationEventFilter"
                  type="number"
                  min={1}
                  placeholder="Event ID"
                  value={allocationEventFilter}
                  onChange={(event) => setAllocationEventFilter(event.target.value)}
                />

                <label htmlFor="allocationInventoryFilter" className="toolbar-label">
                  Inventory
                </label>
                <input
                  id="allocationInventoryFilter"
                  type="number"
                  min={1}
                  placeholder="Inventory ID"
                  value={allocationInventoryFilter}
                  onChange={(event) => setAllocationInventoryFilter(event.target.value)}
                />

                <label htmlFor="allocationStatusFilter" className="toolbar-label">
                  Status
                </label>
                <select
                  id="allocationStatusFilter"
                  value={allocationStatusFilter}
                  onChange={(event) => setAllocationStatusFilter(event.target.value)}
                >
                  <option value="">All</option>
                  {ALLOCATION_STATUSES.map((item) => (
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
              caption="Allocation request board"
              columns={allocationColumns}
              rows={allocations}
              getRowKey={(row) => row.allocationId}
              emptyMessage="No allocations matched current filters."
            />
          </AppCard>

          <AppCard title="Create Allocation" subtitle="Submit allocation requests against inventory and disaster events.">
            <form className="event-create-form" onSubmit={handleCreateAllocation}>
              {allocationFormError ? (
                <AlertBanner variant="warning" title="Allocation validation" message={allocationFormError} />
              ) : null}

              <div className="event-form-grid">
                <label>
                  Inventory ID
                  <select
                    name="inventoryId"
                    value={allocationForm.inventoryId}
                    onChange={handleAllocationFormChange}
                    required
                  >
                    <option value="">Select an inventory</option>
                    {inventories
                      .filter((i) => i.quantity > 0)
                      .map((i) => (
                        <option key={i.inventoryId} value={i.inventoryId}>
                          #{i.inventoryId} - {i.resourceName} ({i.warehouseName})
                        </option>
                      ))}
                  </select>
                </label>
                <label>
                  Event ID
                  <select
                    name="eventId"
                    value={allocationForm.eventId}
                    onChange={handleAllocationFormChange}
                    required
                  >
                    <option value="">Select an event</option>
                    {disasterEvents.map((e) => (
                      <option key={e.eventId} value={e.eventId}>
                        #{e.eventId} - {e.eventName} ({e.city})
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Quantity
                  <input
                    type="number"
                    min={0.0001}
                    step="0.01"
                    name="quantity"
                    value={allocationForm.quantity}
                    onChange={handleAllocationFormChange}
                    required
                  />
                </label>
                <label>
                  Initial Status
                  <select
                    name="status"
                    value={allocationForm.status}
                    onChange={handleAllocationFormChange}
                  >
                    {ALLOCATION_STATUSES.map((item) => (
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
                    checked={allocationForm.requiresApproval}
                    onChange={handleAllocationFormChange}
                  />
                  Requires approval
                </label>
                <label>
                  Approval Requested By (optional)
                  <input
                    type="number"
                    min={1}
                    name="approvalRequestedBy"
                    value={allocationForm.approvalRequestedBy}
                    onChange={handleAllocationFormChange}
                  />
                </label>
              </div>

              <div className="event-form-actions">
                <button type="submit" className="table-action-btn" disabled={isCreatingAllocation}>
                  {isCreatingAllocation ? 'Creating...' : 'Create Allocation'}
                </button>
              </div>
            </form>
          </AppCard>
        </>
      )}
    </div>
  )
}
