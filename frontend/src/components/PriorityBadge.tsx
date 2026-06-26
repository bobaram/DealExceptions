const colors: Record<string, { bg: string; text: string }> = {
  Critical: { bg: '#fee2e2', text: '#991b1b' },
  High: { bg: '#ffedd5', text: '#9a3412' },
  Medium: { bg: '#fef9c3', text: '#854d0e' },
  Low: { bg: '#dcfce7', text: '#166534' },
}

export function PriorityBadge({ priority }: { priority: string }) {
  const c = colors[priority] ?? { bg: '#f1f5f9', text: '#475569' }
  return (
    <span style={{
      display: 'inline-block',
      padding: '2px 10px',
      borderRadius: '9999px',
      fontSize: '12px',
      fontWeight: 600,
      background: c.bg,
      color: c.text,
    }}>
      {priority}
    </span>
  )
}
