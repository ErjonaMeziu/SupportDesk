import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AssignAgentRequest,
  ChangeStatusRequest,
  Comment,
  CreateCommentRequest,
  CreateTicketRequest,
  PagedResult,
  TicketDetail,
  TicketListItem,
  TicketQuery,
  UpdateTicketRequest
} from '../models/ticket.model';

/**
 * All ticket HTTP communication lives here. Components never call HttpClient directly -
 * they depend on this service, which keeps API shape/URL concerns out of the UI layer.
 */
@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly baseUrl = '/api/tickets';

  constructor(private readonly http: HttpClient) {}

  getTickets(query: TicketQuery): Observable<PagedResult<TicketListItem>> {
    let params = new HttpParams();

    if (query.page) params = params.set('page', query.page);
    if (query.pageSize) params = params.set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    if (query.status) params = params.set('status', query.status);
    if (query.priority) params = params.set('priority', query.priority);
    if (query.assignedAgentId) params = params.set('assignedAgentId', query.assignedAgentId);
    if (query.overdueOnly) params = params.set('overdueOnly', query.overdueOnly);

    return this.http.get<PagedResult<TicketListItem>>(this.baseUrl, { params });
  }

  getTicket(id: number): Observable<TicketDetail> {
    return this.http.get<TicketDetail>(`${this.baseUrl}/${id}`);
  }

  createTicket(request: CreateTicketRequest): Observable<TicketDetail> {
    return this.http.post<TicketDetail>(this.baseUrl, request);
  }

  updateTicket(id: number, request: UpdateTicketRequest): Observable<TicketDetail> {
    return this.http.put<TicketDetail>(`${this.baseUrl}/${id}`, request);
  }

  deleteTicket(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  changeStatus(id: number, request: ChangeStatusRequest): Observable<TicketDetail> {
    return this.http.patch<TicketDetail>(`${this.baseUrl}/${id}/status`, request);
  }

  assignAgent(id: number, request: AssignAgentRequest): Observable<TicketDetail> {
    return this.http.patch<TicketDetail>(`${this.baseUrl}/${id}/assignment`, request);
  }

  addComment(id: number, request: CreateCommentRequest): Observable<Comment> {
    return this.http.post<Comment>(`${this.baseUrl}/${id}/comments`, request);
  }
}
