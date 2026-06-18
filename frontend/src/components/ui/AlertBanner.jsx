const variantToTone = {
  info: 'info',
  warning: 'warning',
  success: 'success',
  danger: 'danger',
}

export function AlertBanner({
  variant = 'info',
  title,
  message,
  children,
}) {
  const tone = variantToTone[variant] || 'info'

  return (
    <section className={`alert-banner ${tone}`} role="status" aria-live="polite">
      <div className="alert-banner-content">
        {title ? <strong>{title}</strong> : null}
        {message ? <p>{message}</p> : null}
      </div>
      {children ? <div className="alert-banner-actions">{children}</div> : null}
    </section>
  )
}
