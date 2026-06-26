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

  void currentUser

  return (
    <div>
      <div className="meta-label" style={{ marginBottom: '14px' }}>Activity</div>

      {/* Timeline */}
      {timeline.length === 0 ? (
        <p style={{ color: 'var(--muted)', fontSize: '13px', marginBottom: '16px' }}>No activity yet.</p>
      ) : (
        <div style={{ marginBottom: '20px' }}>
          {timeline.map((item, i) => {
            const isComment = item.type === 'comment'
            const isLast = i === timeline.length - 1
            return (
              <div key={i} style={{ display: 'flex', gap: '12px', paddingBottom: isLast ? 0 : '12px' }}>
                {/* Left: dot + line */}
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', flexShrink: 0 }}>
                  <div style={{
                    width: '28px', height: '28px', borderRadius: '50%',
                    background: isComment ? 'var(--primary-light)' : '#f0fdf4',
                    border: `2px solid ${isComment ? 'var(--primary-border)' : '#bbf7d0'}`,
                    display: 'flex', alignItems: 'center', justifyContent: 'center',
                    fontSize: '12px', flexShrink: 0,
                  }}>
                    {isComment ? '💬' : '↻'}
                  </div>
                  {!isLast && (
                    <div style={{ width: '2px', flex: 1, background: 'var(--border)', marginTop: '4px' }} />
                  )}
                </div>

                {/* Right: content */}
                <div style={{ flex: 1, paddingTop: '4px' }}>
                  {isComment ? (
                    <div>
                      <div style={{ display: 'flex', alignItems: 'baseline', gap: '8px', marginBottom: '4px', flexWrap: 'wrap' }}>
                        <span style={{ fontWeight: 600, fontSize: '13px', color: 'var(--text)' }}>{item.data.authorName}</span>
                        <span style={{ fontSize: '11px', color: 'var(--subtle)' }}>{formatDate(item.data.createdAt)}</span>
                      </div>
                      <p style={{ fontSize: '13px', color: 'var(--text-2)', lineHeight: '1.55', whiteSpace: 'pre-wrap' }}>
                        {item.data.text}
                      </p>
                    </div>
                  ) : (
                    <div>
                      <div style={{ display: 'flex', alignItems: 'baseline', gap: '8px', marginBottom: '4px', flexWrap: 'wrap' }}>
                        <span style={{ fontWeight: 600, fontSize: '12px', color: '#15803d', textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                          Status changed
                        </span>
                        <span style={{ fontSize: '11px', color: 'var(--subtle)' }}>{formatDate(item.data.changedAt)}</span>
                      </div>
                      <p style={{ fontSize: '13px', color: 'var(--text-2)' }}>
                        <span style={{ fontWeight: 500 }}>{item.data.changedBy}</span>
                        {' · '}
                        <span style={{ color: 'var(--muted)' }}>{item.data.fromStatus}</span>
                        <span style={{ margin: '0 4px', color: 'var(--subtle)' }}>→</span>
                        <span style={{ fontWeight: 600, color: 'var(--text)' }}>{item.data.toStatus}</span>
                        {item.data.notes && (
                          <span style={{ color: 'var(--muted)' }}> — {item.data.notes}</span>
                        )}
                      </p>
                    </div>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}

      {/* Add comment form */}
      <form onSubmit={handleSubmit}>
        <textarea
          value={text}
          onChange={e => setText(e.target.value)}
          placeholder="Add a comment…"
          rows={3}
          disabled={submitting}
          style={{ width: '100%', marginBottom: '8px', resize: 'vertical' }}
        />
        {error && <p style={{ color: 'var(--critical)', fontSize: '12px', marginBottom: '8px' }}>{error}</p>}
        <button type="submit" disabled={submitting || !text.trim()} className="btn btn-primary" style={{ fontSize: '13px' }}>
          {submitting ? 'Adding…' : 'Add Comment'}
        </button>
      </form>
    </div>
  )
}
