import type { ExceptionSummary } from '../types'
import { StatusBadge } from './StatusBadge'
import { PriorityBadge } from './PriorityBadge'

interface Props {
  exceptions: ExceptionSummary[]
  selectedId: number | null
  onSelect: (id: number) => void
}

export function ExceptionList({ exceptions, selectedId, onSelect }: Props) {
  if (exceptions.length === 0) {
    return (
      <div style={{ padding: '48px 32px', textAlign: 'center' }}>
        <div style={{ fontSize: '32px', marginBottom: '8px' }}>📋</div>
        <div style={{ fontWeight: 600, color: 'var(--text)', marginBottom: '4px' }}>No exceptions found</div>
        <div style={{ fontSize: '13px', color: 'var(--muted)' }}>Try adjusting your filters</div>
      </div>
    )
  }

  return (
    <div style={{ overflowX: 'auto' }}>
      <table>
        <thead>
          <tr>
            <th>Deal Ref</th>
            <th>Client</th>
            <th>Type</th>
            <th>Priority</th>
            <th>Status</th>
            <th>Owner</th>
            <th>Created</th>
          </tr>
        </thead>
        <tbody>
          {exceptions.map(exc => {
            const isSelected = exc.id === selectedId
            return (
              <tr
                key={exc.id}
                onClick={() => onSelect(exc.id)}
                style={{
                  borderLeft: isSelected ? '3px solid var(--primary)' : '3px solid transparent',
                  background: isSelected ? 'var(--primary-light)' : undefined,
                }}
              >
                <td>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px', flexWrap: 'wrap' }}>
                    <span style={{ fontWeight: 600, fontFamily: 'monospace', fontSize: '12px', color: 'var(--text)' }}>
                      {exc.dealRef}
                    </span>
                    {exc.isPossibleDuplicate && (
                      <span title="May be a duplicate" style={{
                        color: '#92400e',
                        fontSize: '10px',
                        fontWeight: 700,
                        background: '#fef3c7',
                        border: '1px solid #fde68a',
                        borderRadius: '4px',
                        padding: '1px 5px',
                        letterSpacing: '0.02em',
                      }}>
                        DUPE?
                      </span>
                    )}
                  </div>
                </td>
                <td style={{ fontWeight: 500 }}>{exc.clientName}</td>
                <td style={{ color: 'var(--muted)' }}>{exc.exceptionType}</td>
                <td><PriorityBadge priority={exc.priority} /></td>
                <td><StatusBadge status={exc.status} /></td>
                <td>
                  {exc.assignedOwner
                    ? <span style={{ color: 'var(--text-2)' }}>{exc.assignedOwner}</span>
                    : <span style={{ color: 'var(--subtle)', fontStyle: 'italic', fontSize: '12px' }}>Unassigned</span>
                  }
                </td>
                <td style={{ color: 'var(--muted)', whiteSpace: 'nowrap', fontSize: '12px' }}>
                  {new Date(exc.createdAt).toLocaleDateString('en-ZA', { day: 'numeric', month: 'short', year: 'numeric' })}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
