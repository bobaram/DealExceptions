import { api } from './client'
import type { OpenByOwnerRow, CriticalOverdueRow, ByStatusPriorityRow, AvgTimeToCloseRow } from '../types'

export const reportsApi = {
  openByOwner: () => api.get<OpenByOwnerRow[]>('/reports/open-by-owner'),
  criticalOverdue: () => api.get<CriticalOverdueRow[]>('/reports/critical-overdue'),
  byStatusPriority: () => api.get<ByStatusPriorityRow[]>('/reports/by-status-priority'),
  avgTimeToClose: () => api.get<AvgTimeToCloseRow[]>('/reports/avg-time-to-close'),
}
