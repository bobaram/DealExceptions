import { api } from './client'
import type { ExceptionSummary, ExceptionDetail, PagedResult } from '../types'

export interface ExceptionFilters {
  status?: string
  priority?: string
  search?: string
  openOnly?: boolean
  page?: number
  pageSize?: number
}

export interface CreateExceptionPayload {
  dealRef: string
  clientName: string
  exceptionType: string
  description: string
  priority: string
  assignedOwner?: string
  createdBy: string
}

export interface UpdateStatusPayload {
  status: string
  changedBy: string
  notes?: string
}

function buildQuery(filters: ExceptionFilters): string {
  const p = new URLSearchParams()
  if (filters.status)   p.set('status', filters.status)
  if (filters.priority) p.set('priority', filters.priority)
  if (filters.search)   p.set('search', filters.search)
  if (filters.openOnly) p.set('openOnly', 'true')
  if (filters.page && filters.page > 1) p.set('page', String(filters.page))
  if (filters.pageSize) p.set('pageSize', String(filters.pageSize))
  const s = p.toString()
  return s ? `?${s}` : ''
}

export const exceptionsApi = {
  list: (filters: ExceptionFilters = {}) =>
    api.get<PagedResult<ExceptionSummary>>(`/exceptions${buildQuery(filters)}`),
  get: (id: number) =>
    api.get<ExceptionDetail>(`/exceptions/${id}`),
  create: (payload: CreateExceptionPayload) =>
    api.post<ExceptionDetail>('/exceptions', payload),
  updateStatus: (id: number, payload: UpdateStatusPayload) =>
    api.patch<ExceptionDetail>(`/exceptions/${id}/status`, payload),
}
