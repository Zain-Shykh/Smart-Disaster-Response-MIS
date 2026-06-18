import { Link } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { moduleRoutes } from '../app/navigation'
import { useAuth } from '../context/AuthContext'
import { AlertBanner } from '../components/ui/AlertBanner'
import { AppCard } from '../components/ui/AppCard'
import { LoadingState } from '../components/ui/LoadingState'
import { StatusBadge } from '../components/ui/StatusBadge'

import { DashboardCharts } from '../components/dashboard/DashboardCharts'

/* ── Pakistani Ticker Alerts ── */
const TICKER_ALERTS = [
  '⚠ URGENT: Water levels rising at Sukkur Barrage — Sindh PDMA on high alert',
  '🔴 1122 Team Alpha dispatched to Sector G-7 Islamabad — Structure fire reported',
  '🌊 GLOF warning issued for Hunza Valley, Gilgit-Baltistan — Evacuations underway',
  '🚑 Edhi Ambulance Fleet mobilized to Lyari, Karachi — Mass casualty incident',
  '⚠ Monsoon Flash Flood alert: Nowshera & Charsadda districts — Indus river gauge critical',
  '🔴 Earthquake aftershock M4.2 detected near Balakot — Rescue 1122 KPK responding',
  '🌡️ Heatwave Emergency: Jacobabad 52°C — Anti-heatstroke camps activated',
  '📦 PM Relief Fund: PKR 2.5M dispatched to Muzaffarabad relief operations',
  '🚁 Army Engineering Corps deploying de-watering pumps to Rajanpur, Punjab',
  '⚠ Anti-snake venom stock critical at DHQ Hospital Dera Ismail Khan',
  '🔴 Pakistan Red Crescent: 500 winter tents dispatched to Quetta earthquake zone',
  '🌊 Indus River at Guddu Barrage: 450,000 cusecs — DANGER level exceeded',
  '📡 NDMA Situation Room: 14 active disaster events across 4 provinces',
  '🚑 1122 Water Rescue unit deployed to Swat River — 3 villages cut off',
  '⚠ Margalla Hills wildfire spreading — ICT Administration coordinating response',
]

const ROLE_PROFILES = {
  administrator: {
    label: 'Administrator',
    mission: 'Oversee end-to-end national disaster response integrity and NDMA governance controls.',
    focus: 'NDMA Command View',
    priorityPaths: ['admin/users', 'admin/rbac', 'governance/approvals', 'compliance/audit', 'analytics/reports'],
    showAnalytics: true,
  },
  emergencyoperator: {
    label: 'Emergency Operator',
    mission: 'Coordinate incident response across provincial PDMAs and maintain decision momentum.',
    focus: 'Provincial Operations Center',
    priorityPaths: ['operations/reports', 'operations/rescue-teams', 'medical/routing', 'governance/approvals', 'analytics/reports'],
    showAnalytics: true,
  },
  fieldofficer: {
    label: 'Field Officer',
    mission: 'Drive rapid field interventions with Rescue 1122, Edhi Foundation, and Army Corps.',
    focus: 'Field Deployment Readiness',
    priorityPaths: ['operations/disaster-events', 'operations/rescue-teams', 'logistics/allocations', 'medical/admissions', 'medical/routing'],
    showAnalytics: false,
  },
  warehousemanager: {
    label: 'Warehouse Manager',
    mission: 'Maintain ration pack, tent, and medical supply confidence across NDMA warehouses.',
    focus: 'Supply Chain Board',
    priorityPaths: ['logistics/resources', 'logistics/inventory', 'logistics/allocations', 'governance/approvals'],
    showAnalytics: false,
  },
  financeofficer: {
    label: 'Finance Officer',
    mission: 'Protect PKR financial traceability for PM Relief Fund and international aid.',
    focus: 'Financial Audit Desk',
    priorityPaths: ['finance/donations', 'finance/expenses', 'governance/approvals', 'compliance/audit'],
    showAnalytics: true,
  },
}

function normalizeRole(role) {
  return String(role || '').toLowerCase().replace(/\s+/g, '')
}

function hasAccess(userRoles, requiredRoles) {
  const userRoleSet = new Set(userRoles.map(normalizeRole))
  return requiredRoles.some((role) => userRoleSet.has(normalizeRole(role)))
}

function getStepNumber(phaseStep) {
  const raw = String(phaseStep || '').replace('FE-', '')
  const value = Number(raw)
  return Number.isFinite(value) ? value : 999
}

/* ── Ticker Component ── */
function CrisisTicker() {
  const [offset, setOffset] = useState(0)
  useEffect(() => {
    const id = setInterval(() => setOffset((o) => o - 1), 30)
    return () => clearInterval(id)
  }, [])

  const joined = TICKER_ALERTS.join('     ///     ')
  return (
    <div className="crisis-ticker" aria-label="Live crisis feed">
      <span className="ticker-label">LIVE FEED</span>
      <div className="ticker-track">
        <span className="ticker-content" style={{ transform: `translateX(${offset}px)` }}>
          {joined}     ///     {joined}
        </span>
      </div>
    </div>
  )
}

/* ── City coordinate lookup (% within the map grid) ── */
const CITY_COORDS = {
  islamabad:      { x: 62, y: 24 },
  rawalpindi:     { x: 61, y: 25 },
  lahore:         { x: 66, y: 35 },
  karachi:        { x: 40, y: 78 },
  peshawar:       { x: 55, y: 20 },
  quetta:         { x: 28, y: 48 },
  muzaffarabad:   { x: 65, y: 18 },
  nowshera:       { x: 58, y: 22 },
  charsadda:      { x: 56, y: 19 },
  balakot:        { x: 63, y: 16 },
  jacobabad:      { x: 44, y: 55 },
  sukkur:         { x: 44, y: 60 },
  rajanpur:       { x: 50, y: 48 },
  'dera ismail khan': { x: 52, y: 32 },
  gilgit:         { x: 58, y: 8 },
  hunza:          { x: 60, y: 5 },
  swat:           { x: 54, y: 15 },
  multan:         { x: 56, y: 42 },
  faisalabad:     { x: 60, y: 36 },
  hyderabad:      { x: 44, y: 70 },
  bahawalpur:     { x: 56, y: 50 },
  abbottabad:     { x: 62, y: 20 },
  mardan:         { x: 56, y: 20 },
  gwadar:         { x: 15, y: 68 },
  turbat:         { x: 18, y: 62 },
  zhob:           { x: 35, y: 38 },
  sibi:           { x: 36, y: 50 },
  larkana:        { x: 42, y: 58 },
  mirpur:         { x: 66, y: 22 },
  mansehra:       { x: 62, y: 18 },
}

function resolveCoords(city) {
  if (!city) return null
  const key = city.toLowerCase().trim()
  if (CITY_COORDS[key]) return CITY_COORDS[key]
  // Partial match
  for (const [k, v] of Object.entries(CITY_COORDS)) {
    if (key.includes(k) || k.includes(key)) return v
  }
  return null
}

function severityClass(sev) {
  const s = String(sev || '').toLowerCase()
  if (s === 'critical') return 'critical'
  if (s === 'high') return 'high'
  return 'medium'
}

/* ── Pakistan Map Overlay (API-driven) ── */
function PakistanMapOverlay() {
  const [pins, setPins] = useState([])

  useEffect(() => {
    let cancelled = false
    async function fetchReports() {
      try {
        const { getEmergencyReports } = await import('../services/api/emergencyReportsApi')
        const reports = await getEmergencyReports()
        if (cancelled) return
        const mapped = (Array.isArray(reports) ? reports : [])
          .map((r) => {
            const coords = resolveCoords(r.city)
            if (!coords) return null
            return {
              id: r.reportId,
              name: r.city,
              type: r.disasterType || 'Unknown',
              severity: r.severityLevel || 'Medium',
              status: r.status,
              x: coords.x,
              y: coords.y,
            }
          })
          .filter(Boolean)
        setPins(mapped.length > 0 ? mapped : [])
      } catch {
        setPins([])
      }
    }
    fetchReports()
    const interval = setInterval(fetchReports, 30000)
    return () => { cancelled = true; clearInterval(interval) }
  }, [])

  /* Pakistan simplified SVG outline */
  const pakistanPath = "M 58 2 L 65 5 L 68 12 L 72 18 L 70 22 L 72 26 L 68 30 L 70 36 L 68 42 L 62 48 L 58 55 L 52 62 L 48 70 L 44 78 L 38 82 L 30 78 L 25 70 L 20 65 L 15 68 L 10 60 L 14 52 L 18 44 L 22 36 L 28 28 L 35 22 L 42 18 L 48 12 L 52 8 Z"

  return (
    <div className="pak-map-overlay">
      <div className="pak-map-header">
        <span className="pak-map-title">GLOBAL WATCH — PAKISTAN THEATER</span>
        <span className="pak-map-live-dot" />
        <span className="pak-map-live-text">LIVE TELEMETRY</span>
        <span style={{ marginLeft: 'auto', fontFamily: 'var(--font-mono)', fontSize: '.65rem', color: 'var(--ink-400)' }}>
          {pins.length} ACTIVE INCIDENT{pins.length !== 1 ? 'S' : ''}
        </span>
      </div>
      <div className="pak-map-grid">
        {/* SVG Pakistan outline */}
        <svg viewBox="0 0 82 85" style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', opacity: 0.2 }}>
          <path d={pakistanPath} fill="none" stroke="var(--blue-500)" strokeWidth="0.5" />
          {/* Province dividers */}
          <line x1="42" y1="18" x2="48" y2="55" stroke="var(--line-200)" strokeWidth="0.3" strokeDasharray="1,1" />
          <line x1="28" y1="28" x2="62" y2="48" stroke="var(--line-200)" strokeWidth="0.3" strokeDasharray="1,1" />
          <line x1="52" y1="8" x2="70" y2="22" stroke="var(--line-200)" strokeWidth="0.3" strokeDasharray="1,1" />
        </svg>

        {/* Province labels */}
        <span className="province-label" style={{ left: '18%', top: '42%' }}>BALOCHISTAN</span>
        <span className="province-label" style={{ left: '50%', top: '36%' }}>PUNJAB</span>
        <span className="province-label" style={{ left: '38%', top: '68%' }}>SINDH</span>
        <span className="province-label" style={{ left: '50%', top: '17%' }}>KPK</span>
        <span className="province-label" style={{ left: '55%', top: '5%' }}>GB</span>
        <span className="province-label" style={{ left: '67%', top: '20%' }}>AJK</span>
        <span className="province-label" style={{ left: '60%', top: '23%' }}>ICT</span>

        {pins.map((pin) => (
          <div
            key={`${pin.id}-${pin.name}`}
            className={`map-pin ${severityClass(pin.severity)}`}
            style={{ left: `${pin.x}%`, top: `${pin.y}%` }}
            title={`#${pin.id} ${pin.name}: ${pin.type} — ${pin.severity} [${pin.status}]`}
          >
            <span className="map-pin-pulse" />
            <span className="map-pin-label">{pin.name}</span>
          </div>
        ))}

        {pins.length === 0 && (
          <div style={{ position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <span style={{ fontFamily: 'var(--font-mono)', fontSize: '.75rem', color: 'var(--ink-400)', letterSpacing: '1px' }}>
              NO ACTIVE INCIDENTS — STANDBY MODE
            </span>
          </div>
        )}
      </div>
      <div className="pak-map-legend">
        <span className="legend-item critical">● Critical / Shadeed</span>
        <span className="legend-item high">● High / Zyada</span>
        <span className="legend-item medium">● Medium / Darmiyana</span>
      </div>
    </div>
  )
}

export function DashboardPage() {
  const { user, roles } = useAuth()

  if (!user) {
    return (
      <LoadingState
        title="Initializing command center"
        message="Fetching role context and NDMA workflow access..."
      />
    )
  }

  const visibleModules = moduleRoutes.filter((item) => hasAccess(roles, item.roles))
  const normalizedRoles = roles.map(normalizeRole)
  const primaryRoleKey = normalizedRoles.find((role) => ROLE_PROFILES[role]) || 'administrator'
  const roleProfile = ROLE_PROFILES[primaryRoleKey]

  const priorityPathSet = new Set(roleProfile.priorityPaths)
  const prioritizedModules = visibleModules.filter((item) => priorityPathSet.has(item.path))
  const secondaryModules = visibleModules.filter((item) => !priorityPathSet.has(item.path))
  const quickActions = [...prioritizedModules, ...secondaryModules].slice(0, 6)

  const liveModules = visibleModules.filter((item) => getStepNumber(item.phaseStep) < 80)
  const inProgressModules = visibleModules.filter((item) => {
    const stepNumber = getStepNumber(item.phaseStep)
    return stepNumber >= 80 && stepNumber < 90
  })

  return (
    <div>
      <AlertBanner
        variant="info"
        title={`${roleProfile.label} — ${roleProfile.focus}`}
        message={roleProfile.mission}
      />

      <div className="role-pills">
        {roles.map((role) => (
          <span className="role-pill" key={role}>{role}</span>
        ))}
      </div>

      <div className="kpi-grid">
        <article className="kpi-card">
          <span>Signed in as</span>
          <strong>{user?.fullName || 'System User'}</strong>
        </article>
        <article className="kpi-card">
          <span>{roleProfile.focus}</span>
          <strong>{visibleModules.length}</strong>
        </article>
        <article className="kpi-card">
          <span>Live Modules</span>
          <strong>
            <StatusBadge label={`${liveModules.length} active`} status="success" />
          </strong>
        </article>
        <article className="kpi-card">
          <span>Next-Wave Modules</span>
          <strong>
            <StatusBadge label={`${inProgressModules.length} upcoming`} status="planned" />
          </strong>
        </article>
      </div>

      {/* Pakistan Map Overlay */}
      <PakistanMapOverlay />

      {roleProfile.showAnalytics && (
        <div style={{ marginBottom: '1.5rem' }}>
          <DashboardCharts />
        </div>
      )}

      <AppCard
        title="Quick Actions"
        subtitle="Jump directly into role-priority workflows — Rescue 1122, PDMA, Edhi coordination desks."
      >
        <div className="action-grid">
          {quickActions.map((module) => (
            <Link key={module.path} to={`/${module.path}`} className="action-card">
              <h3>{module.label}</h3>
              <p>{module.description}</p>
            </Link>
          ))}
        </div>
      </AppCard>

      {/* Crisis Ticker at bottom */}
      <CrisisTicker />
    </div>
  )
}
