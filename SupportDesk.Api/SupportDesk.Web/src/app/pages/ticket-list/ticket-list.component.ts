import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Agent } from '../../core/models/agent.model';
import { TICKET_PRIORITIES, TICKET_STATUSES, TicketListItem, TicketPriority, TicketQuery, TicketStatus } from '../../core/models/ticket.model';
import { AgentService } from '../../core/services/agent.service';
import { TicketService } from '../../core/services/ticket.service';
import { extractErrorMessage } from '../../core/util/api-error.util';
import { PriorityBadgeComponent } from '../../shared/priority-badge/priority-badge.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';

/**
 * All search/filter/pagination state here is resolved server-side (TicketService.getTickets ->
 * GET /api/tickets with query params) - the component never filters a locally-cached list.
 */
@Component({
  selector: 'app-ticket-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink, StatusBadgeComponent, PriorityBadgeComponent],
  templateUrl: './ticket-list.component.html'
})
export class TicketListComponent implements OnInit, OnDestroy {
  readonly statuses: TicketStatus[] = TICKET_STATUSES;
  readonly priorities: TicketPriority[] = TICKET_PRIORITIES;

  tickets: TicketListItem[] = [];
  agents: Agent[] = [];

  page = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  statusFilter: TicketStatus | '' = '';
  priorityFilter: TicketPriority | '' = '';
  agentFilter: number | '' = '';
  overdueOnly = false;
  readonly searchControl = new FormControl('', { nonNullable: true });

  loading = true;
  error: string | null = null;

  private searchSubscription?: Subscription;

  constructor(
    private readonly ticketService: TicketService,
    private readonly agentService: AgentService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.agentService.getAgents().subscribe(agents => (this.agents = agents));

    this.searchSubscription = this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => {
        this.page = 1;
        this.load();
      });

    this.load();
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
  }

  onFilterChange(): void {
    this.page = 1;
    this.load();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) {
      return;
    }
    this.page = page;
    this.load();
  }

  openTicket(ticket: TicketListItem): void {
    this.router.navigate(['/tickets', ticket.id]);
  }

  load(): void {
    this.loading = true;
    this.error = null;

    const query: TicketQuery = {
      page: this.page,
      pageSize: this.pageSize,
      search: this.searchControl.value.trim() || undefined,
      status: this.statusFilter || undefined,
      priority: this.priorityFilter || undefined,
      assignedAgentId: this.agentFilter || undefined,
      overdueOnly: this.overdueOnly || undefined
    };

    this.ticketService.getTickets(query).subscribe({
      next: result => {
        this.tickets = result.items;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.loading = false;
      },
      error: err => {
        this.error = extractErrorMessage(err);
        this.loading = false;
      }
    });
  }
}
