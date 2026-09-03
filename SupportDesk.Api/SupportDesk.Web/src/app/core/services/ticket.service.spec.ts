import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TicketService } from './ticket.service';

describe('TicketService', () => {
  let service: TicketService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TicketService]
    });

    service = TestBed.inject(TicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getTickets sends every active filter as a query parameter and omits empty ones', () => {
    service
      .getTickets({ page: 2, pageSize: 25, search: 'invoice', status: 'InProgress', overdueOnly: true })
      .subscribe();

    const req = httpMock.expectOne(
      r =>
        r.url === '/api/tickets' &&
        r.params.get('page') === '2' &&
        r.params.get('pageSize') === '25' &&
        r.params.get('search') === 'invoice' &&
        r.params.get('status') === 'InProgress' &&
        r.params.get('overdueOnly') === 'true'
    );

    expect(req.request.method).toBe('GET');
    expect(req.request.params.has('priority')).toBeFalse();
    expect(req.request.params.has('assignedAgentId')).toBeFalse();

    req.flush({ items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 });
  });

  it('changeStatus PATCHes the dedicated status endpoint with the requested status', () => {
    service.changeStatus(7, { status: 'Resolved' }).subscribe();

    const req = httpMock.expectOne('/api/tickets/7/status');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ status: 'Resolved' });

    req.flush({});
  });

  it('assignAgent PATCHes the assignment endpoint, sending null to unassign', () => {
    service.assignAgent(7, { agentId: null }).subscribe();

    const req = httpMock.expectOne('/api/tickets/7/assignment');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ agentId: null });

    req.flush({});
  });

  it('createTicket POSTs to /api/tickets with the given payload', () => {
    const payload = {
      title: 'Cannot log in',
      description: 'Customer is locked out after a password reset.',
      customerName: 'Jane Doe',
      customerEmail: 'jane@example.com',
      priority: 'High' as const
    };

    service.createTicket(payload).subscribe();

    const req = httpMock.expectOne('/api/tickets');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);

    req.flush({});
  });
});
