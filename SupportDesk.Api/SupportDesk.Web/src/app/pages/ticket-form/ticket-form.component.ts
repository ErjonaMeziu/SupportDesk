import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Agent } from '../../core/models/agent.model';
import { TICKET_PRIORITIES, TicketPriority } from '../../core/models/ticket.model';
import { AgentService } from '../../core/services/agent.service';
import { TicketService } from '../../core/services/ticket.service';
import { extractErrorMessage } from '../../core/util/api-error.util';

/**
 * Handles both create (no :id route param) and edit (:id present). Client-side validation here
 * mirrors the backend's rules for a responsive UX, but every rule is re-enforced server-side -
 * a stale or bypassed form still gets a clear, readable error via submitError.
 */
@Component({
  selector: 'app-ticket-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './ticket-form.component.html'
})
export class TicketFormComponent implements OnInit {
  readonly priorities: TicketPriority[] = TICKET_PRIORITIES;

  isEditMode = false;
  private ticketId: number | null = null;

  loading = false;
  saving = false;
  loadError: string | null = null;
  submitError: string | null = null;
  closedReadOnly = false;

  agents: Agent[] = [];

  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(200)]],
    description: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(4000)]],
    customerName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    customerEmail: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    priority: ['Normal' as TicketPriority, [Validators.required]],
    assignedAgentId: ['']
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly ticketService: TicketService,
    private readonly agentService: AgentService
  ) {}

  ngOnInit(): void {
    this.agentService.getAgents(undefined, true).subscribe(agents => (this.agents = agents));

    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam) {
      return;
    }

    this.isEditMode = true;
    this.ticketId = Number(idParam);
    this.loading = true;

    this.ticketService.getTicket(this.ticketId).subscribe({
      next: ticket => {
        if (ticket.status === 'Closed') {
          this.closedReadOnly = true;
          this.form.disable();
        }

        this.form.patchValue({
          title: ticket.title,
          description: ticket.description,
          customerName: ticket.customerName,
          customerEmail: ticket.customerEmail,
          priority: ticket.priority
        });
        this.loading = false;
      },
      error: err => {
        this.loadError = extractErrorMessage(err);
        this.loading = false;
      }
    });
  }

  submit(): void {
    if (this.form.invalid || this.closedReadOnly) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.submitError = null;
    const value = this.form.getRawValue();

    const request$ = this.isEditMode && this.ticketId
      ? this.ticketService.updateTicket(this.ticketId, {
          title: value.title,
          description: value.description,
          customerName: value.customerName,
          customerEmail: value.customerEmail,
          priority: value.priority
        })
      : this.ticketService.createTicket({
          title: value.title,
          description: value.description,
          customerName: value.customerName,
          customerEmail: value.customerEmail,
          priority: value.priority,
          assignedAgentId: value.assignedAgentId ? Number(value.assignedAgentId) : null
        });

    request$.subscribe({
      next: ticket => this.router.navigate(['/tickets', ticket.id]),
      error: err => {
        this.submitError = extractErrorMessage(err);
        this.saving = false;
      }
    });
  }
}
