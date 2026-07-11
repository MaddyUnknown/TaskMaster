import { Component } from '@angular/core';
import { LucideInfo } from '@lucide/angular';
import { DialogComponent } from '../../components/dialog/dialog.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [LucideInfo, DialogComponent],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent {
  showSystemInfo = false;
  constructor() {}
}
