const labels: Record<string, string> = {
  InReview: 'In Review',
}

const colors: Record<string, string> = {
  New: '#dbeafe',
  Pending: '#fef9c3',
  InReview: '#e0f2fe',
  Approved: '#dcfce7',
  Rejected: '#fee2e2',
  Closed: '#f1f5f9',
}

const textColors: Record<string, string> = {
  New: '#1e40af',
  Pending: '#854d0e',
  InReview: '#0369a1',
  Approved: '#166534',
  Rejected: '#991b1b',
  Closed: '#475569',
}

export function StatusBadge({ status }: { status: string }) {
  return (
    <span style={{
      display: 'inline-block',
      padding: '2px 10px',
      borderRadius: '9999px',
      fontSize: '12px',
      fontWeight: 600,
      background: colors[status] ?? '#f1f5f9',
      color: textColors[status] ?? '#475569',
    }}>
      {labels[status] ?? status}
    </span>
  )
}
