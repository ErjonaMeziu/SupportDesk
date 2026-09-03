import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  template: `
    <div class="modal-backdrop" (click)="cancel.emit()">
      <div class="modal-card" (click)="$event.stopPropagation()">
        <h2>{{ title }}</h2>
        <p>{{ message }}</p>
        <div class="modal-actions">
          <button type="button" class="btn btn-secondary" (click)="cancel.emit()">Cancel</button>
          <button type="button" class="btn btn-danger" (click)="confirm.emit()">{{ confirmLabel }}</button>
        </div>
      </div>
    </div>
  `
})
export class ConfirmDialogComponent {
  @Input() title = 'Are you sure?';
  @Input() message = 'This action cannot be undone.';
  @Input() confirmLabel = 'Confirm';
  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
}
