export type TicketPriority = 'Low' | 'Normal' | 'High' | 'Critical';
export type TicketStatus = 'New' | 'InProgress' | 'Resolved' | 'Closed';

export const TICKET_PRIORITIES: TicketPriority[] = ['Low', 'Normal', 'High', 'Critical'];
export const TICKET_STATUSES: TicketStatus[] = ['New', 'InProgress', 'Resolved', 'Closed'];

export interface AgentSummary {
  id: number;
  fullName: string;
  active: boolean;
}

export interface Comment {
  id: number;
  authorName: string;
  body: string;
  createdDate: string;
}

export interface TicketListItem {
  id: number;
  reference: string;
  title: string;
  customerName: string;
  priority: TicketPriority;
  status: TicketStatus;
  assignedAgentId: number | null;
  assignedAgentName: string | null;
  dueDate: string;
  isOverdue: boolean;
  createdDate: string;
}

export interface TicketDetail {
  id: number;
  reference: string;
  title: string;
  description: string;
  customerName: string;
  customerEmail: string;
  priority: TicketPriority;
  status: TicketStatus;
  assignedAgentId: number | null;
  assignedAgent: AgentSummary | null;
  createdDate: string;
  lastModifiedDate: string;
  resolvedDate: string | null;
  closedDate: string | null;
  dueDate: string;
  isOverdue: boolean;
  allowedNextStatuses: TicketStatus[];
  comments: Comment[];
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  customerName: string;
  customerEmail: string;
  priority: TicketPriority;
  assignedAgentId?: number | null;
}

export interface UpdateTicketRequest {
  title: string;
  description: string;
  customerName: string;
  customerEmail: string;
  priority: TicketPriority;
}

export interface ChangeStatusRequest {
  status: TicketStatus;
}

export interface AssignAgentRequest {
  agentId: number | null;
}

export interface CreateCommentRequest {
  authorName: string;
  body: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface TicketQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: TicketStatus | '';
  priority?: TicketPriority | '';
  assignedAgentId?: number | '';
  overdueOnly?: boolean;
}
