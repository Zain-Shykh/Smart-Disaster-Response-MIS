export function hasTrimmedText(value, minLength = 1) {
  return typeof value === 'string' && value.trim().length >= minLength
}

export function isPositiveIntegerString(value) {
  if (value === null || value === undefined) {
    return false
  }

  const text = String(value).trim()
  if (!text) {
    return false
  }

  const parsed = Number(text)
  return Number.isInteger(parsed) && parsed > 0
}

export function isPositiveDecimalString(value) {
  if (value === null || value === undefined) {
    return false
  }

  const text = String(value).trim()
  if (!text) {
    return false
  }

  const parsed = Number(text)
  return Number.isFinite(parsed) && parsed > 0
}
