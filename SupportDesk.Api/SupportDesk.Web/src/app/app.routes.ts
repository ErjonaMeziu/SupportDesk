import { Routes } from '@angular/router';
import { TicketListComponent } from './pages/ticket-list/ticket-list.component';
import { TicketDetailComponent } from './pages/ticket-detail/ticket-detail.component';
import { TicketFormComponent } from './pages/ticket-form/ticket-form.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tickets' },
  { path: 'tickets', component: TicketListComponent },
  { path: 'tickets/new', component: TicketFormComponent },
  { path: 'tickets/:id/edit', component: TicketFormComponent },
  { path: 'tickets/:id', component: TicketDetailComponent },
  { path: '**', redirectTo: 'tickets' }
];
