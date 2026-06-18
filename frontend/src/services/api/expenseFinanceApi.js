import apiClient from './client'

export async function getExpenses(query = {}) {
  const { data } = await apiClient.get('DonationFinance/expenses', { params: query })
  return data
}

export async function createExpense(payload) {
  const { data } = await apiClient.post('DonationFinance/expenses', payload)
  return data
}

export async function updateExpensePaymentStatus(expenseId, payload) {
  const { data } = await apiClient.patch(`DonationFinance/expenses/${expenseId}/payment-status`, payload)
  return data
}
