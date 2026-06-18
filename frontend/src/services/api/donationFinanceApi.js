import apiClient from './client'

export async function getDonors(query = {}) {
  const { data } = await apiClient.get('DonationFinance/donors', { params: query })
  return data
}

export async function createDonor(payload) {
  const { data } = await apiClient.post('DonationFinance/donors', payload)
  return data
}

export async function getDonations(query = {}) {
  const { data } = await apiClient.get('DonationFinance/donations', { params: query })
  return data
}

export async function createDonation(payload) {
  const { data } = await apiClient.post('DonationFinance/donations', payload)
  return data
}

export async function getDonationsSummary() {
  const { data } = await apiClient.get('DonationFinance/donations/summary')
  return data
}

export async function getExpensesSummary() {
  const { data } = await apiClient.get('DonationFinance/expenses/summary')
  return data
}

export async function updateDonationStatus(donationId, payload) {
  const { data } = await apiClient.patch(`DonationFinance/donations/${donationId}/status`, payload)
  return data
}

export async function getDonorPhones(donorId) {
  const { data } = await apiClient.get(`Donor/${donorId}/Phone`)
  return data
}

export async function addDonorPhone(donorId, payload) {
  const { data } = await apiClient.post(`Donor/${donorId}/Phone`, payload)
  return data
}

export async function updateDonorPhone(donorId, currentPhone, payload) {
  const { data } = await apiClient.put(`Donor/${donorId}/Phone/${encodeURIComponent(currentPhone)}`, payload)
  return data
}

export async function deleteDonorPhone(donorId, currentPhone) {
  await apiClient.delete(`Donor/${donorId}/Phone/${encodeURIComponent(currentPhone)}`)
}
