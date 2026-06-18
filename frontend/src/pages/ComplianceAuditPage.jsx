import { useCallback, useEffect, useMemo, useState , useRef} from 'react'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { getApprovalHistory, getAuditLogs } from '../services/api/auditComplianceApi'

const DECISION_OPTIONS = ['Approved', 'Rejected', 'Escalated']

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

export function ComplianceAuditPage() {
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)

  const [auditLogs, setAuditLogs] = useState([])
  const [approvalHistory, setApprovalHistory] = useState([])

  const [auditTableFilter, setAuditTableFilter] = useState('')
  const [auditActionFilter, setAuditActionFilter] = useState('')
  const [auditUserIdFilter, setAuditUserIdFilter] = useState('')
  const [auditFromFilter, setAuditFromFilter] = useState('')
  const [auditToFilter, setAuditToFilter] = useState('')
  const [auditLimit, setAuditLimit] = useState(100)

  const [approvalDecisionFilter, setApprovalDecisionFilter] = useState('')

  const [selectedAuditLog, setSelectedAuditLog] = useState(null)
  const [selectedApprovalEntry, setSelectedApprovalEntry] = useState(null)

  
  const filtersRef = useRef({ auditTableFilter, auditActionFilter, auditUserIdFilter, auditFromFilter, auditToFilter, approvalDecisionFilter })
  filtersRef.current = { auditTableFilter, auditActionFilter, auditUserIdFilter, auditFromFilter, auditToFilter, approvalDecisionFilter }

const loadData = useCallback(
    async ({ refreshOnly = false } = {}) => {
      if (refreshOnly) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }

      try {
        const [auditData, approvalData] = await Promise.all([
          getAuditLogs({
            ...(filtersRef.current.auditTableFilter ? { tableName: filtersRef.current.auditTableFilter.trim() } : {}),
            ...(filtersRef.current.auditActionFilter ? { action: filtersRef.current.auditActionFilter.trim() } : {}),
            ...(filtersRef.current.auditUserIdFilter ? { userId: Number(filtersRef.current.auditUserIdFilter) } : {}),
            ...(filtersRef.current.auditFromFilter ? { from: new Date(filtersRef.current.auditFromFilter).toISOString() } : {}),
            ...(filtersRef.current.auditToFilter ? { to: new Date(filtersRef.current.auditToFilter).toISOString() } : {}),
            limit: auditLimit,
          }),
          getApprovalHistory({
            ...(filtersRef.current.approvalDecisionFilter ? { decision: filtersRef.current.approvalDecisionFilter } : {}),
          }),
        ])

        const auditRows = Array.isArray(auditData) ? auditData : []
        const approvalRows = Array.isArray(approvalData) ? approvalData : []

        setAuditLogs(auditRows)
        setApprovalHistory(approvalRows)

        if (!selectedAuditLog && auditRows.length > 0) {
          setSelectedAuditLog(auditRows[0])
        }

        if (!selectedApprovalEntry && approvalRows.length > 0) {
          setSelectedApprovalEntry(approvalRows[0])
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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const auditColumns = useMemo(
    () => [
      {
        key: 'logId',
        header: 'Log',
        render: (row) => <span className="mono-cell">#{row.logId}</span>,
      },
      {
        key: 'tableName',
        header: 'Table',
      },
      {
        key: 'action',
        header: 'Action',
        render: (row) => <StatusBadge label={row.action || '-'} status={row.action || 'active'} />,
      },
      {
        key: 'recordId',
        header: 'Record',
        render: (row) => <span className="mono-cell">{row.recordId || '-'}</span>,
      },
      {
        key: 'username',
        header: 'User',
        render: (row) => row.username || (row.userId ? `User #${row.userId}` : '-'),
      },
      {
        key: 'timestamp',
        header: 'Timestamp',
        render: (row) => formatDateTime(row.timestamp),
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <button
            type="button"
            className="table-action-btn"
            onClick={() => setSelectedAuditLog(row)}
          >
            Open
          </button>
        ),
      },
    ],
    [],
  )

  const approvalColumns = useMemo(
    () => [
      {
        key: 'requestId',
        header: 'Request',
        render: (row) => <span className="mono-cell">#{row.requestId}</span>,
      },
      {
        key: 'historyId',
        header: 'History',
        render: (row) => <span className="mono-cell">#{row.historyId}</span>,
      },
      {
        key: 'decision',
        header: 'Decision',
        render: (row) => <StatusBadge label={row.decision || '-'} status={row.decision || 'active'} />,
      },
      {
        key: 'actionByName',
        header: 'Action By',
        render: (row) => row.actionByName || (row.actionBy ? `User #${row.actionBy}` : '-'),
      },
      {
        key: 'actionTime',
        header: 'Action Time',
        render: (row) => formatDateTime(row.actionTime),
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <button
            type="button"
            className="table-action-btn"
            onClick={() => setSelectedApprovalEntry(row)}
          >
            Open
          </button>
        ),
      },
    ],
    [],
  )

  if (isLoading) {
    return (
      <LoadingState
        title="Loading audit monitoring"
        message="Preparing audit logs and approval history explorer views."
      />
    )
  }

  return (
    <div>
<AppCard
        title="Audit Log Explorer"
        subtitle="Search and inspect audit logs with filter controls and detailed payload snapshots."
        actions={
          <div className="toolbar-inline">
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
        <div className="event-form-grid" style={{ marginBottom: '0.8rem' }}>
          <label>
            Table Name
            <input
              value={auditTableFilter}
              onChange={(event) => setAuditTableFilter(event.target.value)}
              placeholder="Expense"
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
              placeholder="1"
            />
          </label>
          <label>
            From
            <input
              type="datetime-local"
              value={auditFromFilter}
              onChange={(event) => setAuditFromFilter(event.target.value)}
            />
          </label>
          <label>
            To
            <input
              type="datetime-local"
              value={auditToFilter}
              onChange={(event) => setAuditToFilter(event.target.value)}
            />
          </label>
          <label>
            Limit
            <select
              value={auditLimit}
              onChange={(event) => setAuditLimit(Number(event.target.value))}
            >
              <option value={50}>50</option>
              <option value={100}>100</option>
              <option value={250}>250</option>
              <option value={500}>500</option>
            </select>
          </label>
        </div>

        <DataTable
          caption="System audit logs"
          columns={auditColumns}
          rows={auditLogs}
          getRowKey={(row) => row.logId}
          emptyMessage="No audit logs matched the selected filters."
        />
      </AppCard>

      <AppCard
        title="Audit Log Detail"
        subtitle="Review previous and updated values for the selected audit entry."
      >
        {selectedAuditLog ? (
          <div className="event-create-form">
            <p className="event-form-meta">
              Log #{selectedAuditLog.logId} on {selectedAuditLog.tableName} ({selectedAuditLog.action}) at {formatDateTime(selectedAuditLog.timestamp)}
            </p>
            <p className="event-form-meta">
              Record: {selectedAuditLog.recordId || '-'} | User: {selectedAuditLog.username || (selectedAuditLog.userId ? `#${selectedAuditLog.userId}` : '-')}
            </p>

            <div className="event-form-grid">
              <label>
                Old Value
                <textarea
                  value={stringifyJson(selectedAuditLog.oldValue)}
                  readOnly
                  rows={10}
                />
              </label>
              <label>
                New Value
                <textarea
                  value={stringifyJson(selectedAuditLog.newValue)}
                  readOnly
                  rows={10}
                />
              </label>
            </div>
          </div>
        ) : (
          <AlertBanner
            variant="warning"
            title="No audit log selected"
            message="Open a row in the audit explorer to inspect old/new values."
          />
        )}
      </AppCard>

      <AppCard
        title="Approval History Explorer"
        subtitle="Filter organization-wide approval decisions and inspect comments timeline entries."
      >
        <div className="toolbar-inline" style={{ marginBottom: '0.8rem' }}>
          <label htmlFor="approvalDecisionFilter" className="toolbar-label">
            Decision
          </label>
          <select
            id="approvalDecisionFilter"
            value={approvalDecisionFilter}
            onChange={(event) => setApprovalDecisionFilter(event.target.value)}
          >
            <option value="">All</option>
            {DECISION_OPTIONS.map((decision) => (
              <option key={decision} value={decision}>
                {decision}
              </option>
            ))}
          </select>
        </div>

        <DataTable
          caption="Approval history"
          columns={approvalColumns}
          rows={approvalHistory}
          getRowKey={(row) => `${row.requestId}-${row.historyId}`}
          emptyMessage="No approval history matched the selected filter."
        />
      </AppCard>

      <AppCard
        title="Approval Entry Detail"
        subtitle="Review selected approval decision comments and actor metadata."
      >
        {selectedApprovalEntry ? (
          <div className="event-create-form">
            <p className="event-form-meta">
              Request #{selectedApprovalEntry.requestId}, History #{selectedApprovalEntry.historyId}
            </p>
            <p className="event-form-meta">
              Decision: {selectedApprovalEntry.decision || '-'} | Action by: {selectedApprovalEntry.actionByName || (selectedApprovalEntry.actionBy ? `#${selectedApprovalEntry.actionBy}` : '-')}
            </p>
            <p className="event-form-meta">
              Action time: {formatDateTime(selectedApprovalEntry.actionTime)}
            </p>

            <label>
              Comments
              <textarea value={selectedApprovalEntry.comments || '-'} readOnly rows={6} />
            </label>
          </div>
        ) : (
          <AlertBanner
            variant="warning"
            title="No approval entry selected"
            message="Open a history row to inspect its decision metadata and comments."
          />
        )}
      </AppCard>
    </div>
  )
}
