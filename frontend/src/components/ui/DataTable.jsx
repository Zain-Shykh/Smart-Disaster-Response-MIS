function defaultRowKey(row, index) {
  if (row && (row.id || row.key)) {
    return row.id || row.key
  }

  return index
}

export function DataTable({
  columns,
  rows,
  caption,
  emptyMessage = 'No records available.',
  getRowKey = defaultRowKey,
}) {
  const hasRows = Array.isArray(rows) && rows.length > 0

  return (
    <div className="data-table-wrapper">
      <table className="data-table">
        {caption ? <caption>{caption}</caption> : null}
        <thead>
          <tr>
            {columns.map((column) => (
              <th
                key={column.key}
                scope="col"
                className={column.align ? `align-${column.align}` : ''}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {hasRows ? (
            rows.map((row, rowIndex) => (
              <tr key={getRowKey(row, rowIndex)}>
                {columns.map((column) => (
                  <td
                    key={column.key}
                    className={column.align ? `align-${column.align}` : ''}
                  >
                    {typeof column.render === 'function'
                      ? column.render(row, rowIndex)
                      : row[column.key]}
                  </td>
                ))}
              </tr>
            ))
          ) : (
            <tr>
              <td colSpan={columns.length} className="data-table-empty">
                {emptyMessage}
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}
