import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-loading',
  standalone: true,
  templateUrl: './loading.component.html',
  styleUrl: './loading.component.css',
})
export class LoadingComponent {
  @Input() type: 'spinner' | 'dots' | 'pulse' = 'spinner';
  @Input() size = '3.2rem';
  @Input() text?: string;
  pulseBars = ['100%', '85%', '70%', '90%', '60%'];
}
