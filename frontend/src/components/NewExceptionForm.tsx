import { useState } from 'react'
import type { CreateExceptionPayload } from '../api/exceptions'

interface Props {
  onSubmit: (data: CreateExceptionPayload) => Promise<void>
  onCancel: () => void
  defaultCreatedBy: string
}

const EXCEPTION_TYPES = [
  'Affordability',
  'Credit Limit',
  'Missing Docs',
  'Pricing Override',
  'Director Approval',
  'Data Mismatch',
  'Security Docs',
  'Other',
]

const PRIORITIES = ['Low', 'Medium', 'High', 'Critical']

interface FormErrors {
  dealRef?: string
  clientName?: string
  exceptionType?: string
  description?: string
  priority?: string
}

export function NewExceptionForm({ onSubmit, onCancel, defaultCreatedBy }: Props) {
  const [dealRef, setDealRef] = useState('')
  const [clientName, setClientName] = useState('')
  const [exceptionType, setExceptionType] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState('Medium')
  const [assignedOwner, setAssignedOwner] = useState('')
  const [errors, setErrors] = useState<FormErrors>({})
  const [apiError, setApiError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  function validate(): FormErrors {
    const e: FormErrors = {}
    if (!dealRef.trim()) e.dealRef = 'Deal Ref is required'
    if (!clientName.trim()) e.clientName = 'Client Name is required'
    if (!exceptionType) e.exceptionType = 'Exception Type is required'
    if (!description.trim()) e.description = 'Description is required'
    if (!priority) e.priority = 'Priority is required'
    return e
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const validationErrors = validate()
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors)
      return
    }
    setErrors({})
    setApiError(null)
    setSubmitting(true)
    try {
      const payload: CreateExceptionPayload = {
        dealRef: dealRef.trim(),
        clientName: clientName.trim(),
        exceptionType,
        description: description.trim(),
        priority,
        createdBy: defaultCreatedBy,
      }
      if (assignedOwner.trim()) {
        payload.assignedOwner = assignedOwner.trim()
      }
      await onSubmit(payload)
    } catch (err) {
      setApiError(err instanceof Error ? err.message : 'Failed to create exception')
    } finally {
      setSubmitting(false)
    }
  }

  const fieldStyle: React.CSSProperties = {
    marginBottom: '16px',
  }

  const errorStyle: React.CSSProperties = {
    color: '#dc2626',
    fontSize: '12px',
    marginTop: '4px',
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <div style={fieldStyle}>
        <label htmlFor="nef-dealRef">Deal Ref *</label>
        <input
          id="nef-dealRef"
          type="text"
          value={dealRef}
          onChange={e => setDealRef(e.target.value)}
          placeholder="e.g. DEAL-2024-001"
          disabled={submitting}
        />
        {errors.dealRef && <p style={errorStyle}>{errors.dealRef}</p>}
      </div>

      <div style={fieldStyle}>
        <label htmlFor="nef-clientName">Client Name *</label>
        <input
          id="nef-clientName"
          type="text"
          value={clientName}
          onChange={e => setClientName(e.target.value)}
          placeholder="e.g. Acme Corp"
          disabled={submitting}
        />
        {errors.clientName && <p style={errorStyle}>{errors.clientName}</p>}
      </div>

      <div style={fieldStyle}>
        <label htmlFor="nef-exceptionType">Exception Type *</label>
        <select
          id="nef-exceptionType"
          value={exceptionType}
          onChange={e => setExceptionType(e.target.value)}
          disabled={submitting}
        >
          <option value="">Select exception type...</option>
          {EXCEPTION_TYPES.map(t => (
            <option key={t} value={t}>{t}</option>
          ))}
        </select>
        {errors.exceptionType && <p style={errorStyle}>{errors.exceptionType}</p>}
      </div>

      <div style={fieldStyle}>
        <label htmlFor="nef-description">Description *</label>
        <textarea
          id="nef-description"
          value={description}
          onChange={e => setDescription(e.target.value)}
          placeholder="Describe the exception in detail..."
          rows={4}
          disabled={submitting}
        />
        {errors.description && <p style={errorStyle}>{errors.description}</p>}
      </div>

      <div style={fieldStyle}>
        <label htmlFor="nef-priority">Priority *</label>
        <select
          id="nef-priority"
          value={priority}
          onChange={e => setPriority(e.target.value)}
          disabled={submitting}
        >
          {PRIORITIES.map(p => (
            <option key={p} value={p}>{p}</option>
          ))}
        </select>
        {errors.priority && <p style={errorStyle}>{errors.priority}</p>}
      </div>

      <div style={fieldStyle}>
        <label htmlFor="nef-assignedOwner">Assigned Owner <span style={{ color: '#64748b', fontWeight: 400 }}>(optional)</span></label>
        <input
          id="nef-assignedOwner"
          type="text"
          value={assignedOwner}
          onChange={e => setAssignedOwner(e.target.value)}
          placeholder="e.g. Jane Smith"
          disabled={submitting}
        />
      </div>

      <div style={fieldStyle}>
        <label>Submitting as</label>
        <p style={{
          fontSize: '14px', color: 'var(--text)',
          padding: '8px 10px', margin: 0,
          background: 'var(--bg)', border: '1px solid var(--border)', borderRadius: '6px',
        }}>
          {defaultCreatedBy}
        </p>
      </div>

      {apiError && (
        <div style={{
          marginBottom: '16px',
          padding: '10px 14px',
          background: '#fef2f2',
          border: '1px solid #fecaca',
          borderRadius: '6px',
          color: '#dc2626',
          fontSize: '13px',
        }}>
          {apiError}
        </div>
      )}

      <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
        <button type="button" className="btn btn-ghost" onClick={onCancel} disabled={submitting}>
          Cancel
        </button>
        <button type="submit" className="btn btn-primary" disabled={submitting}>
          {submitting ? 'Creating...' : 'Create Exception'}
        </button>
      </div>
    </form>
  )
}
