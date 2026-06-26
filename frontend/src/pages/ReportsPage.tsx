import { useQuery } from '@tanstack/react-query'
import { reportsApi } from '../api/reports'
import type { ByStatusPriorityRow } from '../types'

const PRIORITIES = ['Low', 'Medium', 'High', 'Critical']
const STATUSES = ['New', 'Pending', 'InReview', 'Approved', 'Rejected', 'Closed']

const PRIORITY_COLORS: Record<string, string> = {
  Critical: '#991b1b',
  High: '#9a3412',
  Medium: '#854d0e',
  Low: '#166534',
}

function SectionHeader({ title, subtitle }: { title: string; subtitle?: string }) {
  return (
    <div style={{ marginBottom: '16px' }}>
      <h2 style={{ fontSize: '15px', fontWeight: 700, color: 'var(--text)' }}>{title}</h2>
      {subtitle && <p style={{ fontSize: '12px', color: 'var(--muted)', marginTop: '2px' }}>{subtitle}</p>}
    </div>
  )
}

function CardShell({ children, accent }: { children: React.ReactNode; accent?: string }) {
  return (
    <div style={{
      background: 'var(--surface)',
      border: '1px solid var(--border)',
      borderTop: accent ? `3px solid ${accent}` : '1px solid var(--border)',
      borderRadius: '10px',
      padding: '20px',
      boxShadow: 'var(--shadow-sm)',
    }}>
      {children}
    </div>
  )
}

function LoadingCard() {
  return <div className="loading" style={{ padding: '32px' }}>Loading…</div>
}

function ErrorCard({ message }: { message: string }) {
  return <div className="error" style={{ margin: 0 }}>{message}</div>
}

// ── Critical Overdue ──────────────────────────────────────────────────────────
function CriticalOverduePanel() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['reports', 'critical-overdue'],
    queryFn: reportsApi.criticalOverdue,
  })

  return (
    <CardShell accent="#dc2626">
      <SectionHeader
        title="Critical Overdue"
        subtitle="Critical exceptions open more than 3 days"
      />
      {isLoading ? <LoadingCard /> : error ? (
        <ErrorCard message={error instanceof Error ? error.message : 'Failed to load'} />
      ) : !data?.length ? (
        <div style={{ padding: '24px', textAlign: 'center', color: 'var(--muted)', fontSize: '13px' }}>
          ✓ No critical overdue exceptions
        </div>
      ) : (
        <div style={{ overflowX: 'auto' }}>
          <table>
            <thead>
              <tr>
                <th>Deal Ref</th>
                <th>Client</th>
                <th>Owner</th>
                <th>Status</th>
                <th>Days Open</th>
              </tr>
            </thead>
            <tbody>
              {data.map(row => (
                <tr key={row.id}>
                  <td style={{ fontFamily: 'monospace', fontWeight: 600, fontSize: '12px' }}>{row.dealRef}</td>
                  <td style={{ fontWeight: 500 }}>{row.clientName}</td>
                  <td style={{ color: 'var(--muted)' }}>{row.owner}</td>
                  <td>{row.status}</td>
                  <td>
                    <span style={{
                      display: 'inline-block',
                      padding: '2px 10px',
                      borderRadius: '9999px',
                      fontSize: '12px',
                      fontWeight: 700,
                      background: row.daysOpen > 7 ? '#fee2e2' : '#ffedd5',
                      color: row.daysOpen > 7 ? '#991b1b' : '#9a3412',
                    }}>
                      {row.daysOpen}d
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </CardShell>
  )
}

// ── Open by Owner ─────────────────────────────────────────────────────────────
function OpenByOwnerPanel() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['reports', 'open-by-owner'],
    queryFn: reportsApi.openByOwner,
  })

  const max = data ? Math.max(...data.map(r => r.count), 1) : 1

  return (
    <CardShell accent="var(--primary)">
      <SectionHeader title="Open by Owner" subtitle="Open exceptions per assignee" />
      {isLoading ? <LoadingCard /> : error ? (
        <ErrorCard message={error instanceof Error ? error.message : 'Failed to load'} />
      ) : !data?.length ? (
        <div style={{ padding: '24px', textAlign: 'center', color: 'var(--muted)', fontSize: '13px' }}>No open exceptions</div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
          {data.map(row => (
            <div key={row.owner}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                <span style={{ fontSize: '13px', fontWeight: 500, color: 'var(--text-2)' }}>{row.owner}</span>
                <span style={{ fontSize: '13px', fontWeight: 700, color: 'var(--primary)' }}>{row.count}</span>
              </div>
              <div style={{ height: '6px', background: 'var(--bg)', borderRadius: '3px', overflow: 'hidden' }}>
                <div style={{
                  height: '100%',
                  width: `${(row.count / max) * 100}%`,
                  background: 'var(--primary)',
                  borderRadius: '3px',
                  transition: 'width 0.3s ease',
                }} />
              </div>
            </div>
          ))}
        </div>
      )}
    </CardShell>
  )
}

// ── Avg Time to Close ─────────────────────────────────────────────────────────
function AvgTimeToClosePanel() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['reports', 'avg-time-to-close'],
    queryFn: reportsApi.avgTimeToClose,
  })

  const max = data ? Math.max(...data.map(r => r.avgDaysToClose), 1) : 1

  return (
    <CardShell accent="var(--low)">
      <SectionHeader title="Avg Days to Close" subtitle="By exception type (closed / approved / rejected)" />
      {isLoading ? <LoadingCard /> : error ? (
        <ErrorCard message={error instanceof Error ? error.message : 'Failed to load'} />
      ) : !data?.length ? (
        <div style={{ padding: '24px', textAlign: 'center', color: 'var(--muted)', fontSize: '13px' }}>No closed exceptions yet</div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
          {data.map(row => (
            <div key={row.exceptionType}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                <span style={{ fontSize: '13px', fontWeight: 500, color: 'var(--text-2)' }}>{row.exceptionType}</span>
                <span style={{ fontSize: '13px', fontWeight: 700, color: 'var(--low)' }}>{row.avgDaysToClose.toFixed(1)}d</span>
              </div>
              <div style={{ height: '6px', background: 'var(--bg)', borderRadius: '3px', overflow: 'hidden' }}>
                <div style={{
                  height: '100%',
                  width: `${(row.avgDaysToClose / max) * 100}%`,
                  background: 'var(--low)',
                  borderRadius: '3px',
                  transition: 'width 0.3s ease',
                }} />
              </div>
            </div>
          ))}
        </div>
      )}
    </CardShell>
  )
}

// ── By Status × Priority matrix ───────────────────────────────────────────────
function ByStatusPriorityPanel() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['reports', 'by-status-priority'],
    queryFn: reportsApi.byStatusPriority,
  })

  function lookup(rows: ByStatusPriorityRow[], status: string, priority: string) {
    return rows.find(r => r.status === status && r.priority === priority)?.count ?? 0
  }

  const presentStatuses = data
    ? STATUSES.filter(s => data.some(r => r.status === s))
    : STATUSES

  return (
    <CardShell accent="var(--medium)">
      <SectionHeader title="By Status × Priority" subtitle="Count of exceptions in each status / priority combination" />
      {isLoading ? <LoadingCard /> : error ? (
        <ErrorCard message={error instanceof Error ? error.message : 'Failed to load'} />
      ) : !data?.length ? (
        <div style={{ padding: '24px', textAlign: 'center', color: 'var(--muted)', fontSize: '13px' }}>No data</div>
      ) : (
        <div style={{ overflowX: 'auto' }}>
          <table style={{ minWidth: '400px' }}>
            <thead>
              <tr>
                <th style={{ textAlign: 'left' }}>Status</th>
                {PRIORITIES.map(p => (
                  <th key={p} style={{ textAlign: 'center', color: PRIORITY_COLORS[p] }}>{p}</th>
                ))}
                <th style={{ textAlign: 'center', color: 'var(--muted)' }}>Total</th>
              </tr>
            </thead>
            <tbody>
              {presentStatuses.map(status => {
                const rowTotal = PRIORITIES.reduce((sum, p) => sum + lookup(data, status, p), 0)
                return (
                  <tr key={status}>
                    <td style={{ fontWeight: 500 }}>{status === 'InReview' ? 'In Review' : status}</td>
                    {PRIORITIES.map(p => {
                      const count = lookup(data, status, p)
                      return (
                        <td key={p} style={{ textAlign: 'center' }}>
                          {count > 0 ? (
                            <span style={{
                              display: 'inline-block',
                              minWidth: '28px',
                              padding: '2px 8px',
                              borderRadius: '6px',
                              fontSize: '12px',
                              fontWeight: 700,
                              background: p === 'Critical' ? '#fee2e2' : p === 'High' ? '#ffedd5' : p === 'Medium' ? '#fef9c3' : '#dcfce7',
                              color: PRIORITY_COLORS[p],
                            }}>
                              {count}
                            </span>
                          ) : (
                            <span style={{ color: 'var(--subtle)', fontSize: '12px' }}>—</span>
                          )}
                        </td>
                      )
                    })}
                    <td style={{ textAlign: 'center', fontWeight: 700, color: 'var(--text-2)', fontSize: '13px' }}>{rowTotal}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </CardShell>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────
export default function ReportsPage() {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      <CriticalOverduePanel />
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
        <OpenByOwnerPanel />
        <AvgTimeToClosePanel />
      </div>
      <ByStatusPriorityPanel />
    </div>
  )
}
