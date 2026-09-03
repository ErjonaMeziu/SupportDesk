import { Component, Input } from '@angular/core';
import { TicketStatus } from '../../core/models/ticket.model';

const STATUS_LABELS: Record<TicketStatus, string> = {
  New: 'New',
  InProgress: 'In Progress',
  Resolved: 'Resolved',
  Closed: 'Closed'
};

@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `<span class="badge" [class]="'badge-status-' + status.toLowerCase()">{{ label }}</span>`
})
export class StatusBadgeComponent {
  @Input({ required: true }) status!: TicketStatus;

  get label(): string {
    return STATUS_LABELS[this.status];
  }
}
