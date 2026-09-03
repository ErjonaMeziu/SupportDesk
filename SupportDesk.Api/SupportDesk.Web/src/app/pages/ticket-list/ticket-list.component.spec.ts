import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AgentService } from '../../core/services/agent.service';
import { TicketService } from '../../core/services/ticket.service';
import { TicketListComponent } from './ticket-list.component';

describe('TicketListComponent', () => {
  let fixture: ComponentFixture<TicketListComponent>;
  let component: TicketListComponent;
  let ticketServiceSpy: jasmine.SpyObj<TicketService>;

  beforeEach(async () => {
    ticketServiceSpy = jasmine.createSpyObj('TicketService', ['getTickets']);
    ticketServiceSpy.getTickets.and.returnValue(of({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 }));

    const agentServiceSpy = jasmine.createSpyObj('AgentService', ['getAgents']);
    agentServiceSpy.getAgents.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [TicketListComponent],
      providers: [
        provideRouter([]),
        { provide: TicketService, useValue: ticketServiceSpy },
        { provide: AgentService, useValue: agentServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TicketListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // runs ngOnInit -> the initial load()
  });

  it('loads tickets from the server on init', () => {
    expect(ticketServiceSpy.getTickets).toHaveBeenCalledTimes(1);
  });

  it('debounces the search input and only queries once typing settles, with the final term', fakeAsync(() => {
    ticketServiceSpy.getTickets.calls.reset();

    component.searchControl.setValue('inv');
    tick(100);
    component.searchControl.setValue('invo');
    tick(100);
    component.searchControl.setValue('invoice');

    tick(299);
    expect(ticketServiceSpy.getTickets).not.toHaveBeenCalled();

    tick(1);
    expect(ticketServiceSpy.getTickets).toHaveBeenCalledTimes(1);
    expect(ticketServiceSpy.getTickets.calls.mostRecent().args[0].search).toBe('invoice');
  }));

  it('resets pagination to page 1 whenever a filter changes', () => {
    component.page = 3;
    component.statusFilter = 'InProgress';

    component.onFilterChange();

    expect(component.page).toBe(1);
    expect(ticketServiceSpy.getTickets.calls.mostRecent().args[0].status).toBe('InProgress');
  });
});
