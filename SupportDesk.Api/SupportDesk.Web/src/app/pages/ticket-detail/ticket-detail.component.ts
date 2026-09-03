import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Agent } from '../../core/models/agent.model';
import { CreateCommentRequest, TicketDetail, TicketStatus } from '../../core/models/ticket.model';
import { AgentService } from '../../core/services/agent.service';
import { TicketService } from '../../core/services/ticket.service';
import { extractErrorMessage } from '../../core/util/api-error.util';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';
import { PriorityBadgeComponent } from '../../shared/priority-badge/priority-badge.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';

const STATUS_ACTION_LABELS: Record<TicketStatus, string> = {
  New: 'New',
  InProgress: 'Start Progress',
  Resolved: 'Mark Resolved',
  Closed: 'Close Ticket'
};

/**
 * Only ever offers the status actions the API says are currently valid
 * (ticket.allowedNextStatuses) and disables every mutation once the ticket is Closed - but the
 * backend is still the final authority, so any rejection still surfaces through actionError.
 */
@Component({
  selector: 'app-ticket-detail',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    StatusBadgeComponent,
    PriorityBadgeComponent,
    ConfirmDialogComponent
  ],
  templateUrl: './ticket-detail.component.html'
})
export class TicketDetailComponent implements OnInit {
  ticket: TicketDetail | null = null;
  agents: Agent[] = [];

  loading = true;
  loadError: string | null = null;
  actionError: string | null = null;
  showDeleteConfirm = false;

  selectedAgentId: number | '' = '';

  private readonly fb = inject(FormBuilder);

  readonly commentForm = this.fb.nonNullable.group({
    authorName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    body: ['', [Validators.required, Validators.maxLength(4000)]]
  });

  private ticketId!: number;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly ticketService: TicketService,
    private readonly agentService: AgentService
  ) {}

  ngOnInit(): void {
    this.ticketId = Number(this.route.snapshot.paramMap.get('id'));
    this.agentService.getAgents(undefined, true).subscribe(agents => (this.agents = agents));
    this.load();
  }

  get isClosed(): boolean {
    return this.ticket?.status === 'Closed';
  }

  statusActionLabel(status: TicketStatus): string {
    if (status === 'InProgress' && this.ticket?.status === 'Resolved') {
      return 'Reopen (In Progress)';
    }
    return STATUS_ACTION_LABELS[status];
  }

  changeStatus(status: TicketStatus): void {
    if (!this.ticket) return;
    this.actionError = null;

    this.ticketService.changeStatus(this.ticket.id, { status }).subscribe({
      next: updated => (this.ticket = updated),
      error: err => (this.actionError = extractErrorMessage(err))
    });
  }

  assignSelectedAgent(): void {
    if (!this.ticket || this.selectedAgentId === '') return;
    this.actionError = null;

    this.ticketService.assignAgent(this.ticket.id, { agentId: Number(this.selectedAgentId) }).subscribe({
      next: updated => {
        this.ticket = updated;
        this.selectedAgentId = '';
      },
      error: err => (this.actionError = extractErrorMessage(err))
    });
  }

  unassignAgent(): void {
    if (!this.ticket) return;
    this.actionError = null;

    this.ticketService.assignAgent(this.ticket.id, { agentId: null }).subscribe({
      next: updated => (this.ticket = updated),
      error: err => (this.actionError = extractErrorMessage(err))
    });
  }

  submitComment(): void {
    if (!this.ticket || this.commentForm.invalid) {
      this.commentForm.markAllAsTouched();
      return;
    }

    this.actionError = null;
    const request: CreateCommentRequest = this.commentForm.getRawValue();

    this.ticketService.addComment(this.ticket.id, request).subscribe({
      next: () => {
        this.commentForm.reset();
        this.load();
      },
      error: err => (this.actionError = extractErrorMessage(err))
    });
  }

  confirmDelete(): void {
    if (!this.ticket) return;

    this.ticketService.deleteTicket(this.ticket.id).subscribe({
      next: () => this.router.navigate(['/tickets']),
      error: err => {
        this.actionError = extractErrorMessage(err);
        this.showDeleteConfirm = false;
      }
    });
  }

  private load(): void {
    this.loading = true;
    this.loadError = null;

    this.ticketService.getTicket(this.ticketId).subscribe({
      next: ticket => {
        this.ticket = ticket;
        this.loading = false;
      },
      error: err => {
        this.loadError = extractErrorMessage(err);
        this.loading = false;
      }
    });
  }
}
