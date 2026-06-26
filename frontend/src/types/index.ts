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
