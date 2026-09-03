import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AgentService } from '../../core/services/agent.service';
import { TicketDetail } from '../../core/models/ticket.model';
import { TicketService } from '../../core/services/ticket.service';
import { TicketDetailComponent } from './ticket-detail.component';

function buildTicket(overrides: Partial<TicketDetail>): TicketDetail {
  return {
    id: 1,
    reference: 'TCK-2026-0001',
    title: 'Cannot access account',
    description: 'Customer is locked out after a password reset attempt.',
    customerName: 'Jane Doe',
    customerEmail: 'jane@example.com',
    priority: 'Normal',
    status: 'New',
    assignedAgentId: null,
    assignedAgent: null,
    createdDate: '2026-09-01T10:00:00Z',
    lastModifiedDate: '2026-09-01T10:00:00Z',
    resolvedDate: null,
    closedDate: null,
    dueDate: '2026-09-04T10:00:00Z',
    isOverdue: false,
    allowedNextStatuses: ['InProgress'],
    comments: [],
    ...overrides
  };
}

async function renderWithTicket(ticket: TicketDetail) {
  const ticketServiceSpy = jasmine.createSpyObj('TicketService', [
    'getTicket',
    'changeStatus',
    'assignAgent',
    'addComment',
    'deleteTicket'
  ]);
  ticketServiceSpy.getTicket.and.returnValue(of(ticket));

  const agentServiceSpy = jasmine.createSpyObj('AgentService', ['getAgents']);
  agentServiceSpy.getAgents.and.returnValue(of([]));

  await TestBed.configureTestingModule({
    imports: [TicketDetailComponent],
    providers: [
      provideRouter([]),
      { provide: TicketService, useValue: ticketServiceSpy },
      { provide: AgentService, useValue: agentServiceSpy },
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => '1' } } } }
    ]
  }).compileComponents();

  const fixture = TestBed.createComponent(TicketDetailComponent);
  fixture.detectChanges();
  return fixture;
}

describe('TicketDetailComponent', () => {
  it('offers status actions and an enabled comment form for an open ticket', async () => {
    const fixture = await renderWithTicket(buildTicket({ status: 'New', allowedNextStatuses: ['InProgress'] }));
    const el: HTMLElement = fixture.nativeElement;

    expect(el.querySelector('.status-actions button')).withContext('status action button').toBeTruthy();
    expect(el.querySelector('form textarea')).withContext('comment textarea').toBeTruthy();
    expect(el.querySelector('.banner-readonly')).toBeFalsy();
  });

  it('hides every mutation control behind the read-only banner once a ticket is Closed', async () => {
    const fixture = await renderWithTicket(buildTicket({ status: 'Closed', allowedNextStatuses: [] }));
    const el: HTMLElement = fixture.nativeElement;

    expect(el.querySelector('.banner-readonly')).withContext('read-only banner').toBeTruthy();
    expect(el.querySelector('.status-actions button')).withContext('no status actions').toBeFalsy();
    expect(el.querySelector('form textarea')).withContext('no comment form').toBeFalsy();
    expect(el.querySelector('#assignAgent')).withContext('no assignment control').toBeFalsy();
    expect(el.querySelector('a[href$="/edit"]')).withContext('no edit link').toBeFalsy();
  });
});
