import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { DataTable } from '../components/ui/DataTable'
import { StatusBadge } from '../components/ui/StatusBadge'

export function ModulePlaceholderPage({ title, description, stepId }) {
  const checklistColumns = [
    { key: 'item', header: 'Implementation Item' },
    {
      key: 'status',
      header: 'Status',
      align: 'right',
      render: (row) => <StatusBadge label={row.status} status="planned" />,
    },
  ]

  const checklistRows = [
    { id: 'forms', item: 'Build forms and DTO-compatible validation rules', status: 'Pending' },
    { id: 'api', item: 'Integrate API calls and error state handling', status: 'Pending' },
    { id: 'ux', item: 'Add loading, empty, and success feedback states', status: 'Pending' },
  ]

  return (
    <div className="placeholder-page">
      <h2>{title}</h2>
      <p>{description}</p>

      <AlertBanner
        variant="warning"
        title="Module under construction"
        message={`This section is tracked under implementation step ${stepId}.`}
      />

      <AppCard title="Build Checklist" subtitle="Standard delivery checklist for each module page.">
        <DataTable columns={checklistColumns} rows={checklistRows} />
      </AppCard>

      <span className="status-chip">
        Planned in frontend step <strong>{stepId}</strong>
      </span>
    </div>
  )
}
