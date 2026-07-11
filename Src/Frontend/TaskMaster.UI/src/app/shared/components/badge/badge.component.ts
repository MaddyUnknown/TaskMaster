import { Component, Input } from '@angular/core';

export type BadgeVariant =
  | 'default'
  | 'primary'
  | 'success'
  | 'warning'
  | 'error'
  | 'info'
  | 'running'
  | 'queued'
  | 'active'
  | 'inactive';

@Component({
  selector: 'app-badge',
  standalone: true,
  templateUrl: './badge.component.html',
  styleUrl: './badge.component.css',
})
export class BadgeComponent {
  @Input() variant: BadgeVariant = 'default';
  @Input() dot = false;
  @Input() pill = true;
}
