import type { Comment, StatusHistory } from '../types'
import { useState } from 'react'

interface Props {
  comments: Comment[]
  statusHistories: StatusHistory[]
  onAddComment: (text: string) => Promise<void>
  currentUser?: string
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('en-ZA', { dateStyle: 'medium', timeStyle: 'short' })
}

export function CommentFeed({ comments, statusHistories, onAddComment, currentUser = 'User' }: Props) {
  const [text, setText] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const timeline = [
    ...comments.map(c => ({ type: 'comment' as const, at: c.createdAt, data: c })),
    ...statusHistories.map(h => ({ type: 'status' as const, at: h.changedAt, data: h })),
  ].sort((a, b) => a.at.localeCompare(b.at))

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!text.trim()) return
    setSubmitting(true)
    setError(null)
    try {
      await onAddComment(text.trim())
      setText('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add comment')
    } finally {
      setSubmitting(false)
    }
  }

  // currentUser is intentionally used via the prop interface for future personalization
  void currentUser

  return (
    <div>
      <h3 style={{ marginBottom: '12px', fontSize: '15px', fontWeight: 600 }}>Activity</h3>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginBottom: '16px' }}>
        {timeline.length === 0 && <p style={{ color: '#64748b', fontSize: '14px' }}>No activity yet.</p>}
        {timeline.map((item, i) => (
          <div key={i} style={{
            padding: '10px 12px',
            borderRadius: '8px',
            fontSize: '14px',
            background: item.type === 'comment' ? '#f8fafc' : '#f0fdf4',
            border: '1px solid',
            borderColor: item.type === 'comment' ? '#e2e8f0' : '#bbf7d0',
          }}>
            {item.type === 'comment' ? (
              <>
                <strong>{item.data.authorName}</strong>
                <span style={{ color: '#64748b', fontSize: '12px', marginLeft: '8px' }}>{formatDate(item.data.createdAt)}</span>
                <p style={{ margin: '4px 0 0', whiteSpace: 'pre-wrap' }}>{item.data.text}</p>
              </>
            ) : (
              <>
                <span style={{ color: '#16a34a', fontSize: '12px', fontWeight: 600 }}>STATUS CHANGE</span>
                <span style={{ color: '#64748b', fontSize: '12px', marginLeft: '8px' }}>{formatDate(item.data.changedAt)}</span>
                <p style={{ margin: '4px 0 0' }}>
                  <strong>{item.data.changedBy}</strong> changed status from <strong>{item.data.fromStatus}</strong> to <strong>{item.data.toStatus}</strong>
                  {item.data.notes && <span style={{ color: '#64748b' }}> — {item.data.notes}</span>}
                </p>
              </>
            )}
          </div>
        ))}
      </div>

      <form onSubmit={handleSubmit}>
        <textarea
          value={text}
          onChange={e => setText(e.target.value)}
          placeholder="Add a comment..."
          rows={3}
          disabled={submitting}
          style={{ width: '100%', marginBottom: '8px', resize: 'vertical' }}
        />
        {error && <p style={{ color: '#dc2626', fontSize: '13px', marginBottom: '8px' }}>{error}</p>}
        <button type="submit" disabled={submitting || !text.trim()} className="btn btn-primary">
          {submitting ? 'Adding...' : 'Add Comment'}
        </button>
      </form>
    </div>
  )
}
