import { useCallback, useEffect, useMemo, useState , useRef} from 'react'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import {
  getApprovalSummary,
  getAuditLogs,
  getFinancialSummary,
  getIncidentsByLocation,
  getIncidentsByType,
  getOverviewReport,
  getPrioritizedIncidents,
  getResourceUtilization,
  getIncidentsBySeverity,
  getIncidentTrend,
} from '../services/api/reportsAnalyticsApi'
import { DashboardCharts } from '../components/dashboard/DashboardCharts'

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

function formatNumber(value, digits = 0) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    return '-'
  }

  return Number(value).toFixed(digits)
}

function getPriorityStatus(priorityLabel) {
  const normalized = String(priorityLabel || '').toLowerCase()
  if (normalized.includes('critical')) {
    return 'blocked'
  }

  if (normalized.includes('high')) {
    return 'warning'
  }

  if (normalized.includes('medium')) {
    return 'active'
  }

  return 'planned'
}

function stringifyJson(value) {
  if (!value) {
    return '-'
  }

  if (typeof value !== 'string') {
    return String(value)
  }

  try {
    const parsed = JSON.parse(value)
    return JSON.stringify(parsed, null, 2)
  } catch {
    return value
  }
}

function getErrorMessage(error) {
  if (!error?.response) {
    return 'Unable to reach the backend service. Verify the API is running and try again.'
  }

  const data = error.response.data

  if (typeof data === 'string') {
    return data
  }

  if (typeof data?.detail === 'string') {
    return data.detail
  }

  if (typeof data?.title === 'string') {
    return data.title
  }

  return 'Unable to load incident analytics. Please try again.'
}

function getTopEntry(entries, key) {
  if (!Array.isArray(entries) || entries.length === 0) {
    return null
  }

  return entries.reduce((currentBest, entry) => {
    const currentCount = Number(currentBest?.count ?? currentBest?.[key] ?? 0)
    const nextCount = Number(entry?.count ?? entry?.[key] ?? 0)
    return nextCount > currentCount ? entry : currentBest
  }, entries[0])
}

export function IncidentAnalyticsPage() {
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')

  const [locationRows, setLocationRows] = useState([])
  const [typeRows, setTypeRows] = useState([])
  const [prioritizedRows, setPrioritizedRows] = useState([])
  const [resourceRows, setResourceRows] = useState([])
  const [overview, setOverview] = useState(null)
  const [financialSummary, setFinancialSummary] = useState(null)
  const [approvalSummary, setApprovalSummary] = useState(null)
  const [auditLogs, setAuditLogs] = useState([])
  const [selectedAuditLog, setSelectedAuditLog] = useState(null)

  const [fromFilter, setFromFilter] = useState('')
  const [toFilter, setToFilter] = useState('')
  const [prioritizedLimit, setPrioritizedLimit] = useState(50)
  const [resourceEventIdFilter, setResourceEventIdFilter] = useState('')
  const [financialEventIdFilter, setFinancialEventIdFilter] = useState('')
  const [auditTableFilter, setAuditTableFilter] = useState('')
  const [auditActionFilter, setAuditActionFilter] = useState('')
  const [auditUserIdFilter, setAuditUserIdFilter] = useState('')
  const [auditLimit, setAuditLimit] = useState(100)

  
  const filtersRef = useRef({ fromFilter, toFilter, resourceEventIdFilter, financialEventIdFilter, auditTableFilter, auditActionFilter, auditUserIdFilter })
  filtersRef.current = { fromFilter, toFilter, resourceEventIdFilter, financialEventIdFilter, auditTableFilter, auditActionFilter, auditUserIdFilter }

const loadData = useCallback(
    async ({ refreshOnly = false } = {}) => {
      setErrorMessage('')

      if (refreshOnly) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }

      try {
        const commonDateQuery = {
          ...(filtersRef.current.fromFilter ? { from: new Date(filtersRef.current.fromFilter).toISOString() } : {}),
          ...(filtersRef.current.toFilter ? { to: new Date(filtersRef.current.toFilter).toISOString() } : {}),
        }

        const [locations, types, prioritized, resources, overviewData, financialData, approvalsData, auditData] = await Promise.all([
          getIncidentsByLocation(commonDateQuery),
          getIncidentsByType(commonDateQuery),
          getPrioritizedIncidents({ limit: prioritizedLimit }),
          getResourceUtilization({
            ...(filtersRef.current.resourceEventIdFilter ? { eventId: Number(filtersRef.current.resourceEventIdFilter) } : {}),
          }),
          getOverviewReport(),
          getFinancialSummary({
            ...(filtersRef.current.financialEventIdFilter ? { eventId: Number(filtersRef.current.financialEventIdFilter) } : {}),
          }),
          getApprovalSummary(),
          getAuditLogs({
            ...(filtersRef.current.auditTableFilter ? { tableName: filtersRef.current.auditTableFilter.trim() } : {}),
            ...(filtersRef.current.auditActionFilter ? { action: filtersRef.current.auditActionFilter.trim() } : {}),
            ...(filtersRef.current.auditUserIdFilter ? { userId: Number(filtersRef.current.auditUserIdFilter) } : {}),
            ...(filtersRef.current.fromFilter ? { from: new Date(filtersRef.current.fromFilter).toISOString() } : {}),
            ...(filtersRef.current.toFilter ? { to: new Date(filtersRef.current.toFilter).toISOString() } : {}),
            limit: auditLimit,
          }),
        ])

        setLocationRows(Array.isArray(locations) ? locations : [])
        setTypeRows(Array.isArray(types) ? types : [])
        setPrioritizedRows(Array.isArray(prioritized) ? prioritized : [])
        setResourceRows(Array.isArray(resources) ? resources : [])
        setOverview(overviewData || null)
        setFinancialSummary(financialData || null)
        setApprovalSummary(approvalsData || null)
        setAuditLogs(Array.isArray(auditData) ? auditData : [])

        if (!selectedAuditLog && Array.isArray(auditData) && auditData.length > 0) {
          setSelectedAuditLog(auditData[0])
        }
      } catch (error) {
        setErrorMessage(getErrorMessage(error))
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

  const locationColumns = useMemo(
    () => [
      {
        key: 'location',
        header: 'Location',
        render: (row) => `${row.city}, ${row.province}`,
      },
      {
        key: 'totalReports',
        header: 'Total Reports',
        align: 'right',
      },
      {
        key: 'criticalReports',
        header: 'Critical',
        align: 'right',
      },
      {
        key: 'highReports',
        header: 'High',
        align: 'right',
      },
    ],
    [],
  )

  const typeColumns = useMemo(
    () => [
      {
        key: 'disasterType',
        header: 'Disaster Type',
      },
      {
        key: 'totalReports',
        header: 'Total Reports',
        align: 'right',
      },
      {
        key: 'averageResponseMinutes',
        header: 'Avg Response (min)',
        align: 'right',
        render: (row) => formatNumber(row.averageResponseMinutes, 1),
      },
      {
        key: 'averageResolutionMinutes',
        header: 'Avg Resolution (min)',
        align: 'right',
        render: (row) => formatNumber(row.averageResolutionMinutes, 1),
      },
    ],
    [],
  )

  const prioritizedColumns = useMemo(
    () => [
      {
        key: 'reportId',
        header: 'Report',
        render: (row) => <span className="mono-cell">#{row.reportId}</span>,
      },
      {
        key: 'eventName',
        header: 'Event',
        render: (row) => row.eventName || `Event #${row.eventId || '-'}`,
      },
      {
        key: 'city',
        header: 'City',
      },
      {
        key: 'disasterType',
        header: 'Type',
      },
      {
        key: 'priorityLabel',
        header: 'Priority',
        render: (row) => (
          <StatusBadge
            label={`${row.priorityLabel || 'Unknown'} (${formatNumber(row.priorityScore, 2)})`}
            status={getPriorityStatus(row.priorityLabel)}
          />
        ),
      },
      {
        key: 'estimatedResponseMinutes',
        header: 'Est. Response (min)',
        align: 'right',
      },
      {
        key: 'reportTime',
        header: 'Reported At',
        render: (row) => formatDateTime(row.reportTime),
      },
    ],
    [],
  )

  const resourceColumns = useMemo(
    () => [
      {
        key: 'resourceName',
        header: 'Resource',
        render: (row) => (
          <div>
            <strong>{row.resourceName}</strong>
            <div className="table-subtext">{row.resourceType}</div>
          </div>
        ),
      },
      {
        key: 'requestedQuantity',
        header: 'Requested',
        align: 'right',
        render: (row) => formatNumber(row.requestedQuantity, 2),
      },
      {
        key: 'dispatchedQuantity',
        header: 'Dispatched',
        align: 'right',
        render: (row) => formatNumber(row.dispatchedQuantity, 2),
      },
      {
        key: 'consumedQuantity',
        header: 'Consumed',
        align: 'right',
        render: (row) => formatNumber(row.consumedQuantity, 2),
      },
      {
        key: 'currentStock',
        header: 'Current Stock',
        align: 'right',
        render: (row) => formatNumber(row.currentStock, 2),
      },
      {
        key: 'fulfillment',
        header: 'Fulfillment',
        align: 'right',
        render: (row) => {
          const requested = Number(row.requestedQuantity) || 0
          const dispatched = Number(row.dispatchedQuantity) || 0
          const fulfillment = requested > 0 ? (dispatched / requested) * 100 : 0
          return `${formatNumber(fulfillment, 1)}%`
        },
      },
    ],
    [],
  )

  const logisticsKpis = useMemo(() => {
    const totals = resourceRows.reduce(
      (acc, row) => {
        acc.requested += Number(row.requestedQuantity) || 0
        acc.dispatched += Number(row.dispatchedQuantity) || 0
        acc.consumed += Number(row.consumedQuantity) || 0
        acc.stock += Number(row.currentStock) || 0
        return acc
      },
      { requested: 0, dispatched: 0, consumed: 0, stock: 0 },
    )

    const dispatchFulfillment = totals.requested > 0
      ? (totals.dispatched / totals.requested) * 100
      : 0

    return {
      totals,
      dispatchFulfillment,
      lowStockInventories: overview?.lowStockInventories || 0,
    }
  }, [resourceRows, overview])

  const donationSummaryRows = useMemo(
    () => (Array.isArray(financialSummary?.donationSummary) ? financialSummary.donationSummary : []),
    [financialSummary],
  )

  const expenseSummaryRows = useMemo(
    () => (Array.isArray(financialSummary?.expenseSummary) ? financialSummary.expenseSummary : []),
    [financialSummary],
  )

  const approvalRequestStatusRows = useMemo(
    () => (Array.isArray(approvalSummary?.requestStatusSummary) ? approvalSummary.requestStatusSummary : []),
    [approvalSummary],
  )

  const approvalDecisionRows = useMemo(
    () => (Array.isArray(approvalSummary?.decisionSummary) ? approvalSummary.decisionSummary : []),
    [approvalSummary],
  )

  const donationColumns = useMemo(
    () => [
      {
        key: 'status',
        header: 'Status',
        render: (row) => <StatusBadge label={row.status} status={row.status} />,
      },
      {
        key: 'count',
        header: 'Count',
        align: 'right',
      },
      {
        key: 'amount',
        header: 'Amount',
        align: 'right',
        render: (row) => formatNumber(row.amount, 2),
      },
    ],
    [],
  )

  const expenseColumns = useMemo(
    () => [
      {
        key: 'category',
        header: 'Category',
      },
      {
        key: 'count',
        header: 'Count',
        align: 'right',
      },
      {
        key: 'amount',
        header: 'Amount',
        align: 'right',
        render: (row) => formatNumber(row.amount, 2),
      },
    ],
    [],
  )

  const approvalRequestColumns = useMemo(
    () => [
      {
        key: 'requestType',
        header: 'Request Type',
      },
      {
        key: 'status',
        header: 'Status',
        render: (row) => <StatusBadge label={row.status} status={row.status} />,
      },
      {
        key: 'count',
        header: 'Count',
        align: 'right',
      },
    ],
    [],
  )

  const approvalDecisionColumns = useMemo(
    () => [
      {
        key: 'decision',
        header: 'Decision',
        render: (row) => <StatusBadge label={row.decision} status={row.decision} />,
      },
      {
        key: 'count',
        header: 'Count',
        align: 'right',
      },
    ],
    [],
  )

  const auditColumns = useMemo(
    () => [
      {
        key: 'logId',
        header: 'Log',
        render: (row) => <span className="mono-cell">#{row.logId}</span>,
      },
      {
        key: 'timestamp',
        header: 'Timestamp',
        render: (row) => formatDateTime(row.timestamp),
      },
      {
        key: 'action',
        header: 'Action',
        render: (row) => <StatusBadge label={row.action || '-'} status={row.action || 'planned'} />,
      },
      {
        key: 'tableName',
        header: 'Table',
      },
      {
        key: 'recordId',
        header: 'Record',
        render: (row) => <span className="mono-cell">{row.recordId || '-'}</span>,
      },
      {
        key: 'username',
        header: 'Actor',
        render: (row) => row.username || (row.userId ? `User #${row.userId}` : '-'),
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <button type="button" className="table-action-btn" onClick={() => setSelectedAuditLog(row)}>
            Open
          </button>
        ),
      },
    ],
    [],
  )

  const auditActionBreakdown = useMemo(() => {
    const counts = new Map()

    auditLogs.forEach((entry) => {
      const key = entry.action || 'Unknown'
      counts.set(key, (counts.get(key) || 0) + 1)
    })

    return Array.from(counts.entries())
      .map(([action, count]) => ({ action, count }))
      .sort((left, right) => right.count - left.count)
  }, [auditLogs])

  const auditTableBreakdown = useMemo(() => {
    const counts = new Map()

    auditLogs.forEach((entry) => {
      const key = entry.tableName || 'Unknown'
      counts.set(key, (counts.get(key) || 0) + 1)
    })

    return Array.from(counts.entries())
      .map(([tableName, count]) => ({ tableName, count }))
      .sort((left, right) => right.count - left.count)
  }, [auditLogs])

  const auditUsers = useMemo(() => new Set(auditLogs.map((entry) => entry.username || entry.userId).filter(Boolean)).size, [auditLogs])

  const latestAudit = auditLogs[0] || null
  const topAuditAction = getTopEntry(auditActionBreakdown, 'count')
  const topAuditTable = getTopEntry(auditTableBreakdown, 'count')

  if (isLoading) {
    return (
      <LoadingState
        title="Loading incident analytics"
        message="Preparing location, type, and prioritized incident intelligence."
      />
    )
  }

  return (
    <div>
{errorMessage ? (
        <AlertBanner
          variant="danger"
          title="Analytics refresh failed"
          message={errorMessage}
        >
          <button
            type="button"
            className="table-action-btn"
            onClick={() => loadData({ refreshOnly: true })}
            disabled={isRefreshing}
          >
            Retry
          </button>
        </AlertBanner>
      ) : null}

      <AppCard
        title="Incident Analytics Controls"
        subtitle="Filter incident widgets by date range, control prioritized queue depth, and scope resource utilization by event."
        actions={
          <button
            type="button"
            className="table-action-btn"
            onClick={() => loadData({ refreshOnly: true })}
            disabled={isRefreshing}
          >
            {isRefreshing ? 'Refreshing...' : 'Refresh'}
          </button>
        }
      >
        <div className="event-form-grid">
          <label>
            From
            <input
              type="datetime-local"
              value={fromFilter}
              onChange={(event) => setFromFilter(event.target.value)}
            />
          </label>
          <label>
            To
            <input
              type="datetime-local"
              value={toFilter}
              onChange={(event) => setToFilter(event.target.value)}
            />
          </label>
          <label>
            Prioritized Queue Limit
            <select
              value={prioritizedLimit}
              onChange={(event) => setPrioritizedLimit(Number(event.target.value))}
            >
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
              <option value={250}>250</option>
            </select>
          </label>
          <label>
            Resource Event ID
            <input
              type="number"
              min={1}
              value={resourceEventIdFilter}
              onChange={(event) => setResourceEventIdFilter(event.target.value)}
              placeholder="All events"
            />
          </label>
          <label>
            Financial Event ID
            <input
              type="number"
              min={1}
              value={financialEventIdFilter}
              onChange={(event) => setFinancialEventIdFilter(event.target.value)}
              placeholder="All events"
            />
          </label>
        </div>
      </AppCard>

      <div style={{ marginTop: '1.5rem' }}>
        <DashboardCharts />
      </div>

      <AppCard
        title="Audit Monitoring"
        subtitle="Trace user actions, filter by table and action, and drill into record payloads."
        actions={
          <button
            type="button"
            className="table-action-btn"
            onClick={() => loadData({ refreshOnly: true })}
            disabled={isRefreshing}
          >
            {isRefreshing ? 'Refreshing...' : 'Refresh'}
          </button>
        }
      >
        <div className="event-form-grid">
          <label>
            Table Name
            <input
              value={auditTableFilter}
              onChange={(event) => setAuditTableFilter(event.target.value)}
              placeholder="Donation"
            />
          </label>
          <label>
            Action
            <input
              value={auditActionFilter}
              onChange={(event) => setAuditActionFilter(event.target.value)}
              placeholder="INSERT"
            />
          </label>
          <label>
            User ID
            <input
              type="number"
              min={1}
              value={auditUserIdFilter}
              onChange={(event) => setAuditUserIdFilter(event.target.value)}
              placeholder="All users"
            />
          </label>
          <label>
            Limit
            <select value={auditLimit} onChange={(event) => setAuditLimit(Number(event.target.value))}>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
              <option value={250}>250</option>
            </select>
          </label>
        </div>

        <div className="kpi-grid" style={{ marginTop: '1rem' }}>
          <article className="kpi-card">
            <span>Audit Entries</span>
            <strong>{auditLogs.length}</strong>
          </article>
          <article className="kpi-card">
            <span>Unique Actors</span>
            <strong>{auditUsers}</strong>
          </article>
          <article className="kpi-card">
            <span>Top Action</span>
            <strong>
              <StatusBadge label={topAuditAction ? `${topAuditAction.action} (${topAuditAction.count})` : 'None'} status="active" />
            </strong>
          </article>
          <article className="kpi-card">
            <span>Top Table</span>
            <strong>
              <StatusBadge label={topAuditTable ? `${topAuditTable.tableName} (${topAuditTable.count})` : 'None'} status="planned" />
            </strong>
          </article>
        </div>

        <div className="dashboard-grid-two">
          <AppCard title="Action Breakdown" subtitle="Distribution of audit activity by action type.">
            <DataTable
              caption="Audit action breakdown"
              columns={[
                { key: 'action', header: 'Action' },
                { key: 'count', header: 'Count', align: 'right' },
              ]}
              rows={auditActionBreakdown}
              getRowKey={(row) => row.action}
              emptyMessage="No audit actions matched the selected filters."
            />
          </AppCard>

          <AppCard title="Table Breakdown" subtitle="Audit activity grouped by affected table.">
            <DataTable
              caption="Audit table breakdown"
              columns={[
                { key: 'tableName', header: 'Table' },
                { key: 'count', header: 'Count', align: 'right' },
              ]}
              rows={auditTableBreakdown}
              getRowKey={(row) => row.tableName}
              emptyMessage="No audit tables matched the selected filters."
            />
          </AppCard>
        </div>

        <DataTable
          caption="Audit log drill-down"
          columns={auditColumns}
          rows={auditLogs}
          getRowKey={(row) => row.logId}
          emptyMessage="No audit logs matched the selected filters."
        />
      </AppCard>

      <AppCard
        title="Audit Log Detail"
        subtitle="Open an audit row to inspect old/new values and trace metadata."
      >
        {selectedAuditLog ? (
          <div className="event-create-form">
            <p className="event-form-meta">
              Log #{selectedAuditLog.logId} | {selectedAuditLog.tableName} | {selectedAuditLog.action} | {formatDateTime(selectedAuditLog.timestamp)}
            </p>
            <p className="event-form-meta">
              Actor: {selectedAuditLog.username || (selectedAuditLog.userId ? `User #${selectedAuditLog.userId}` : '-')} | Record: {selectedAuditLog.recordId || '-'} | IP: {selectedAuditLog.ipAddress || '-'}
            </p>
            <div className="event-form-grid">
              <label>
                Old Value
                <textarea value={stringifyJson(selectedAuditLog.oldValue)} readOnly rows={10} />
              </label>
              <label>
                New Value
                <textarea value={stringifyJson(selectedAuditLog.newValue)} readOnly rows={10} />
              </label>
            </div>
          </div>
        ) : (
          <AlertBanner
            variant="warning"
            title="No audit log selected"
            message="Open an audit row above to inspect payload snapshots."
          />
        )}
      </AppCard>

      <AppCard
        title="Timeline Snapshot"
        subtitle="Latest audit activity for quick monitoring at a glance."
      >
        {latestAudit ? (
          <div className="event-create-form">
            <p className="event-form-meta">
              Latest action: {latestAudit.action} on {latestAudit.tableName} at {formatDateTime(latestAudit.timestamp)}
            </p>
            <p className="event-form-meta">
              Triggered by {latestAudit.username || (latestAudit.userId ? `User #${latestAudit.userId}` : 'unknown user')}.
            </p>
          </div>
        ) : (
          <AlertBanner
            variant="warning"
            title="No audit activity"
            message="No audit logs were returned for the selected filters."
          />
        )}
      </AppCard>

      <div className="kpi-grid" style={{ marginBottom: '1rem' }}>
        <article className="kpi-card">
          <span>Total Requested</span>
          <strong>{formatNumber(logisticsKpis.totals.requested, 2)}</strong>
        </article>
        <article className="kpi-card">
          <span>Total Dispatched</span>
          <strong>{formatNumber(logisticsKpis.totals.dispatched, 2)}</strong>
        </article>
        <article className="kpi-card">
          <span>Dispatch Fulfillment</span>
          <strong>
            <StatusBadge
              label={`${formatNumber(logisticsKpis.dispatchFulfillment, 1)}%`}
              status={logisticsKpis.dispatchFulfillment >= 80 ? 'success' : 'warning'}
            />
          </strong>
        </article>
        <article className="kpi-card">
          <span>Low-Stock Inventories</span>
          <strong>
            <StatusBadge
              label={String(logisticsKpis.lowStockInventories)}
              status={logisticsKpis.lowStockInventories > 0 ? 'blocked' : 'success'}
            />
          </strong>
        </article>
      </div>

      <div className="dashboard-grid-two">
        <AppCard
          title="Incidents by Location"
          subtitle="Distribution of incident load by city and province including severity concentration."
        >
          <DataTable
            caption="Incident distribution by location"
            columns={locationColumns}
            rows={locationRows}
            getRowKey={(row) => `${row.city}-${row.province}`}
            emptyMessage="No location analytics available for the selected date range."
          />
        </AppCard>

        <AppCard
          title="Incidents by Disaster Type"
          subtitle="Volume and average response/resolution timings by disaster category."
        >
          <DataTable
            caption="Incident distribution by disaster type"
            columns={typeColumns}
            rows={typeRows}
            getRowKey={(row) => row.disasterType}
            emptyMessage="No type analytics available for the selected date range."
          />
        </AppCard>
      </div>

      <AppCard
        title="Prioritized Incident Queue"
        subtitle="Operationally ranked incident queue for rapid triage and dispatch decisions."
      >
        <DataTable
          caption="Prioritized incident queue"
          columns={prioritizedColumns}
          rows={prioritizedRows}
          getRowKey={(row) => row.reportId}
          emptyMessage="No prioritized incidents available."
        />
      </AppCard>

      <AppCard
        title="Resource Utilization and Logistics KPIs"
        subtitle="Requested, dispatched, consumed, and current stock trends for logistics performance monitoring."
      >
        <DataTable
          caption="Resource utilization overview"
          columns={resourceColumns}
          rows={resourceRows}
          getRowKey={(row) => row.resourceId}
          emptyMessage="No resource utilization data available for the selected event filter."
        />
      </AppCard>

      <div className="kpi-grid" style={{ marginTop: '1rem', marginBottom: '1rem' }}>
        <article className="kpi-card">
          <span>Confirmed Donations</span>
          <strong>{formatNumber(financialSummary?.confirmedDonationAmount, 2)}</strong>
        </article>
        <article className="kpi-card">
          <span>Total Expenses</span>
          <strong>{formatNumber(financialSummary?.totalExpenseAmount, 2)}</strong>
        </article>
        <article className="kpi-card">
          <span>Net Balance</span>
          <strong>
            <StatusBadge
              label={formatNumber(financialSummary?.netBalance, 2)}
              status={(Number(financialSummary?.netBalance) || 0) >= 0 ? 'success' : 'blocked'}
            />
          </strong>
        </article>
        <article className="kpi-card">
          <span>Approval Decisions Logged</span>
          <strong>{approvalDecisionRows.reduce((sum, item) => sum + (Number(item.count) || 0), 0)}</strong>
        </article>
      </div>

      <div className="dashboard-grid-two">
        <AppCard
          title="Donation Summary"
          subtitle="Donation counts and totals grouped by financial status."
        >
          <DataTable
            caption="Donation status summary"
            columns={donationColumns}
            rows={donationSummaryRows}
            getRowKey={(row) => row.status}
            emptyMessage="No donation summary rows returned for the selected event filter."
          />
        </AppCard>

        <AppCard
          title="Expense Summary"
          subtitle="Expense category totals for budget and burn-rate visibility."
        >
          <DataTable
            caption="Expense category summary"
            columns={expenseColumns}
            rows={expenseSummaryRows}
            getRowKey={(row) => row.category}
            emptyMessage="No expense summary rows returned for the selected event filter."
          />
        </AppCard>
      </div>

      <div className="dashboard-grid-two">
        <AppCard
          title="Approval Request Summary"
          subtitle="Counts by request type and current approval status."
        >
          <DataTable
            caption="Approval request status summary"
            columns={approvalRequestColumns}
            rows={approvalRequestStatusRows}
            getRowKey={(row) => `${row.requestType}-${row.status}`}
            emptyMessage="No approval request summary data returned."
          />
        </AppCard>

        <AppCard
          title="Approval Decision Summary"
          subtitle="Decision volume distribution across approval outcomes."
        >
          <DataTable
            caption="Approval decision summary"
            columns={approvalDecisionColumns}
            rows={approvalDecisionRows}
            getRowKey={(row) => row.decision}
            emptyMessage="No approval decision summary data returned."
          />
        </AppCard>
      </div>
    </div>
  )
}
