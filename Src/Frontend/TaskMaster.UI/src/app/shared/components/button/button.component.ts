import { Component, Input } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost';
export type ButtonSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'button[app-button]',
  standalone: true,
  templateUrl: './button.component.html',
  styleUrl: './button.component.css',
  host: {
    '[class.btn-primary]': 'variant === "primary"',
    '[class.btn-secondary]': 'variant === "secondary"',
    '[class.btn-danger]': 'variant === "danger"',
    '[class.btn-ghost]': 'variant === "ghost"',
    '[class.btn-sm]': 'size === "sm"',
    '[class.btn-md]': 'size === "md"',
    '[class.btn-lg]': 'size === "lg"',
  },
})
export class ButtonComponent {
  @Input() variant: ButtonVariant = 'primary';
  @Input() size: ButtonSize = 'md';
  @Input() loading = false;
}
