import apiClient from './client'

export async function getPatients(query = {}) {
  const { data } = await apiClient.get('HospitalPatient/patients', { params: query })
  return data
}

export async function getHospitals(query = {}) {
  const { data } = await apiClient.get('HospitalPatient/hospitals', { params: query })
  return data
}

export async function getAdmissions(query = {}) {
  const { data } = await apiClient.get('HospitalPatient/admissions', { params: query })
  return data
}

export async function routePatientToHospital(hospitalId, payload) {
  const { data } = await apiClient.post(`HospitalPatient/hospitals/${hospitalId}/route-patient`, payload)
  return data
}

export async function autoRoutePatient(payload) {
  const { data } = await apiClient.post('HospitalPatient/hospitals/route-patient/auto', payload)
  return data
}
