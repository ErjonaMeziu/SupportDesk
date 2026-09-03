import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('creates the app shell', () => {
    const fixture = TestBed.createComponent(AppComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders the Support Desk brand link to the ticket list', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const link = fixture.nativeElement.querySelector('a.brand') as HTMLAnchorElement;

    expect(link.textContent).toContain('Support Desk');
  });
});
