import apiClient from './client'

export async function getPatients(query = {}) {
  const { data } = await apiClient.get('HospitalPatient/patients', { params: query })
  return data
}

export async function createPatient(payload) {
  const { data } = await apiClient.post('HospitalPatient/patients', payload)
  return data
}

export async function getAdmissions(query = {}) {
  const { data } = await apiClient.get('HospitalPatient/admissions', { params: query })
  return data
}

export async function createAdmission(payload) {
  const { data } = await apiClient.post('HospitalPatient/admissions', payload)
  return data
}

export async function updateAdmissionStatus(admissionId, payload) {
  const { data } = await apiClient.patch(`HospitalPatient/admissions/${admissionId}/status`, payload)
  return data
}
