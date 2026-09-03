import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Agent } from '../models/agent.model';

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly baseUrl = '/api/agents';

  constructor(private readonly http: HttpClient) {}

  getAgents(search?: string, activeOnly?: boolean): Observable<Agent[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    if (activeOnly !== undefined) params = params.set('activeOnly', activeOnly);

    return this.http.get<Agent[]>(this.baseUrl, { params });
  }

  getAgent(id: number): Observable<Agent> {
    return this.http.get<Agent>(`${this.baseUrl}/${id}`);
  }
}
