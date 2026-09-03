import { Component, Input } from '@angular/core';
import { TicketPriority } from '../../core/models/ticket.model';

@Component({
  selector: 'app-priority-badge',
  standalone: true,
  template: `<span class="badge" [class]="'badge-priority-' + priority.toLowerCase()">{{ priority }}</span>`
})
export class PriorityBadgeComponent {
  @Input({ required: true }) priority!: TicketPriority;
}
