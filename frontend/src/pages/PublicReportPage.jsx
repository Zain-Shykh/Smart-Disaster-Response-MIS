import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { submitPublicReport } from '../services/api/publicReportApi'

export function PublicReportPage() {
  const navigate = useNavigate()
  
  const [formState, setFormState] = useState({
    nationalId: '',
    firstName: '',
    lastName: '',
    street: '',
    area: '',
    city: '',
    province: '',
    disasterType: 'Monsoon Flash Flood',
    severityLevel: 'Medium',
    description: '',
  })
  
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  async function handleSubmit(event) {
    event.preventDefault()
    setErrorMessage('')
    setSuccessMessage('')
    setIsSubmitting(true)

    try {
      await submitPublicReport(formState)
      navigate('/login')
    } catch (error) {
      const errData = error?.response?.data
      if (typeof errData === 'string') {
        setErrorMessage(errData)
      } else if (errData && errData.title) {
        setErrorMessage(errData.title)
      } else if (errData && errData.errors) {
        const firstError = Object.values(errData.errors)[0]
        setErrorMessage(Array.isArray(firstError) ? firstError[0] : 'Validation failed.')
      } else {
        setErrorMessage('Failed to submit report. Please try again.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  function handleInputChange(event) {
    const { name, value } = event.target
    setFormState((prev) => ({ ...prev, [name]: value }))
  }

  return (
    <div className="auth-screen">
      <section className="auth-card-wrap" style={{ width: '100%', maxWidth: '600px', margin: '0 auto' }}>
        <form className="auth-card" onSubmit={handleSubmit} noValidate>
          <h2>Report A Disaster</h2>
          <p className="auth-subtitle">Civilian emergency reporting helpline.</p>

          {errorMessage ? <div className="auth-error">{errorMessage}</div> : null}
          {successMessage ? <div style={{ color: 'var(--lime-400)', marginBottom: '1rem', padding: '1rem', backgroundColor: 'rgba(57,255,20,0.06)', border: '1px solid rgba(57,255,20,0.25)', borderRadius: '4px', fontFamily: 'var(--font-mono)' }}>{successMessage}</div> : null}

          <div className="event-form-grid">
            <label>
              National ID
              <input name="nationalId" required value={formState.nationalId} onChange={handleInputChange} />
            </label>
            <label>
              First Name
              <input name="firstName" required value={formState.firstName} onChange={handleInputChange} />
            </label>
            <label>
              Last Name
              <input name="lastName" required value={formState.lastName} onChange={handleInputChange} />
            </label>
            <label>
              Street
              <input name="street" required value={formState.street} onChange={handleInputChange} />
            </label>
            <label>
              Area
              <input name="area" required value={formState.area} onChange={handleInputChange} />
            </label>
            <label>
              City
              <input name="city" required value={formState.city} onChange={handleInputChange} />
            </label>
            <label>
              Province
              <input name="province" required value={formState.province} onChange={handleInputChange} />
            </label>
            <label>
              Disaster Type
              <select name="disasterType" value={formState.disasterType} onChange={handleInputChange}>
                <option value="Monsoon Flash Flood">Monsoon Flash Flood</option>
                <option value="River Flood">River Flood</option>
                <option value="GLOF (Glacial Lake Outburst)">GLOF (Glacial Lake Outburst)</option>
                <option value="Earthquake">Earthquake</option>
                <option value="Heatwave Emergency">Heatwave Emergency</option>
                <option value="Urban Fire">Urban Fire</option>
                <option value="Wildfire">Wildfire</option>
                <option value="Building Collapse">Building Collapse</option>
                <option value="Landslide">Landslide</option>
                <option value="Cyclone / Storm">Cyclone / Storm</option>
                <option value="Chemical Spill">Chemical Spill</option>
                <option value="Industrial Accident">Industrial Accident</option>
                <option value="Terrorist Attack">Terrorist Attack</option>
                <option value="Medical Emergency">Medical Emergency</option>
                <option value="Other">Other</option>
              </select>
            </label>
            <label>
              Severity Level (Shadiddat)
              <select name="severityLevel" value={formState.severityLevel} onChange={handleInputChange}>
                <option value="Low">Low / Kam</option>
                <option value="Medium">Medium / Darmiyana</option>
                <option value="High">High / Zyada</option>
                <option value="Critical">Critical / Shadeed</option>
              </select>
            </label>
          </div>
          
          <div className="event-form-grid" style={{ marginTop: '1rem' }}>
            <label style={{ gridColumn: '1 / -1' }}>
              Description
              <textarea 
                name="description" 
                value={formState.description} 
                onChange={handleInputChange} 
                rows={4} 
              />
            </label>
          </div>

          <div style={{ display: 'flex', gap: '1rem', marginTop: '2rem' }}>
            <button className="btn-primary-solid" type="submit" disabled={isSubmitting} style={{ flex: 1 }}>
              {isSubmitting ? 'Submitting...' : 'Submit Report'}
            </button>
            <button className="table-action-btn" type="button" onClick={() => navigate('/login')} style={{ flex: 1 }}>
              Back to Login
            </button>
          </div>
        </form>
      </section>
    </div>
  )
}
