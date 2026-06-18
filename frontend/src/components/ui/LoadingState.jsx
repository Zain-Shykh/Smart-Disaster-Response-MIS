export function LoadingState({ title = 'Loading data...', message = 'Please wait while content is prepared.' }) {
  return (
    <div className="loading-state" role="status" aria-live="polite">
      <div className="loading-state-spinner" aria-hidden="true"></div>
      <div>
        <strong>{title}</strong>
        <p>{message}</p>
      </div>
    </div>
  )
}
