import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { exceptionsApi, type ExceptionFilters, type CreateExceptionPayload } from '../api/exceptions'
import { commentsApi } from '../api/comments'
import { ExceptionList } from '../components/ExceptionList'
import { ExceptionDetail } from '../components/ExceptionDetail'
import { NewExceptionForm } from '../components/NewExceptionForm'
import ReportsPage from './ReportsPage'

type ActiveView = 'exceptions' | 'reports'

const ALL_STATUSES  = ['New', 'Pending', 'InReview', 'Approved', 'Rejected', 'Closed']
const ALL_PRIORITIES = ['Low', 'Medium', 'High', 'Critical']
const PAGE_SIZE = 20

export default function ExceptionsPage() {
  const queryClient = useQueryClient()
  const [activeView, setActiveView]   = useState<ActiveView>('exceptions')
  const [selectedId, setSelectedId]   = useState<number | null>(null)
  const [filters, setFilters]         = useState<ExceptionFilters>({})
  const [page, setPage]               = useState(1)
  const [showNewForm, setShowNewForm] = useState(false)
  const [searchInput, setSearchInput] = useState('')

  function updateFilters(patch: Partial<ExceptionFilters>) {
    setFilters(f => ({ ...f, ...patch }))
    setPage(1)
  }

  function applySearch(value: string) {
    updateFilters({ search: value || undefined })
  }

  const {
    data: result,
    isLoading: listLoading,
    error: listError,
  } = useQuery({
    queryKey: ['exceptions', filters, page],
    queryFn: () => exceptionsApi.list({ ...filters, page, pageSize: PAGE_SIZE }),
  })

  const exceptions  = result?.items ?? []
  const totalCount  = result?.totalCount ?? 0
  const totalPages  = Math.ceil(totalCount / PAGE_SIZE)

  const {
    data: selectedDetail,
    isLoading: detailLoading,
    error: detailError,
  } = useQuery({
    queryKey: ['exception', selectedId],
    queryFn: () => exceptionsApi.get(selectedId!),
    enabled: selectedId !== null,
  })

  const createMutation = useMutation({
    mutationFn: (payload: CreateExceptionPayload) => exceptionsApi.create(payload),
    onSuccess: (data) => {
      void queryClient.invalidateQueries({ queryKey: ['exceptions'] })
      setShowNewForm(false)
      setSelectedId(data.id)
    },
  })

  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status, notes, changedBy }: { id: number; status: string; notes?: string; changedBy: string }) =>
      exceptionsApi.updateStatus(id, { status, changedBy, notes }),
    onSuccess: (data) => {
      void queryClient.invalidateQueries({ queryKey: ['exceptions'] })
      void queryClient.invalidateQueries({ queryKey: ['exception', data.id] })
    },
  })

  const addCommentMutation = useMutation({
    mutationFn: ({ exceptionId, authorName, text }: { exceptionId: number; authorName: string; text: string }) =>
      commentsApi.add(exceptionId, authorName, text),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['exception', variables.exceptionId] })
    },
  })

  const openCount         = exceptions.filter(e => e.isOpen).length
  const criticalOpenCount = exceptions.filter(e => e.isOpen && e.isCritical).length
  const unassignedCount   = exceptions.filter(e => e.isOpen && !e.assignedOwner).length

  async function handleStatusChange(status: string, notes?: string) {
    if (!selectedId) return
    await updateStatusMutation.mutateAsync({ id: selectedId, status, notes, changedBy: 'User' })
  }

  async function handleAddComment(text: string) {
    if (!selectedId) return
    await addCommentMutation.mutateAsync({ exceptionId: selectedId, authorName: 'User', text })
  }

  async function handleCreateException(data: CreateExceptionPayload) {
    await createMutation.mutateAsync(data)
  }

  return (
    <div style={{ minHeight: '100vh', background: 'var(--bg)' }}>

      {/* Header */}
      <header style={{
        background: '#ffffff',
        borderBottom: '1px solid var(--border)',
        boxShadow: '0 1px 4px rgba(15,23,42,0.06)',
        padding: '14px 24px',
        position: 'sticky',
        top: 0,
        zIndex: 10,
      }}>
        <div style={{ maxWidth: '1400px', margin: '0 auto', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '16px' }}>
          <div>
            <h1 style={{ fontSize: '17px', fontWeight: 700, color: 'var(--text)', letterSpacing: '-0.01em' }}>
              Deal Exceptions Tracker
            </h1>
            <p style={{ fontSize: '12px', color: 'var(--muted)', marginTop: '1px' }}>
              Track and manage deal exception requests
            </p>
          </div>

          {/* Tab switcher */}
          <div style={{
            display: 'flex',
            background: 'var(--bg)',
            border: '1px solid var(--border)',
            borderRadius: '8px',
            padding: '3px',
            gap: '2px',
          }}>
            {(['exceptions', 'reports'] as ActiveView[]).map(view => (
              <button
                key={view}
                onClick={() => setActiveView(view)}
                style={{
                  padding: '5px 14px',
                  fontSize: '13px',
                  fontWeight: 500,
                  borderRadius: '6px',
                  border: 'none',
                  cursor: 'pointer',
                  background: activeView === view ? 'var(--surface)' : 'transparent',
                  color: activeView === view ? 'var(--text)' : 'var(--muted)',
                  boxShadow: activeView === view ? 'var(--shadow-sm)' : 'none',
                  transition: 'all 0.15s',
                  textTransform: 'capitalize',
                }}
              >
                {view}
              </button>
            ))}
          </div>

          {activeView === 'exceptions' && (
            <button className="btn btn-primary" onClick={() => setShowNewForm(true)}>
              <span style={{ fontSize: '16px', lineHeight: 1, marginRight: '2px' }}>+</span>
              New Exception
            </button>
          )}
        </div>
      </header>

      {/* Reports view */}
      {activeView === 'reports' && (
        <div style={{ maxWidth: '1400px', margin: '0 auto', padding: '20px 24px' }}>
          <ReportsPage />
        </div>
      )}

      {/* Exceptions view */}
      {activeView === 'exceptions' && (
      <div style={{ maxWidth: '1400px', margin: '0 auto', padding: '20px 24px' }}>

        {/* Stats Row */}
        <div style={{ display: 'flex', gap: '12px', marginBottom: '16px', flexWrap: 'wrap' }}>
          <div style={{
            flex: '1', minWidth: '140px',
            background: '#ffffff',
            border: '1px solid var(--border)',
            borderTop: '3px solid var(--primary)',
            borderRadius: '10px',
            padding: '14px 18px',
            boxShadow: 'var(--shadow-sm)',
          }}>
            <div style={{ fontSize: '28px', fontWeight: 700, color: 'var(--primary)', lineHeight: 1 }}>{totalCount}</div>
            <div style={{ fontSize: '12px', color: 'var(--muted)', fontWeight: 500, marginTop: '4px' }}>Total Exceptions</div>
          </div>
          <div style={{
            flex: '1', minWidth: '140px',
            background: '#ffffff',
            border: '1px solid var(--border)',
            borderTop: '3px solid var(--critical)',
            borderRadius: '10px',
            padding: '14px 18px',
            boxShadow: 'var(--shadow-sm)',
          }}>
            <div style={{ fontSize: '28px', fontWeight: 700, color: 'var(--critical)', lineHeight: 1 }}>{criticalOpenCount}</div>
            <div style={{ fontSize: '12px', color: 'var(--muted)', fontWeight: 500, marginTop: '4px' }}>Critical Open</div>
          </div>
          <div style={{
            flex: '1', minWidth: '140px',
            background: '#ffffff',
            border: '1px solid var(--border)',
            borderTop: '3px solid var(--medium)',
            borderRadius: '10px',
            padding: '14px 18px',
            boxShadow: 'var(--shadow-sm)',
          }}>
            <div style={{ fontSize: '28px', fontWeight: 700, color: 'var(--medium)', lineHeight: 1 }}>{unassignedCount}</div>
            <div style={{ fontSize: '12px', color: 'var(--muted)', fontWeight: 500, marginTop: '4px' }}>Unassigned</div>
          </div>
          <div style={{
            flex: '1', minWidth: '140px',
            background: '#ffffff',
            border: '1px solid var(--border)',
            borderTop: '3px solid var(--low)',
            borderRadius: '10px',
            padding: '14px 18px',
            boxShadow: 'var(--shadow-sm)',
          }}>
            <div style={{ fontSize: '28px', fontWeight: 700, color: 'var(--low)', lineHeight: 1 }}>{openCount}</div>
            <div style={{ fontSize: '12px', color: 'var(--muted)', fontWeight: 500, marginTop: '4px' }}>Open (this page)</div>
          </div>
        </div>

        {/* Filter Bar */}
        <div style={{
          display: 'flex',
          gap: '8px',
          marginBottom: '16px',
          flexWrap: 'wrap',
          alignItems: 'center',
          padding: '10px 12px',
          background: '#ffffff',
          border: '1px solid var(--border)',
          borderRadius: '10px',
          boxShadow: 'var(--shadow-sm)',
        }}>
          {/* Search with icon */}
          <div style={{ flex: '1', minWidth: '200px', position: 'relative' }}>
            <span style={{
              position: 'absolute', left: '10px', top: '50%', transform: 'translateY(-50%)',
              color: 'var(--subtle)', fontSize: '14px', pointerEvents: 'none',
            }}>⌕</span>
            <input
              type="text"
              placeholder="Search deal ref or client..."
              value={searchInput}
              onChange={e => setSearchInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && applySearch(searchInput)}
              onBlur={() => applySearch(searchInput)}
              style={{ margin: 0, paddingLeft: '28px' }}
            />
          </div>

          <select
            value={filters.status ?? ''}
            onChange={e => updateFilters({ status: e.target.value || undefined })}
            style={{ width: 'auto', minWidth: '130px', margin: 0 }}
          >
            <option value="">All Statuses</option>
            {ALL_STATUSES.map(s => (
              <option key={s} value={s}>{s === 'InReview' ? 'In Review' : s}</option>
            ))}
          </select>

          <select
            value={filters.priority ?? ''}
            onChange={e => updateFilters({ priority: e.target.value || undefined })}
            style={{ width: 'auto', minWidth: '130px', margin: 0 }}
          >
            <option value="">All Priorities</option>
            {ALL_PRIORITIES.map(p => (
              <option key={p} value={p}>{p}</option>
            ))}
          </select>

          <label style={{
            display: 'flex', alignItems: 'center', gap: '6px',
            cursor: 'pointer', fontSize: '13px', fontWeight: 500,
            color: 'var(--text-2)', whiteSpace: 'nowrap', margin: 0,
          }}>
            <input
              type="checkbox"
              checked={filters.openOnly ?? false}
              onChange={e => updateFilters({ openOnly: e.target.checked || undefined })}
              style={{ width: 'auto', display: 'inline-block', margin: 0, accentColor: 'var(--primary)' }}
            />
            Open only
          </label>
        </div>

        {/* Main Content */}
        <div style={{
          display: 'grid',
          gridTemplateColumns: selectedId ? '1fr 420px' : '1fr',
          gap: '16px',
          alignItems: 'start',
        }}>
          {/* List */}
          <div>
            <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
              {listLoading ? (
                <div className="loading">Loading exceptions…</div>
              ) : listError ? (
                <div className="error" style={{ margin: '16px' }}>
                  {listError instanceof Error ? listError.message : 'Unknown error'}
                </div>
              ) : (
                <ExceptionList
                  exceptions={exceptions}
                  selectedId={selectedId}
                  onSelect={setSelectedId}
                />
              )}
            </div>

            {/* Pagination */}
            {!listLoading && !listError && totalPages > 1 && (
              <div style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                gap: '8px',
                marginTop: '12px',
              }}>
                <button
                  className="btn"
                  disabled={page === 1}
                  onClick={() => setPage(p => p - 1)}
                  style={{ padding: '5px 12px', fontSize: '13px' }}
                >
                  ← Prev
                </button>
                <span style={{ fontSize: '13px', color: 'var(--muted)', minWidth: '100px', textAlign: 'center' }}>
                  Page {page} of {totalPages}
                  <span style={{ color: 'var(--subtle)', marginLeft: '6px' }}>({totalCount} total)</span>
                </span>
                <button
                  className="btn"
                  disabled={page >= totalPages}
                  onClick={() => setPage(p => p + 1)}
                  style={{ padding: '5px 12px', fontSize: '13px' }}
                >
                  Next →
                </button>
              </div>
            )}

            {/* Empty / hint */}
            {!listLoading && !listError && exceptions.length > 0 && !selectedId && totalPages <= 1 && (
              <p style={{ marginTop: '12px', textAlign: 'center', color: 'var(--subtle)', fontSize: '13px' }}>
                Select a row to view details
              </p>
            )}
          </div>

          {/* Detail */}
          {selectedId && (
            <div className="card" style={{ padding: 0, position: 'sticky', top: '76px', maxHeight: 'calc(100vh - 96px)', overflowY: 'auto' }}>
              {detailLoading ? (
                <div className="loading">Loading…</div>
              ) : detailError ? (
                <div className="error" style={{ margin: '16px' }}>
                  {detailError instanceof Error ? detailError.message : 'Unknown error'}
                </div>
              ) : selectedDetail ? (
                <ExceptionDetail
                  exception={selectedDetail}
                  onStatusChange={handleStatusChange}
                  onAddComment={handleAddComment}
                />
              ) : null}
            </div>
          )}
        </div>

      </div>
      )} {/* end exceptions view */}

      {/* New Exception Modal */}
      {showNewForm && (
        <div
          onClick={() => setShowNewForm(false)}
          style={{
            position: 'fixed', inset: 0,
            background: 'rgba(15,23,42,0.55)',
            backdropFilter: 'blur(2px)',
            zIndex: 100,
            display: 'flex', alignItems: 'flex-start', justifyContent: 'center',
            padding: '40px 16px', overflowY: 'auto',
          }}
        >
          <div
            onClick={e => e.stopPropagation()}
            style={{
              background: '#ffffff',
              borderRadius: '12px',
              padding: '28px',
              width: '100%', maxWidth: '560px',
              boxShadow: 'var(--shadow-lg)',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
              <h2 style={{ fontSize: '17px', fontWeight: 700 }}>New Exception</h2>
              <button
                onClick={() => setShowNewForm(false)}
                style={{ background: 'none', border: 'none', fontSize: '20px', cursor: 'pointer', color: 'var(--muted)', lineHeight: 1, padding: '4px' }}
                aria-label="Close"
              >
                &times;
              </button>
            </div>
            <NewExceptionForm onSubmit={handleCreateException} onCancel={() => setShowNewForm(false)} />
          </div>
        </div>
      )}
    </div>
  )
}
