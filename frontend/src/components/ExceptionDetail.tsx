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

  return (
    <div>
      {/* Duplicate Warning */}
      {exception.isPossibleDuplicate && (
        <div style={{
          padding: '10px 16px',
          background: '#fffbeb',
          borderBottom: '1px solid #fde68a',
          color: '#92400e',
          fontSize: '12px',
          fontWeight: 500,
          display: 'flex',
          alignItems: 'center',
          gap: '6px',
        }}>
          <span>⚠</span>
          May be a duplicate from a legacy import
        </div>
      )}

      {/* Header */}
      <div style={{ padding: '20px 20px 16px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '8px', flexWrap: 'wrap' }}>
          <PriorityBadge priority={exception.priority} />
          <StatusBadge status={exception.status} />
          <span style={{ marginLeft: 'auto', fontFamily: 'monospace', fontSize: '11px', color: 'var(--subtle)' }}>
            #{exception.id}
          </span>
        </div>
        <div style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text)', marginBottom: '2px' }}>
          {exception.clientName}
        </div>
        <div style={{ fontSize: '13px', color: 'var(--muted)' }}>
          <span style={{ fontFamily: 'monospace', fontWeight: 600, color: 'var(--text-2)' }}>{exception.dealRef}</span>
          <span style={{ margin: '0 6px', color: 'var(--subtle)' }}>·</span>
          {exception.exceptionType}
        </div>
      </div>

      <div className="divider" />

      {/* Metadata grid */}
      <div style={{ padding: '16px 20px', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px 24px' }}>
        <div>
          <div className="meta-label">Owner</div>
          <div style={{ fontSize: '13px', color: 'var(--text-2)' }}>
            {exception.assignedOwner ?? <span style={{ color: 'var(--subtle)', fontStyle: 'italic' }}>Unassigned</span>}
          </div>
        </div>
        <div>
          <div className="meta-label">Created</div>
          <div style={{ fontSize: '13px', color: 'var(--text-2)' }}>{formatDate(exception.createdAt)}</div>
        </div>
        <div>
          <div className="meta-label">Last Updated</div>
          <div style={{ fontSize: '13px', color: 'var(--text-2)' }}>{formatDate(exception.updatedAt)}</div>
        </div>
      </div>

      <div className="divider" />

      {/* Description */}
      <div style={{ padding: '16px 20px' }}>
        <div className="meta-label" style={{ marginBottom: '8px' }}>Description</div>
        <div style={{
          borderLeft: '3px solid var(--primary-border)',
          paddingLeft: '12px',
          fontSize: '13px',
          lineHeight: '1.65',
          whiteSpace: 'pre-wrap',
          color: 'var(--text-2)',
        }}>
          {exception.description}
        </div>
      </div>

      <div className="divider" />

      {/* Status Update */}
      <div style={{ padding: '16px 20px' }}>
        <div className="meta-label" style={{ marginBottom: '12px' }}>Update Status</div>
        <div style={{ marginBottom: '10px' }}>
          <label htmlFor="ed-status" style={{ fontSize: '12px', color: 'var(--muted)', marginBottom: '4px' }}>New Status</label>
          <select
            id="ed-status"
            value={newStatus}
            onChange={e => setNewStatus(e.target.value as typeof newStatus)}
            disabled={savingStatus}
          >
            {ALL_STATUSES.map(s => (
              <option key={s} value={s}>{s === 'InReview' ? 'In Review' : s}</option>
            ))}
          </select>
        </div>
        <div style={{ marginBottom: '10px' }}>
          <label htmlFor="ed-notes" style={{ fontSize: '12px', color: 'var(--muted)', marginBottom: '4px' }}>
            Notes <span style={{ fontWeight: 400, color: 'var(--subtle)' }}>(optional)</span>
          </label>
          <textarea
            id="ed-notes"
            value={statusNotes}
            onChange={e => setStatusNotes(e.target.value)}
            placeholder="Reason for status change…"
            rows={2}
            disabled={savingStatus}
          />
        </div>
        {statusError && <p style={{ color: 'var(--critical)', fontSize: '12px', marginBottom: '8px' }}>{statusError}</p>}
        <button
          className="btn btn-primary"
          onClick={handleStatusSave}
          disabled={savingStatus || (newStatus === exception.status && !statusNotes.trim())}
          style={{ fontSize: '13px' }}
        >
          {savingStatus ? 'Saving…' : 'Save Status'}
        </button>
      </div>

      <div className="divider" />

      {/* Activity */}
      <div style={{ padding: '16px 20px 20px' }}>
        <CommentFeed
          comments={exception.comments}
          statusHistories={exception.statusHistories}
          onAddComment={onAddComment}
        />
      </div>
    </div>
  )
}
