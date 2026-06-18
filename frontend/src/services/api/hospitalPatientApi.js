import apiClient from './client'

export async function getHospitals(query = {}) {
  const { data } = await apiClient.get('HospitalPatient/hospitals', { params: query })
  return data
}

export async function createHospital(payload) {
  const { data } = await apiClient.post('HospitalPatient/hospitals', payload)
  return data
}

export async function updateHospitalBeds(hospitalId, payload) {
  const { data } = await apiClient.patch(`HospitalPatient/hospitals/${hospitalId}/beds`, payload)
  return data
}

export async function searchHospitalsBySpecialization(query = {}) {
  const { data } = await apiClient.get('HospitalPatient/hospitals/search', { params: query })
  return data
}
