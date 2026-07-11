import { Component, Input, Output, EventEmitter } from '@angular/core';
import { LucideX } from '@lucide/angular';

@Component({
  selector: 'app-dialog',
  standalone: true,
  imports: [LucideX],
  templateUrl: './dialog.component.html',
  styleUrl: './dialog.component.css',
})
export class DialogComponent {
  @Input() open = false;
  @Input() title = '';
  @Input() maxWidth = '48rem';
  @Input() showFooter = false;
  @Output() close = new EventEmitter<void>();
}
