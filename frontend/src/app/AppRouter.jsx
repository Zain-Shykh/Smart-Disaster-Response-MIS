import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from '../components/layout/AppShell'
import { ProtectedRoute } from '../components/routing/ProtectedRoute'
import { useAuth } from '../context/AuthContext'
import { DashboardPage } from '../pages/DashboardPage'
import { DisasterEventsPage } from '../pages/DisasterEventsPage'
import { EmergencyReportsPage } from '../pages/EmergencyReportsPage'
import { HospitalCoordinationPage } from '../pages/HospitalCoordinationPage'
import { IncidentAnalyticsPage } from '../pages/IncidentAnalyticsPage'
import { ExpensesPage } from '../pages/ExpensesPage'
import { LoginPage } from '../pages/LoginPage'
import { PublicReportPage } from '../pages/PublicReportPage'
import { ModulePlaceholderPage } from '../pages/ModulePlaceholderPage'
import { NotFoundPage } from '../pages/NotFoundPage'
import { DonationsPage } from '../pages/DonationsPage'
import { ApprovalWorkflowPage } from '../pages/ApprovalWorkflowPage'
import { ComplianceAuditPage } from '../pages/ComplianceAuditPage'
import { PatientAdmissionsPage } from '../pages/PatientAdmissionsPage'
import { PatientRoutingPage } from '../pages/PatientRoutingPage'
import { ResourceLogisticsPage } from '../pages/ResourceLogisticsPage'
import { RescueTeamsPage } from '../pages/RescueTeamsPage'
import { RbacAdminPage } from '../pages/RbacAdminPage'
import { UsersAdminPage } from '../pages/UsersAdminPage'
import { UnauthorizedPage } from '../pages/UnauthorizedPage'
import { moduleRoutes } from './navigation'

function RouteSkeleton({ module }) {
  const moduleOverrides = {
    'operations/disaster-events': <DisasterEventsPage />,
    'operations/reports': <EmergencyReportsPage />,
    'operations/rescue-teams': <RescueTeamsPage />,
    'logistics/resources': <ResourceLogisticsPage />,
    'logistics/inventory': <ResourceLogisticsPage />,
    'logistics/allocations': <ResourceLogisticsPage />,
    'medical/hospitals': <HospitalCoordinationPage />,
    'medical/admissions': <PatientAdmissionsPage />,
    'medical/routing': <PatientRoutingPage />,
    'finance/donations': <DonationsPage />,
    'finance/expenses': <ExpensesPage />,
    'governance/approvals': <ApprovalWorkflowPage />,
    'admin/users': <UsersAdminPage />,
    'admin/rbac': <RbacAdminPage />,
    'analytics/reports': <IncidentAnalyticsPage />,
    'compliance/audit': <ComplianceAuditPage />,
  }

  return (
    <ProtectedRoute allowedRoles={module.roles}>
      {moduleOverrides[module.path] || (
        <ModulePlaceholderPage
          title={module.label}
          description={module.description}
          stepId={module.phaseStep}
        />
      )}
    </ProtectedRoute>
  )
}

export function AppRouter() {
  const { isInitializing, isAuthenticated } = useAuth()

  if (isInitializing) {
    return (
      <div className="screen-loader">
        <div className="screen-loader-card">Restoring secure session...</div>
      </div>
    )
  }

  return (
    <BrowserRouter>
      <Routes>
        <Route
          path="/login"
          element={isAuthenticated ? <Navigate to="/" replace /> : <LoginPage />}
        />
        <Route path="/public-report" element={<PublicReportPage />} />
        <Route path="/unauthorized" element={<UnauthorizedPage />} />

        <Route element={<ProtectedRoute />}>
          <Route element={<AppShell />}>
            <Route index element={<DashboardPage />} />
            {moduleRoutes.map((module) => (
              <Route
                key={module.path}
                path={module.path}
                element={<RouteSkeleton module={module} />}
              />
            ))}
          </Route>
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  )
}
