import { useEffect, useState } from 'react'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  AreaChart,
  Area,
  PieChart,
  Pie,
  Cell,
} from 'recharts'
import { getIncidentsBySeverity, getIncidentTrend, getFinancialSummary } from '../../services/api/reportsAnalyticsApi'
import { getDonationsSummary, getExpensesSummary } from '../../services/api/donationFinanceApi'
import { AppCard } from '../ui/AppCard'
import { LoadingState } from '../ui/LoadingState'
import { AlertCircle, TrendingUp, DollarSign, ShieldAlert } from 'lucide-react'

export function DashboardCharts() {
  const [severityData, setSeverityData] = useState([])
  const [trendData, setTrendData] = useState([])
  const [financialData, setFinancialData] = useState([])
  const [expenseBreakdown, setExpenseBreakdown] = useState([])
  const [donationBreakdown, setDonationBreakdown] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    async function fetchData() {
      try {
        const results = await Promise.allSettled([
          getIncidentsBySeverity(),
          getIncidentTrend(),
          getFinancialSummary(),
          getDonationsSummary(),
          getExpensesSummary(),
        ])

        const [
          severityResult,
          trendResult,
          financialResult,
          donationsSummaryResult,
          expensesSummaryResult,
        ] = results

        if (!results.some((item) => item.status === 'fulfilled')) {
          throw new Error('All dashboard data requests failed.')
        }

        const severity = severityResult.status === 'fulfilled' ? severityResult.value : []
        const trend = trendResult.status === 'fulfilled' ? trendResult.value : []
        const financial = financialResult.status === 'fulfilled' ? financialResult.value : {}
        const donationsSummary = donationsSummaryResult.status === 'fulfilled' ? donationsSummaryResult.value : []
        const expensesSummary = expensesSummaryResult.status === 'fulfilled' ? expensesSummaryResult.value : []

        setSeverityData(Array.isArray(severity) ? severity.map(item => ({
          severityLevel: item.severityLevel || item.SeverityLevel,
          count: item.count || item.Count
        })) : [])

        const rawTrend = Array.isArray(trend) ? trend : []
        const sortedTrend = rawTrend
          .slice()
          .map(item => ({
            ...item,
            // Handle both camelCase and PascalCase from API
            dateValue: new Date(item.date || item.Date || item.reportTime || item.ReportTime),
            count: Number(item.count || item.Count) || 0
          }))
          .filter(item => !isNaN(item.dateValue.getTime()))
          .sort((a, b) => a.dateValue.getTime() - b.dateValue.getTime())

        const peakCount = Math.max(...sortedTrend.map((item) => item.count), 0)
        setTrendData(
          sortedTrend.map((item) => {
            const intensity = peakCount > 0 ? item.count / peakCount : 0

            let color = '#39ff14'
            if (intensity >= 0.8) color = '#ff0000'
            else if (intensity >= 0.6) color = '#ff4d4d'
            else if (intensity >= 0.4) color = '#f0a500'

            return {
              ...item,
              color,
              displayDate: item.dateValue.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }),
            }
          }),
        )

        const donationAmountFromFinancialSummary = Array.isArray(financial?.donationSummary)
          ? financial.donationSummary.reduce((sum, item) => sum + (Number(item?.amount) || 0), 0)
          : 0

        const expenseAmountFromFinancialSummary = Array.isArray(financial?.expenseSummary)
          ? financial.expenseSummary.reduce((sum, item) => sum + (Number(item?.amount) || 0), 0)
          : 0

        const donationAmountFromView = Array.isArray(donationsSummary)
          ? donationsSummary.reduce((sum, item) => sum + (Number(item?.amount) || 0), 0)
          : 0

        const expenseAmountFromView = Array.isArray(expensesSummary)
          ? expensesSummary.reduce((sum, item) => sum + (Number(item?.amount) || 0), 0)
          : 0

        const donationAmount = donationAmountFromFinancialSummary
          || Number(financial?.confirmedDonationAmount)
          || donationAmountFromView
          || 0

        const expenseAmount = expenseAmountFromFinancialSummary
          || Number(financial?.totalExpenseAmount)
          || expenseAmountFromView
          || 0

        // Donation = Lime, Expense = Amber
        setFinancialData([
          { name: 'Donations (PKR)', amount: donationAmount, color: '#39ff14' },
          { name: 'Expenses (PKR)', amount: expenseAmount, color: '#f0a500' },
        ])

        // Process breakdowns for pie charts
        const DONATION_COLORS = ['#39ff14', '#00c8ff', '#58a6ff', '#7fff5e']
        const EXPENSE_COLORS = ['#f0a500', '#ffcc00', '#ff4d4d', '#ff0000']

        if (Array.isArray(financial?.donationSummary)) {
          setDonationBreakdown(financial.donationSummary.map((item, idx) => ({
            name: item.status,
            value: Number(item.amount) || 0,
            color: DONATION_COLORS[idx % DONATION_COLORS.length]
          })))
        }

        if (Array.isArray(financial?.expenseSummary)) {
          setExpenseBreakdown(financial.expenseSummary.map((item, idx) => ({
            name: item.category,
            value: Number(item.amount) || 0,
            color: EXPENSE_COLORS[idx % EXPENSE_COLORS.length]
          })))
        }

        setError(null)
      } catch (err) {
        console.error('Failed to fetch dashboard charts data:', err)
        setError('Failed to load real-time analytics. Please try again later.')
      } finally {
        setIsLoading(false)
      }
    }

    fetchData()
    const interval = setInterval(fetchData, 30000)
    return () => clearInterval(interval)
  }, [])

  if (isLoading) {
    return <div className="p-4"><LoadingState title="Loading Visualizations" message="Aggregating incident and financial data..." /></div>
  }

  if (error) {
    return (
      <div className="p-4">
        <div className="alert-banner alert-banner-danger">
          <AlertCircle size={20} />
          <div>
            <strong>Error</strong>
            <p>{error}</p>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="dashboard-charts-container">
      <div className="dashboard-grid-two">
        <AppCard 
          title="Incident Severity Distribution" 
          subtitle="Breakdown of emergency reports by their assigned severity levels."
          icon={<ShieldAlert size={20} className="text-danger" />}
        >
          <div style={{ width: '100%', height: 300 }}>
            <ResponsiveContainer>
              <BarChart data={severityData}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="rgba(56,68,84,0.4)" />
                <XAxis dataKey="severityLevel" stroke="#6e7681" tick={{ fill: '#8b949e', fontFamily: 'JetBrains Mono', fontSize: 11 }} />
                <YAxis stroke="#6e7681" tick={{ fill: '#8b949e', fontFamily: 'JetBrains Mono', fontSize: 11 }} />
                <Tooltip contentStyle={{ background: '#161b22', border: '1px solid rgba(0,200,255,0.2)', borderRadius: 4, color: '#e6edf3', fontFamily: 'JetBrains Mono', fontSize: 12 }} />
                <Legend wrapperStyle={{ fontFamily: 'JetBrains Mono', fontSize: 11, color: '#8b949e' }} />
                <Bar dataKey="count" name="Incidents" fill="#ff4d4d" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </AppCard>

        <AppCard 
          title="Incident Volume Trend" 
          subtitle="Daily volume of reported incidents showing wave patterns."
          icon={<TrendingUp size={20} className="text-primary" />}
        >
          <div style={{ width: '100%', height: 300 }}>
            <ResponsiveContainer>
              <AreaChart data={trendData}>
                <defs>
                  <linearGradient id="incidentBellFill" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#ff0000" stopOpacity={0.35} />
                    <stop offset="35%" stopColor="#f0a500" stopOpacity={0.25} />
                    <stop offset="65%" stopColor="#00c8ff" stopOpacity={0.15} />
                    <stop offset="95%" stopColor="#39ff14" stopOpacity={0.05} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="rgba(56,68,84,0.4)" />
                <XAxis dataKey="displayDate" stroke="#6e7681" tick={{ fill: '#8b949e', fontFamily: 'JetBrains Mono', fontSize: 11 }} />
                <YAxis stroke="#6e7681" tick={{ fill: '#8b949e', fontFamily: 'JetBrains Mono', fontSize: 11 }} />
                <Tooltip contentStyle={{ background: '#161b22', border: '1px solid rgba(0,200,255,0.2)', borderRadius: 4, color: '#e6edf3', fontFamily: 'JetBrains Mono', fontSize: 12 }} />
                <Legend wrapperStyle={{ fontFamily: 'JetBrains Mono', fontSize: 11, color: '#8b949e' }} />
                <Area 
                  type="natural"
                  dataKey="count" 
                  name="Reports" 
                  stroke="#00c8ff"
                  fillOpacity={1} 
                  fill="url(#incidentBellFill)"
                  strokeWidth={3}
                  dot={({ cx, cy, payload }) => (
                    <circle cx={cx} cy={cy} r={3} stroke="#0a0c10" strokeWidth={1} fill={payload?.color || '#00c8ff'} />
                  )}
                />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </AppCard>
      </div>

      <div style={{ marginTop: '1.5rem' }}>
        <AppCard 
          title="Financial Distribution: Categories & Status" 
          subtitle="Breakdown of funding sources and spending allocation."
          icon={<DollarSign size={20} className="text-success" />}
        >
          <div className="dashboard-grid-two">
            <div>
              <h4 style={{ textAlign: 'center', marginBottom: '.5rem', color: '#39ff14', fontFamily: 'JetBrains Mono', fontSize: '.78rem', letterSpacing: '1px' }}>DONATIONS BY STATUS (PKR)</h4>
              <div style={{ width: '100%', height: 250 }}>
                <ResponsiveContainer>
                  <PieChart>
                    <Pie
                      data={donationBreakdown.length > 0 ? donationBreakdown : [{ name: 'No Data', value: 1, color: '#1c2333' }]}
                      cx="50%"
                      cy="50%"
                      innerRadius={50}
                      outerRadius={70}
                      paddingAngle={5}
                      dataKey="value"
                    >
                      {donationBreakdown.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={entry.color} />
                      ))}
                      {donationBreakdown.length === 0 && <Cell fill="#1c2333" />}
                    </Pie>
                    <Tooltip formatter={(value) => `PKR ${Number(value).toLocaleString()}`} />
                    <Legend />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            </div>
            
            <div>
              <h4 style={{ textAlign: 'center', marginBottom: '.5rem', color: '#f0a500', fontFamily: 'JetBrains Mono', fontSize: '.78rem', letterSpacing: '1px' }}>EXPENSES BY CATEGORY (PKR)</h4>
              <div style={{ width: '100%', height: 250 }}>
                <ResponsiveContainer>
                  <PieChart>
                    <Pie
                      data={expenseBreakdown.length > 0 ? expenseBreakdown : [{ name: 'No Data', value: 1, color: '#1c2333' }]}
                      cx="50%"
                      cy="50%"
                      innerRadius={50}
                      outerRadius={70}
                      paddingAngle={5}
                      dataKey="value"
                    >
                      {expenseBreakdown.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={entry.color} />
                      ))}
                      {expenseBreakdown.length === 0 && <Cell fill="#1c2333" />}
                    </Pie>
                    <Tooltip formatter={(value) => `PKR ${Number(value).toLocaleString()}`} />
                    <Legend />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            </div>
          </div>
          
          <div style={{ marginTop: '2rem', borderTop: '1px solid rgba(56,68,84,0.4)', paddingTop: '1.5rem' }}>
            <h4 style={{ textAlign: 'center', marginBottom: '.8rem', color: '#e6edf3', fontFamily: 'JetBrains Mono', fontSize: '.78rem', letterSpacing: '1px' }}>OVERALL FINANCIAL COMPARISON (PKR)</h4>
            <div style={{ width: '100%', height: 200 }}>
              <ResponsiveContainer>
                <BarChart data={financialData} layout="vertical">
                  <CartesianGrid strokeDasharray="3 3" horizontal={false} stroke="rgba(56,68,84,0.4)" />
                  <XAxis type="number" stroke="#6e7681" tick={{ fill: '#8b949e', fontFamily: 'JetBrains Mono', fontSize: 11 }} />
                  <YAxis dataKey="name" type="category" stroke="#6e7681" tick={{ fill: '#8b949e', fontFamily: 'JetBrains Mono', fontSize: 11 }} />
                  <Tooltip formatter={(value) => `PKR ${Number(value).toLocaleString()}`} contentStyle={{ background: '#161b22', border: '1px solid rgba(0,200,255,0.2)', borderRadius: 4, color: '#e6edf3', fontFamily: 'JetBrains Mono', fontSize: 12 }} />
                  <Bar dataKey="amount" name="PKR Amount" fill="#39ff14" radius={[0, 4, 4, 0]}>
                    {financialData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>
        </AppCard>
      </div>
    </div>
  )
}
