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
  createExpense,
  getExpenses,
  updateExpensePaymentStatus,
} from '../services/api/expenseFinanceApi'

const EXPENSE_CATEGORIES = ['Procurement', 'Operations', 'Medical', 'Logistics']
const EXPENSE_PAYMENT_STATUSES = ['Pending', 'Paid', 'Completed', 'Rejected']

function getDefaultExpenseForm() {
  return {
    eventId: '',
    approvedBy: '',
    category: 'Procurement',
    requiresApproval: true,
    approvalRequestedBy: '',
    amount: '',
    description: '',
    expenseDate: new Date().toISOString().slice(0, 16),
    paymentStatus: 'Pending',
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

export function ExpensesPage() {
  const { notify } = useNotification()

  const [expenses, setExpenses] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [isCreatingExpense, setIsCreatingExpense] = useState(false)
  const [expenseActionKey, setExpenseActionKey] = useState('')

  const [eventFilter, setEventFilter] = useState('')
  const [categoryFilter, setCategoryFilter] = useState('')

  const [expenseForm, setExpenseForm] = useState(() => getDefaultExpenseForm())
  const [expenseFormError, setExpenseFormError] = useState('')

  const canCreateExpense =
    isPositiveIntegerString(expenseForm.eventId) &&
    isPositiveDecimalString(expenseForm.amount) &&
    (!expenseForm.requiresApproval || isPositiveIntegerString(expenseForm.approvalRequestedBy || expenseForm.approvedBy)) &&
    (!expenseForm.description || hasTrimmedText(expenseForm.description, 3))

  
  const filtersRef = useRef({ eventFilter, categoryFilter })
  filtersRef.current = { eventFilter, categoryFilter }

const loadData = useCallback(
    async ({ refreshOnly = false } = {}) => {
      if (refreshOnly) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }

      try {
        const response = await getExpenses({
          ...(filtersRef.current.eventFilter ? { eventId: Number(filtersRef.current.eventFilter) } : {}),
          ...(filtersRef.current.categoryFilter ? { category: filtersRef.current.categoryFilter } : {}),
        })

        setExpenses(Array.isArray(response) ? response : [])
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

  function handleExpenseFormChange(event) {
    const { name, value, type, checked } = event.target
    setExpenseForm((previous) => ({
      ...previous,
      [name]: type === 'checkbox' ? checked : value,
    }))
  }

  async function handleCreateExpense(event) {
    event.preventDefault()
    setExpenseFormError('')

    if (!canCreateExpense) {
      setExpenseFormError('Complete the expense form with a valid event, amount, and approval fields before submitting.')
      return
    }

    const payload = {
      eventId: Number(expenseForm.eventId),
      approvedBy: expenseForm.approvedBy ? Number(expenseForm.approvedBy) : null,
      category: expenseForm.category,
      requiresApproval: expenseForm.requiresApproval,
      approvalRequestedBy: expenseForm.requiresApproval
        ? Number(expenseForm.approvalRequestedBy || expenseForm.approvedBy || 0) || null
        : null,
      amount: toNumericValue(expenseForm.amount),
      description: expenseForm.description.trim() || null,
      expenseDate: expenseForm.expenseDate ? new Date(expenseForm.expenseDate).toISOString() : null,
      paymentStatus: expenseForm.paymentStatus,
    }

    if (!payload.eventId) {
      setExpenseFormError('Event ID is required.')
      return
    }

    if (payload.amount <= 0) {
      setExpenseFormError('Amount must be greater than zero.')
      return
    }

    if (payload.requiresApproval && !payload.approvalRequestedBy) {
      setExpenseFormError('Approval requested by user ID is required when approval is enabled.')
      return
    }

    setIsCreatingExpense(true)

    try {
      await createExpense(payload)
      notify({
        title: 'Expense created',
        message: `Expense for event #${payload.eventId} was recorded.`,
        variant: 'success',
      })
      setExpenseForm(getDefaultExpenseForm())
      await loadData({ refreshOnly: true })
    } catch {
      setExpenseFormError('Unable to create expense. Verify the event, amount, and approval fields.')
    } finally {
      setIsCreatingExpense(false)
    }
  }

  function isExpenseActionBusy(expenseId, paymentStatus) {
    return expenseActionKey === `${expenseId}-${paymentStatus}`
  }

  async function handleUpdatePaymentStatus(row, paymentStatus) {
    setExpenseActionKey(`${row.expenseId}-${paymentStatus}`)

    try {
      await updateExpensePaymentStatus(row.expenseId, { paymentStatus })
      notify({
        title: 'Payment status updated',
        message: `Expense #${row.expenseId} is now ${paymentStatus}.`,
        variant: 'success',
      })
      await loadData({ refreshOnly: true })
    } catch {
      // Global ProblemDetails notifications surface the error.
    } finally {
      setExpenseActionKey('')
    }
  }

  const expenseColumns = useMemo(
    () => [
      {
        key: 'expenseId',
        header: 'Expense',
        render: (row) => <span className="mono-cell">#{row.expenseId}</span>,
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
        key: 'category',
        header: 'Category',
        render: (row) => row.category,
      },
      {
        key: 'amount',
        header: 'Amount',
        render: (row) => row.amount,
      },
      {
        key: 'approvedByName',
        header: 'Approved By',
        render: (row) => row.approvedByName || '-',
      },
      {
        key: 'paymentStatus',
        header: 'Payment Status',
        render: (row) => <StatusBadge label={row.paymentStatus} status={row.paymentStatus} />,
      },
      {
        key: 'expenseDate',
        header: 'Expense Date',
        render: (row) => formatDateTime(row.expenseDate),
      },
      {
        key: 'description',
        header: 'Description',
        render: (row) => row.description || '-',
      },
      {
        key: 'actions',
        header: 'Actions',
        align: 'right',
        render: (row) => (
          <div className="table-actions">
            {EXPENSE_PAYMENT_STATUSES.map((paymentStatus) => (
              <button
                key={paymentStatus}
                type="button"
                className="table-action-btn"
                disabled={isExpenseActionBusy(row.expenseId, paymentStatus) || row.paymentStatus === paymentStatus}
                onClick={() => handleUpdatePaymentStatus(row, paymentStatus)}
              >
                {paymentStatus}
              </button>
            ))}
          </div>
        ),
      },
    ],
    [expenseActionKey],
  )

  if (isLoading) {
    return (
      <LoadingState
        title="Loading expenses"
        message="Preparing expense and payment workflow data from backend services."
      />
    )
  }

  return (
    <div>
<AppCard
        title="Expense Board"
        subtitle="Filter expenses by event or category and update payment state inline."
        actions={
          <div className="toolbar-inline">
            <label htmlFor="eventFilter" className="toolbar-label">
              Event
            </label>
            <input
              id="eventFilter"
              type="number"
              min={1}
              placeholder="Event ID"
              value={eventFilter}
              onChange={(event) => setEventFilter(event.target.value)}
            />

            <label htmlFor="categoryFilter" className="toolbar-label">
              Category
            </label>
            <select
              id="categoryFilter"
              value={categoryFilter}
              onChange={(event) => setCategoryFilter(event.target.value)}
            >
              <option value="">All</option>
              {EXPENSE_CATEGORIES.map((item) => (
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
          caption="Expense listing"
          columns={expenseColumns}
          rows={expenses}
          getRowKey={(row) => row.expenseId}
          emptyMessage="No expenses matched the selected filters."
        />
      </AppCard>

      <AppCard title="Create Expense" subtitle="Record a new expense against a disaster event.">
        <form className="event-create-form" onSubmit={handleCreateExpense} noValidate>
          {expenseFormError ? (
            <AlertBanner variant="warning" title="Expense validation" message={expenseFormError} />
          ) : null}

          <div className="event-form-grid">
            <label>
              Event ID
              <input type="number" min={1} name="eventId" value={expenseForm.eventId} onChange={handleExpenseFormChange} required />
            </label>
            <label>
              Approved By (optional)
              <input type="number" min={1} name="approvedBy" value={expenseForm.approvedBy} onChange={handleExpenseFormChange} />
            </label>
            <label>
              Category
              <select name="category" value={expenseForm.category} onChange={handleExpenseFormChange}>
                {EXPENSE_CATEGORIES.map((item) => (
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
                checked={expenseForm.requiresApproval}
                onChange={handleExpenseFormChange}
              />
              Requires approval
            </label>
            <label>
              Approval Requested By
              <input
                type="number"
                min={1}
                name="approvalRequestedBy"
                value={expenseForm.approvalRequestedBy}
                onChange={handleExpenseFormChange}
                required={expenseForm.requiresApproval}
              />
            </label>
            <label>
              Amount
              <input type="number" min={0.01} step="0.01" name="amount" value={expenseForm.amount} onChange={handleExpenseFormChange} required />
            </label>
            <label>
              Expense Date
              <input type="datetime-local" name="expenseDate" value={expenseForm.expenseDate} onChange={handleExpenseFormChange} />
            </label>
            <label>
              Payment Status
              <select name="paymentStatus" value={expenseForm.paymentStatus} onChange={handleExpenseFormChange}>
                {EXPENSE_PAYMENT_STATUSES.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Description
              <textarea
                name="description"
                value={expenseForm.description}
                onChange={handleExpenseFormChange}
                rows={3}
              />
            </label>
          </div>

          <div className="event-form-actions">
            <button type="submit" className="table-action-btn" disabled={isCreatingExpense || !canCreateExpense}>
              {isCreatingExpense ? 'Creating...' : 'Create Expense'}
            </button>
          </div>
        </form>
      </AppCard>
    </div>
  )
}
