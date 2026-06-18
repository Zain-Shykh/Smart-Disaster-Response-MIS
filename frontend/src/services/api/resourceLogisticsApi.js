import apiClient from './client'

export async function getResources(query = {}) {
  const { data } = await apiClient.get('ResourceLogistics/resources', { params: query })
  return data
}

export async function createResource(payload) {
  const { data } = await apiClient.post('ResourceLogistics/resources', payload)
  return data
}

export async function getWarehouses(query = {}) {
  const { data } = await apiClient.get('ResourceLogistics/warehouses', { params: query })
  return data
}

export async function createWarehouse(payload) {
  const { data } = await apiClient.post('ResourceLogistics/warehouses', payload)
  return data
}

export async function getInventories(query = {}) {
  const { data } = await apiClient.get('ResourceLogistics/inventories', { params: query })
  return data
}

export async function createInventory(payload) {
  const { data } = await apiClient.post('ResourceLogistics/inventories', payload)
  return data
}

export async function updateInventoryLevels(inventoryId, payload) {
  const { data } = await apiClient.patch(`ResourceLogistics/inventories/${inventoryId}`, payload)
  return data
}

export async function getInventoryAlerts(query = {}) {
  const { data } = await apiClient.get('ResourceLogistics/alerts', { params: query })
  return data
}

export async function getAllocations(query = {}) {
  const { data } = await apiClient.get('ResourceLogistics/allocations', { params: query })
  return data
}

export async function createAllocation(payload) {
  const { data } = await apiClient.post('ResourceLogistics/allocations', payload)
  return data
}

export async function updateAllocationStatus(allocationId, payload) {
  const { data } = await apiClient.patch(
    `ResourceLogistics/allocations/${allocationId}/status`,
    payload,
  )
  return data
}

export async function getInventoryHistory(inventoryId, query = {}) {
  const { data } = await apiClient.get(`InventoryHistory/inventory/${inventoryId}/history`, {
    params: query,
  })
  return data
}

export async function getWarehouseInventoryHistory(warehouseId) {
  const { data } = await apiClient.get(`InventoryHistory/warehouse/${warehouseId}/history`)
  return data
}

export async function downloadInventoryHistoryCsv(inventoryId, query = {}) {
  const response = await apiClient.get(`InventoryHistory/inventory/${inventoryId}/history/export`, {
    params: {
      ...query,
      format: 'csv',
    },
    responseType: 'blob',
  })

  const contentDisposition = response.headers['content-disposition'] || ''
  const fileNameMatch = contentDisposition.match(/filename="?([^";]+)"?/i)
  const fileName = fileNameMatch?.[1] || `inventory_${inventoryId}_history.csv`

  return {
    blob: response.data,
    fileName,
  }
}
