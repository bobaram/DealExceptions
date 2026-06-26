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
      <div style={{
        padding: '32px',
        textAlign: 'center',
        color: '#64748b',
        fontSize: '14px',
      }}>
        No exceptions found.
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
                  borderLeft: isSelected ? '3px solid #1d4ed8' : '3px solid transparent',
                  background: isSelected ? '#eff6ff' : undefined,
                }}
              >
                <td>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                    <span style={{ fontWeight: 500, fontFamily: 'monospace', fontSize: '13px' }}>
                      {exc.dealRef}
                    </span>
                    {exc.isPossibleDuplicate && (
                      <span
                        title="This may be a duplicate"
                        style={{
                          color: '#b45309',
                          fontSize: '11px',
                          fontWeight: 600,
                          background: '#fef9c3',
                          border: '1px solid #fde68a',
                          borderRadius: '4px',
                          padding: '1px 5px',
                        }}
                      >
                        (duplicate?)
                      </span>
                    )}
                  </div>
                </td>
                <td>{exc.clientName}</td>
                <td style={{ color: '#475569' }}>{exc.exceptionType}</td>
                <td><PriorityBadge priority={exc.priority} /></td>
                <td><StatusBadge status={exc.status} /></td>
                <td>
                  {exc.assignedOwner
                    ? exc.assignedOwner
                    : <span style={{ color: '#94a3b8', fontStyle: 'italic' }}>(unassigned)</span>
                  }
                </td>
                <td style={{ color: '#64748b', whiteSpace: 'nowrap' }}>
                  {new Date(exc.createdAt).toLocaleDateString()}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
