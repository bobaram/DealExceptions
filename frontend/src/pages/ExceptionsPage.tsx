import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { exceptionsApi, type ExceptionFilters, type CreateExceptionPayload } from '../api/exceptions'
import { commentsApi } from '../api/comments'
import { ExceptionList } from '../components/ExceptionList'
import { ExceptionDetail } from '../components/ExceptionDetail'
import { NewExceptionForm } from '../components/NewExceptionForm'

const ALL_STATUSES = ['New', 'Pending', 'InReview', 'Approved', 'Rejected', 'Closed']
const ALL_PRIORITIES = ['Low', 'Medium', 'High', 'Critical']

export default function ExceptionsPage() {
  const queryClient = useQueryClient()
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [filters, setFilters] = useState<ExceptionFilters>({})
  const [showNewForm, setShowNewForm] = useState(false)
  const [searchInput, setSearchInput] = useState('')

  // Debounce-like: update search filter on Enter or blur
  function applySearch(value: string) {
    setFilters(f => ({ ...f, search: value || undefined }))
  }

  // Fetch exception list
  const {
    data: exceptions,
    isLoading: listLoading,
    error: listError,
  } = useQuery({
    queryKey: ['exceptions', filters],
    queryFn: () => exceptionsApi.list(filters),
  })

  // Fetch selected exception detail
  const {
    data: selectedDetail,
    isLoading: detailLoading,
    error: detailError,
  } = useQuery({
    queryKey: ['exception', selectedId],
    queryFn: () => exceptionsApi.get(selectedId!),
    enabled: selectedId !== null,
  })

  // Create exception mutation
  const createMutation = useMutation({
    mutationFn: (payload: CreateExceptionPayload) => exceptionsApi.create(payload),
    onSuccess: (data) => {
      void queryClient.invalidateQueries({ queryKey: ['exceptions'] })
      setShowNewForm(false)
      setSelectedId(data.id)
    },
  })

  // Update status mutation
  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status, notes, changedBy }: { id: number; status: string; notes?: string; changedBy: string }) =>
      exceptionsApi.updateStatus(id, { status, changedBy, notes }),
    onSuccess: (data) => {
      void queryClient.invalidateQueries({ queryKey: ['exceptions'] })
      void queryClient.invalidateQueries({ queryKey: ['exception', data.id] })
    },
  })

  // Add comment mutation
  const addCommentMutation = useMutation({
    mutationFn: ({ exceptionId, authorName, text }: { exceptionId: number; authorName: string; text: string }) =>
      commentsApi.add(exceptionId, authorName, text),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['exception', variables.exceptionId] })
    },
  })

  // Summary stats
  const openCount = exceptions?.filter(e => e.isOpen).length ?? 0
  const criticalOpenCount = exceptions?.filter(e => e.isOpen && e.isCritical).length ?? 0
  const unassignedCount = exceptions?.filter(e => e.isOpen && !e.assignedOwner).length ?? 0

  async function handleStatusChange(status: string, notes?: string) {
    if (!selectedId) return
    await updateStatusMutation.mutateAsync({
      id: selectedId,
      status,
      notes,
      changedBy: 'User',
    })
  }

  async function handleAddComment(text: string) {
    if (!selectedId) return
    await addCommentMutation.mutateAsync({
      exceptionId: selectedId,
      authorName: 'User',
      text,
    })
  }

  async function handleCreateException(data: CreateExceptionPayload) {
    await createMutation.mutateAsync(data)
  }

  return (
    <div style={{ minHeight: '100vh', background: '#f8fafc' }}>
      {/* Header */}
      <header style={{
        background: '#ffffff',
        borderBottom: '1px solid #e2e8f0',
        padding: '16px 24px',
        position: 'sticky',
        top: 0,
        zIndex: 10,
      }}>
        <div style={{ maxWidth: '1400px', margin: '0 auto' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '12px' }}>
            <div>
              <h1 style={{ fontSize: '20px', fontWeight: 700, color: '#0f172a' }}>Deal Exceptions Tracker</h1>
              <p style={{ fontSize: '13px', color: '#64748b', marginTop: '2px' }}>Track and manage deal exception requests</p>
            </div>
            <button
              className="btn btn-primary"
              onClick={() => setShowNewForm(true)}
              style={{ gap: '6px' }}
            >
              <span style={{ fontSize: '18px', lineHeight: 1 }}>+</span>
              New Exception
            </button>
          </div>

          {/* Summary Cards */}
          <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap' }}>
            <div style={{
              padding: '10px 16px',
              background: '#eff6ff',
              border: '1px solid #bfdbfe',
              borderRadius: '8px',
              minWidth: '120px',
            }}>
              <div style={{ fontSize: '22px', fontWeight: 700, color: '#1d4ed8' }}>{openCount}</div>
              <div style={{ fontSize: '12px', color: '#3b82f6', fontWeight: 500 }}>Open Exceptions</div>
            </div>
            <div style={{
              padding: '10px 16px',
              background: '#fef2f2',
              border: '1px solid #fecaca',
              borderRadius: '8px',
              minWidth: '120px',
            }}>
              <div style={{ fontSize: '22px', fontWeight: 700, color: '#dc2626' }}>{criticalOpenCount}</div>
              <div style={{ fontSize: '12px', color: '#ef4444', fontWeight: 500 }}>Critical Open</div>
            </div>
            <div style={{
              padding: '10px 16px',
              background: '#fffbeb',
              border: '1px solid #fde68a',
              borderRadius: '8px',
              minWidth: '120px',
            }}>
              <div style={{ fontSize: '22px', fontWeight: 700, color: '#d97706' }}>{unassignedCount}</div>
              <div style={{ fontSize: '12px', color: '#f59e0b', fontWeight: 500 }}>Unassigned</div>
            </div>
          </div>
        </div>
      </header>

      <div style={{ maxWidth: '1400px', margin: '0 auto', padding: '20px 24px' }}>
        {/* Filter Bar */}
        <div style={{
          display: 'flex',
          gap: '10px',
          marginBottom: '16px',
          flexWrap: 'wrap',
          alignItems: 'center',
          padding: '12px 16px',
          background: '#ffffff',
          border: '1px solid #e2e8f0',
          borderRadius: '8px',
        }}>
          <div style={{ flex: '1', minWidth: '180px' }}>
            <input
              type="text"
              placeholder="Search deal ref, client, type..."
              value={searchInput}
              onChange={e => setSearchInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && applySearch(searchInput)}
              onBlur={() => applySearch(searchInput)}
              style={{ margin: 0 }}
            />
          </div>

          <select
            value={filters.status ?? ''}
            onChange={e => setFilters(f => ({ ...f, status: e.target.value || undefined }))}
            style={{ width: 'auto', minWidth: '140px', margin: 0 }}
          >
            <option value="">All Statuses</option>
            {ALL_STATUSES.map(s => (
              <option key={s} value={s}>{s === 'InReview' ? 'In Review' : s}</option>
            ))}
          </select>

          <select
            value={filters.priority ?? ''}
            onChange={e => setFilters(f => ({ ...f, priority: e.target.value || undefined }))}
            style={{ width: 'auto', minWidth: '140px', margin: 0 }}
          >
            <option value="">All Priorities</option>
            {ALL_PRIORITIES.map(p => (
              <option key={p} value={p}>{p}</option>
            ))}
          </select>

          <label style={{
            display: 'flex',
            alignItems: 'center',
            gap: '6px',
            cursor: 'pointer',
            fontSize: '13px',
            fontWeight: 500,
            color: '#374151',
            whiteSpace: 'nowrap',
            margin: 0,
          }}>
            <input
              type="checkbox"
              checked={filters.openOnly ?? false}
              onChange={e => setFilters(f => ({ ...f, openOnly: e.target.checked || undefined }))}
              style={{ width: 'auto', display: 'inline-block', margin: 0 }}
            />
            Open only
          </label>
        </div>

        {/* Main Content: Two Column Layout */}
        <div style={{
          display: 'grid',
          gridTemplateColumns: selectedId ? '1fr 420px' : '1fr',
          gap: '16px',
          alignItems: 'start',
        }}>
          {/* Left: Exception List */}
          <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
            {listLoading ? (
              <div className="loading">Loading exceptions...</div>
            ) : listError ? (
              <div className="error">
                Failed to load exceptions: {listError instanceof Error ? listError.message : 'Unknown error'}
              </div>
            ) : (
              <ExceptionList
                exceptions={exceptions ?? []}
                selectedId={selectedId}
                onSelect={setSelectedId}
              />
            )}
          </div>

          {/* Right: Exception Detail */}
          {selectedId && (
            <div className="card" style={{ position: 'sticky', top: '140px', maxHeight: 'calc(100vh - 160px)', overflowY: 'auto' }}>
              {detailLoading ? (
                <div className="loading">Loading details...</div>
              ) : detailError ? (
                <div className="error">
                  Failed to load details: {detailError instanceof Error ? detailError.message : 'Unknown error'}
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

          {/* Placeholder when nothing selected */}
          {!selectedId && !listLoading && (
            <div style={{
              display: 'none',
            }} />
          )}
        </div>

        {/* Empty state hint when list is loaded but nothing selected */}
        {!listLoading && !listError && (exceptions ?? []).length > 0 && !selectedId && (
          <div style={{
            marginTop: '12px',
            textAlign: 'center',
            color: '#94a3b8',
            fontSize: '13px',
          }}>
            Select an exception to view details
          </div>
        )}
      </div>

      {/* New Exception Modal */}
      {showNewForm && (
        <div
          onClick={() => setShowNewForm(false)}
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(15, 23, 42, 0.5)',
            zIndex: 100,
            display: 'flex',
            alignItems: 'flex-start',
            justifyContent: 'center',
            padding: '40px 16px',
            overflowY: 'auto',
          }}
        >
          <div
            onClick={e => e.stopPropagation()}
            style={{
              background: '#ffffff',
              borderRadius: '12px',
              padding: '28px',
              width: '100%',
              maxWidth: '560px',
              boxShadow: '0 20px 60px -10px rgba(0,0,0,0.3)',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
              <h2 style={{ fontSize: '18px', fontWeight: 700 }}>New Exception</h2>
              <button
                onClick={() => setShowNewForm(false)}
                style={{
                  background: 'none',
                  border: 'none',
                  fontSize: '20px',
                  cursor: 'pointer',
                  color: '#64748b',
                  lineHeight: 1,
                  padding: '4px',
                }}
                aria-label="Close"
              >
                &times;
              </button>
            </div>
            <NewExceptionForm
              onSubmit={handleCreateException}
              onCancel={() => setShowNewForm(false)}
            />
          </div>
        </div>
      )}
    </div>
  )
}
