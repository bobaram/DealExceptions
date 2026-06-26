export interface ExceptionSummary {
  id: number;
  dealRef: string;
  clientName: string;
  exceptionType: string;
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  status: 'New' | 'Pending' | 'InReview' | 'Approved' | 'Rejected' | 'Closed';
  assignedOwner: string | null;
  createdAt: string;
  updatedAt: string;
  isOpen: boolean;
  isCritical: boolean;
  isPossibleDuplicate: boolean;
}

export interface ExceptionDetail extends ExceptionSummary {
  description: string;
  comments: Comment[];
  statusHistories: StatusHistory[];
}

export interface Comment {
  id: number;
  authorName: string;
  text: string;
  createdAt: string;
}

export interface StatusHistory {
  id: number;
  fromStatus: string;
  toStatus: string;
  changedBy: string;
  changedAt: string;
  notes: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface OpenByOwnerRow {
  owner: string;
  count: number;
}

export interface CriticalOverdueRow {
  id: number;
  dealRef: string;
  clientName: string;
  owner: string;
  createdAt: string;
  status: string;
  daysOpen: number;
}

export interface ByStatusPriorityRow {
  status: string;
  priority: string;
  count: number;
}

export interface AvgTimeToCloseRow {
  exceptionType: string;
  avgDaysToClose: number;
}
