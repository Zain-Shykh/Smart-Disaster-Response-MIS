import { useCallback, useEffect, useMemo, useState , useRef} from 'react'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useAuth } from '../context/AuthContext'
import { useNotification } from '../context/NotificationContext'
import {
  decideApprovalRequest,
  getAllApprovalHistory,
  getApprovalHistory,
  getApprovalRequests,
  getApprovalRequest,
} from '../services/api/approvalWorkflowApi'

const REQUEST_TYPES = ['ResourceDistribution', 'RescueDeployment', 'Financial']
const REQUEST_STATUSES = ['Pending', 'Approved', 'Rejected']
const HISTORY_DECISIONS = ['Approved', 'Rejected', 'Escalated']

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

export function ApprovalWorkflowPage() {
  const { user } = useAuth()
  const { notify } = useNotification()

  const [requests, setRequests] = useState([])
  const [selectedRequest, setSelectedRequest] = useState(null)
  const [requestHistory, setRequestHistory] = useState([])
  const [allHistory, setAllHistory] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [isDeciding, setIsDeciding] = useState(false)
  const [selectedRequestId, setSelectedRequestId] = useState('')

  const [requestTypeFilter, setRequestTypeFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [historyDecisionFilter, setHistoryDecisionFilter] = useState('')

  const [decisionForm, setDecisionForm] = useState({
    decision: 'Approved',
    comments: '',
  })
  const [decisionError, setDecisionError] = useState('')

  
  const filtersRef = useRef({ requestTypeFilter, statusFilter, historyDecisionFilter })
  filtersRef.current = { requestTypeFilter, statusFilter, historyDecisionFilter }

const loadData = useCallback(
    async ({ refreshOnly = false } = {}) => {
      if (refreshOnly) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }

      try {
        const [requestData, historyData] = await Promise.all([
          getApprovalRequests({
            ...(filtersRef.current.requestTypeFilter ? { requestType: filtersRef.current.requestTypeFilter } : {}),
            ...(filtersRef.current.statusFilter ? { status: filtersRef.current.statusFilter } : {}),
          }),
          getAllApprovalHistory(
            filtersRef.current.historyDecisionFilter ? { decision: filtersRef.current.historyDecisionFilter } : {},
          ),
        ])

        setRequests(Array.isArray(requestData) ? requestData : [])
        setAllHistory(Array.isArray(historyData) ? historyData : [])

        if (!selectedRequestId && Array.isArray(requestData) && requestData.length > 0) {
          setSelectedRequestId(requestData[0].requestId)
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
    async function loadSelectedRequest() {
      if (!selectedRequestId) {
        setSelectedRequest(null)
        setRequestHistory([])
        return
      }

      const requestId = Number(selectedRequestId)
      if (!requestId) {
        return
      }

      try {
        const [requestData, historyData] = await Promise.all([
          getApprovalRequest(requestId),
          getApprovalHistory(requestId),
        ])

        setSelectedRequest(requestData)
        setRequestHistory(Array.isArray(historyData) ? historyData : [])
        setDecisionForm({
          decision: requestData?.status === 'Pending' ? 'Approved' : requestData?.status || 'Approved',
          comments: '',
        })
      } catch {
        setSelectedRequest(null)
        setRequestHistory([])
      }
    }

    loadSelectedRequest()
  }, [selectedRequestId])

  function handleDecisionChange(event) {
    const { name, value } = event.target
    setDecisionForm((previous) => ({
      ...previous,
      [name]: value,
    }))
  }

  async function handleDecideRequest(event) {
    event.preventDefault()
    setDecisionError('')

    if (!selectedRequest) {
      setDecisionError('Select an approval request first.')
      return
    }

    if (!user?.userId) {
      setDecisionError('Current user context is unavailable. Re-login and try again.')
      return
    }

    setIsDeciding(true)

    try {
      await decideApprovalRequest(selectedRequest.requestId, {
        decision: decisionForm.decision,
        actionBy: user.userId,
        comments: decisionForm.comments.trim() || null,
        versionToken: selectedRequest.versionToken,
      })

      notify({
        title: 'Approval decision saved',
        message: `Request #${selectedRequest.requestId} was updated to ${decisionForm.decision}.`,
        variant: 'success',
      })

      await loadData({ refreshOnly: true })
      const refreshedRequest = await getApprovalRequest(selectedRequest.requestId)
      setSelectedRequest(refreshedRequest)
      setDecisionForm((previous) => ({
        ...previous,
        decision: refreshedRequest.status,
        comments: '',
      }))
      const refreshedHistory = await getApprovalHistory(selectedRequest.requestId)
      setRequestHistory(Array.isArray(refreshedHistory) ? refreshedHistory : [])
    } catch {
      setDecisionError('Unable to save decision. Verify request state and version token.')
    } finally {
      setIsDeciding(false)
    }
  }

  const requestColumns = useMemo(
    () => [
      {
        key: 'requestId',
        header: 'Request',
        render: (row) => <span className="mono-cell">#{row.requestId}</span>,
      },
      {
        key: 'requestType',
        header: 'Type',
        render: (row) => row.requestType,
      },
      {
        key: 'status',
        header: 'Status',
        render: (row) => <StatusBadge label={row.status} status={row.status} />,
      },
      {
        key: 'requestedByName',
        header: 'Requested By',
        render: (row) => row.requestedByName,
      },
      {
        key: 'requestTime',
        header: 'Time',
        render: (row) => formatDateTime(row.requestTime),
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <button
            type="button"
            className="table-action-btn"
            onClick={() => setSelectedRequestId(row.requestId)}
          >
            Open
          </button>
        ),
      },
    ],
    [],
  )

  const requestHistoryColumns = useMemo(
    () => [
      {
        key: 'historyId',
        header: 'History',
        render: (row) => <span className="mono-cell">#{row.historyId}</span>,
      },
      {
        key: 'actionByName',
        header: 'Action By',
        render: (row) => row.actionByName,
      },
      {
        key: 'decision',
        header: 'Decision',
        render: (row) => <StatusBadge label={row.decision} status={row.decision} />,
      },
      {
        key: 'actionTime',
        header: 'Time',
        render: (row) => formatDateTime(row.actionTime),
      },
      {
        key: 'comments',
        header: 'Comments',
        render: (row) => row.comments || '-',
      },
    ],
    [],
  )

  const allHistoryColumns = useMemo(
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
        key: 'actionByName',
        header: 'Actor',
        render: (row) => row.actionByName,
      },
      {
        key: 'decision',
        header: 'Decision',
        render: (row) => <StatusBadge label={row.decision} status={row.decision} />,
      },
      {
        key: 'actionTime',
        header: 'Time',
        render: (row) => formatDateTime(row.actionTime),
      },
    ],
    [],
  )

  if (isLoading) {
    return (
      <LoadingState
        title="Loading approval workflow"
        message="Preparing the approval inbox and decision history from backend services."
      />
    )
  }

  return (
    <div>
<AppCard
        title="Approval Inbox"
        subtitle="Filter pending and reviewed requests before opening the detail panel."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="requestTypeFilter" className="toolbar-label">
              Type
            </label>
            <select
              id="requestTypeFilter"
              value={requestTypeFilter}
              onChange={(event) => setRequestTypeFilter(event.target.value)}
            >
              <option value="">All</option>
              {REQUEST_TYPES.map((item) => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>

            <label htmlFor="statusFilter" className="toolbar-label">
              Status
            </label>
            <select
              id="statusFilter"
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value)}
            >
              <option value="">All</option>
              {REQUEST_STATUSES.map((item) => (
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
          caption="Approval request inbox"
          columns={requestColumns}
          rows={requests}
          getRowKey={(row) => row.requestId}
          emptyMessage="No approval requests matched the selected filters."
        />
      </AppCard>

      <AppCard
        title="Request Detail"
        subtitle="Review the selected request and record an approval decision or escalation."
      >
        {selectedRequest ? (
          <div className="approval-detail-layout">
            <div className="approval-detail-panel">
              <div className="approval-detail-header">
                <h3>{selectedRequest.requestType} #{selectedRequest.requestId}</h3>
                <StatusBadge label={selectedRequest.status} status={selectedRequest.status} />
              </div>
              <p>{selectedRequest.description || 'No description provided.'}</p>
              <dl className="approval-detail-grid">
                <div><dt>Requested By</dt><dd>{selectedRequest.requestedByName}</dd></div>
                <div><dt>Reviewed By</dt><dd>{selectedRequest.reviewedByName || '-'}</dd></div>
                <div><dt>Time</dt><dd>{formatDateTime(selectedRequest.requestTime)}</dd></div>
                <div><dt>Allocation ID</dt><dd>{selectedRequest.allocationId || '-'}</dd></div>
                <div><dt>Assignment ID</dt><dd>{selectedRequest.assignmentId || '-'}</dd></div>
                <div><dt>Expense ID</dt><dd>{selectedRequest.expenseId || '-'}</dd></div>
              </dl>
            </div>

            <form className="event-create-form" onSubmit={handleDecideRequest}>
              {decisionError ? (
                <AlertBanner variant="warning" title="Decision validation" message={decisionError} />
              ) : null}

              <div className="event-form-grid">
                <label>
                  Decision
                  <select name="decision" value={decisionForm.decision} onChange={handleDecisionChange}>
                    {HISTORY_DECISIONS.map((item) => (
                      <option key={item} value={item}>
                        {item}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Comments
                  <textarea
                    name="comments"
                    value={decisionForm.comments}
                    onChange={handleDecisionChange}
                    rows={4}
                  />
                </label>
              </div>

              <div className="event-form-actions">
                <button type="submit" className="table-action-btn" disabled={isDeciding}>
                  {isDeciding ? 'Saving...' : 'Save Decision'}
                </button>
              </div>
            </form>
          </div>
        ) : (
          <AlertBanner
            variant="warning"
            title="No request selected"
            message="Select a request from the inbox to review details and record a decision."
          />
        )}
      </AppCard>

      <AppCard
        title="Request History"
        subtitle="Timeline for the selected request, plus a global approval history feed."
      >
        <DataTable
          caption="Selected request history"
          columns={requestHistoryColumns}
          rows={requestHistory}
          getRowKey={(row) => row.historyId}
          emptyMessage="Select a request to load its history timeline."
        />
      </AppCard>

      <AppCard
        title="Approval History Feed"
        subtitle="All approval history events across the finance and operations workflows."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="historyDecisionFilter" className="toolbar-label">
              Decision
            </label>
            <select
              id="historyDecisionFilter"
              value={historyDecisionFilter}
              onChange={(event) => setHistoryDecisionFilter(event.target.value)}
            >
              <option value="">All</option>
              {HISTORY_DECISIONS.map((item) => (
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
          caption="Global approval history"
          columns={allHistoryColumns}
          rows={allHistory}
          getRowKey={(row) => `${row.requestId}-${row.historyId}`}
          emptyMessage="No approval history entries matched the selected filter."
        />
      </AppCard>
    </div>
  )
}
