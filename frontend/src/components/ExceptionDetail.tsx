import { useState } from 'react'
import type { ExceptionDetail as ExceptionDetailType } from '../types'
import { StatusBadge } from './StatusBadge'
import { PriorityBadge } from './PriorityBadge'
import { CommentFeed } from './CommentFeed'

interface Props {
  exception: ExceptionDetailType
  onStatusChange: (status: string, notes?: string) => Promise<void>
  onAddComment: (text: string) => Promise<void>
}

const ALL_STATUSES = ['New', 'Pending', 'InReview', 'Approved', 'Rejected', 'Closed']

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('en-ZA', { dateStyle: 'medium', timeStyle: 'short' })
}

export function ExceptionDetail({ exception, onStatusChange, onAddComment }: Props) {
  const [newStatus, setNewStatus] = useState(exception.status)
  const [statusNotes, setStatusNotes] = useState('')
  const [savingStatus, setSavingStatus] = useState(false)
  const [statusError, setStatusError] = useState<string | null>(null)

  async function handleStatusSave() {
    if (newStatus === exception.status && !statusNotes.trim()) return
    setSavingStatus(true)
    setStatusError(null)
    try {
      await onStatusChange(newStatus, statusNotes.trim() || undefined)
      setStatusNotes('')
    } catch (err) {
      setStatusError(err instanceof Error ? err.message : 'Failed to update status')
    } finally {
      setSavingStatus(false)
    }
  }

  const labelStyle: React.CSSProperties = {
    fontSize: '11px',
    fontWeight: 600,
    textTransform: 'uppercase',
    letterSpacing: '0.05em',
    color: '#64748b',
    marginBottom: '2px',
  }

  const valueStyle: React.CSSProperties = {
    fontSize: '14px',
    color: '#0f172a',
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Duplicate Warning */}
      {exception.isPossibleDuplicate && (
        <div style={{
          padding: '10px 14px',
          background: '#fffbeb',
          border: '1px solid #fde68a',
          borderRadius: '8px',
          color: '#92400e',
          fontSize: '13px',
          display: 'flex',
          alignItems: 'flex-start',
          gap: '8px',
        }}>
          <span style={{ fontSize: '16px', lineHeight: 1 }}>&#9888;</span>
          <span>Warning: this row may be a duplicate from a legacy Excel import</span>
        </div>
      )}

      {/* Header */}
      <div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '6px', flexWrap: 'wrap' }}>
          <span style={{ fontFamily: 'monospace', fontWeight: 700, fontSize: '16px' }}>{exception.dealRef}</span>
          <PriorityBadge priority={exception.priority} />
          <StatusBadge status={exception.status} />
        </div>
        <div style={{ fontSize: '15px', fontWeight: 600, color: '#0f172a' }}>{exception.clientName}</div>
        <div style={{ fontSize: '13px', color: '#64748b', marginTop: '2px' }}>{exception.exceptionType}</div>
      </div>

      {/* Details Grid */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: '1fr 1fr',
        gap: '12px 20px',
      }}>
        <div>
          <div style={labelStyle}>Assigned Owner</div>
          <div style={valueStyle}>
            {exception.assignedOwner
              ? exception.assignedOwner
              : <span style={{ color: '#94a3b8', fontStyle: 'italic' }}>Unassigned</span>
            }
          </div>
        </div>
        <div>
          <div style={labelStyle}>Created</div>
          <div style={valueStyle}>{formatDate(exception.createdAt)}</div>
        </div>
        <div>
          <div style={labelStyle}>Last Updated</div>
          <div style={valueStyle}>{formatDate(exception.updatedAt)}</div>
        </div>
        <div>
          <div style={labelStyle}>Exception ID</div>
          <div style={{ ...valueStyle, fontFamily: 'monospace', color: '#64748b' }}>#{exception.id}</div>
        </div>
      </div>

      {/* Description */}
      <div>
        <div style={labelStyle}>Description</div>
        <div style={{
          marginTop: '6px',
          padding: '12px',
          background: '#f8fafc',
          border: '1px solid #e2e8f0',
          borderRadius: '6px',
          fontSize: '14px',
          lineHeight: '1.6',
          whiteSpace: 'pre-wrap',
          color: '#334155',
        }}>
          {exception.description}
        </div>
      </div>

      {/* Status Change */}
      <div style={{
        padding: '16px',
        background: '#f8fafc',
        border: '1px solid #e2e8f0',
        borderRadius: '8px',
      }}>
        <h3 style={{ fontSize: '14px', fontWeight: 600, marginBottom: '12px' }}>Update Status</h3>
        <div style={{ marginBottom: '10px' }}>
          <label htmlFor="ed-status" style={{ fontSize: '12px', fontWeight: 500, marginBottom: '4px', display: 'block', color: '#64748b' }}>
            New Status
          </label>
          <select
            id="ed-status"
            value={newStatus}
            onChange={e => setNewStatus(e.target.value)}
            disabled={savingStatus}
          >
            {ALL_STATUSES.map(s => (
              <option key={s} value={s}>
                {s === 'InReview' ? 'In Review' : s}
              </option>
            ))}
          </select>
        </div>
        <div style={{ marginBottom: '10px' }}>
          <label htmlFor="ed-notes" style={{ fontSize: '12px', fontWeight: 500, marginBottom: '4px', display: 'block', color: '#64748b' }}>
            Notes <span style={{ fontWeight: 400, color: '#94a3b8' }}>(optional)</span>
          </label>
          <textarea
            id="ed-notes"
            value={statusNotes}
            onChange={e => setStatusNotes(e.target.value)}
            placeholder="Reason for status change..."
            rows={2}
            disabled={savingStatus}
          />
        </div>
        {statusError && (
          <p style={{ color: '#dc2626', fontSize: '13px', marginBottom: '8px' }}>{statusError}</p>
        )}
        <button
          className="btn btn-primary"
          onClick={handleStatusSave}
          disabled={savingStatus || (newStatus === exception.status && !statusNotes.trim())}
          style={{ fontSize: '13px' }}
        >
          {savingStatus ? 'Saving...' : 'Save Status'}
        </button>
      </div>

      {/* Activity / Comments */}
      <div>
        <CommentFeed
          comments={exception.comments}
          statusHistories={exception.statusHistories}
          onAddComment={onAddComment}
        />
      </div>
    </div>
  )
}
