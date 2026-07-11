import { Component, Input } from '@angular/core';
import { BreadcrumbComponent, BreadcrumbItem } from '../../components/breadcrumb/breadcrumb.component';

@Component({
  selector: 'app-content',
  standalone: true,
  imports: [BreadcrumbComponent],
  templateUrl: './content.component.html',
  styleUrl: './content.component.css'
})
export class ContentComponent {
  @Input() title?: string;
  @Input() description?: string;
  @Input() breadcrumbs: BreadcrumbItem[] = [];
}
