const ROLE_ADMIN = 'Administrator'
const ROLE_OPERATOR = 'EmergencyOperator'
const ROLE_FIELD = 'FieldOfficer'
const ROLE_WAREHOUSE = 'WarehouseManager'
const ROLE_FINANCE = 'FinanceOfficer'

export const allRoles = [
  ROLE_ADMIN,
  ROLE_OPERATOR,
  ROLE_FIELD,
  ROLE_WAREHOUSE,
  ROLE_FINANCE,
]

export const moduleRoutes = [
  {
    path: 'operations/disaster-events',
    label: 'Disaster Events',
    description: 'Create, update, and monitor disaster event lifecycle data.',
    roles: [ROLE_ADMIN, ROLE_OPERATOR, ROLE_FIELD],
    phaseStep: 'FE-030',
  },
  {
    path: 'operations/reports',
    label: 'Emergency Reports',
    description: 'Capture emergency incidents and manage response status flow.',
    roles: [ROLE_ADMIN, ROLE_OPERATOR, ROLE_FIELD],
    phaseStep: 'FE-031',
  },
  {
    path: 'operations/rescue-teams',
    label: 'Rescue Coordination',
    description: 'Manage rescue teams, recommendations, assignments, and activity logs.',
    roles: [ROLE_ADMIN, ROLE_OPERATOR, ROLE_FIELD],
    phaseStep: 'FE-034',
  },
  {
    path: 'logistics/resources',
    label: 'Resources and Warehouses',
    description: 'Maintain resource and warehouse records for active disaster events.',
    roles: [ROLE_ADMIN, ROLE_WAREHOUSE],
    phaseStep: 'FE-040',
  },
  {
    path: 'logistics/inventory',
    label: 'Inventory and Alerts',
    description: 'Track stock levels and trigger low-stock operational alerts.',
    roles: [ROLE_ADMIN, ROLE_WAREHOUSE],
    phaseStep: 'FE-041',
  },
  {
    path: 'logistics/allocations',
    label: 'Resource Allocations',
    description: 'Create and approve dispatch requests with inventory safeguards.',
    roles: [ROLE_ADMIN, ROLE_WAREHOUSE, ROLE_FIELD],
    phaseStep: 'FE-043',
  },
  {
    path: 'medical/hospitals',
    label: 'Hospital Coordination',
    description: 'Manage hospital readiness, beds, and specialization routing.',
    roles: [ROLE_ADMIN, ROLE_OPERATOR, ROLE_FIELD],
    phaseStep: 'FE-050',
  },
  {
    path: 'medical/admissions',
    label: 'Patient Admissions',
    description: 'Handle patient intake, admissions, and auto-routing escalations.',
    roles: [ROLE_ADMIN, ROLE_OPERATOR, ROLE_FIELD],
    phaseStep: 'FE-051',
  },
  {
    path: 'medical/routing',
    label: 'Patient Routing',
    description: 'Manually route patients to hospitals or use escalation-aware auto routing.',
    roles: [ROLE_ADMIN, ROLE_OPERATOR, ROLE_FIELD],
    phaseStep: 'FE-052',
  },
  {
    path: 'finance/donations',
    label: 'Donations',
    description: 'Track donors, donations, and donation confirmation state.',
    roles: [ROLE_ADMIN, ROLE_FINANCE],
    phaseStep: 'FE-053',
  },
  {
    path: 'finance/expenses',
    label: 'Expenses',
    description: 'Capture expenses, payment state, and linked approval requests.',
    roles: [ROLE_ADMIN, ROLE_FINANCE],
    phaseStep: 'FE-054',
  },
  {
    path: 'governance/approvals',
    label: 'Approval Workflow',
    description: 'Review pending requests and maintain complete decision history.',
    roles: [ROLE_ADMIN, ROLE_WAREHOUSE, ROLE_FINANCE],
    phaseStep: 'FE-055',
  },
  {
    path: 'admin/users',
    label: 'User Administration',
    description: 'Manage user identities, status, and profile operations.',
    roles: [ROLE_ADMIN],
    phaseStep: 'FE-060',
  },
  {
    path: 'admin/rbac',
    label: 'RBAC Administration',
    description: 'Maintain roles, permissions, and role mapping assignments.',
    roles: [ROLE_ADMIN],
    phaseStep: 'FE-062',
  },
  {
    path: 'analytics/reports',
    label: 'MIS Analytics',
    description: 'Display incident, logistics, finance, and approval dashboards.',
    roles: allRoles,
    phaseStep: 'FE-070',
  },
  {
    path: 'compliance/audit',
    label: 'Audit Monitoring',
    description: 'Search and review system activity trails and compliance signals.',
    roles: [ROLE_ADMIN, ROLE_FINANCE],
    phaseStep: 'FE-064',
  },
]

export const navigationSections = [
  {
    title: 'Core',
    items: [{ label: 'Dashboard', path: '/', roles: allRoles, tag: 'Live' }],
  },
  {
    title: 'Operations',
    items: moduleRoutes.filter((item) => item.path.startsWith('operations/')),
  },
  {
    title: 'Logistics',
    items: moduleRoutes.filter((item) => item.path.startsWith('logistics/')),
  },
  {
    title: 'Medical',
    items: moduleRoutes.filter((item) => item.path.startsWith('medical/')),
  },
  {
    title: 'Finance and Governance',
    items: moduleRoutes.filter(
      (item) => item.path.startsWith('finance/') || item.path.startsWith('governance/'),
    ),
  },
  {
    title: 'Administration and Intelligence',
    items: moduleRoutes.filter(
      (item) =>
        item.path.startsWith('admin/') ||
        item.path.startsWith('analytics/') ||
        item.path.startsWith('compliance/'),
    ),
  },
]
