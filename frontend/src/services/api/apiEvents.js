const problemListeners = new Set()
const unauthorizedListeners = new Set()

export function subscribeApiProblem(listener) {
  problemListeners.add(listener)
  return () => {
    problemListeners.delete(listener)
  }
}

export function subscribeUnauthorized(listener) {
  unauthorizedListeners.add(listener)
  return () => {
    unauthorizedListeners.delete(listener)
  }
}

export function emitApiProblem(payload) {
  problemListeners.forEach((listener) => listener(payload))
}

export function emitUnauthorized(payload) {
  unauthorizedListeners.forEach((listener) => listener(payload))
}
