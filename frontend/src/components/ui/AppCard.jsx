export function AppCard({ title, subtitle, children, actions }) {
  return (
    <article className="ui-card">
      {title || subtitle || actions ? (
        <header className="ui-card-header">
          <div>
            {title ? <h3 className="ui-card-title">{title}</h3> : null}
            {subtitle ? <p className="ui-card-subtitle">{subtitle}</p> : null}
          </div>
          {actions ? <div className="ui-card-actions">{actions}</div> : null}
        </header>
      ) : null}
      <div className="ui-card-body">{children}</div>
    </article>
  )
}
