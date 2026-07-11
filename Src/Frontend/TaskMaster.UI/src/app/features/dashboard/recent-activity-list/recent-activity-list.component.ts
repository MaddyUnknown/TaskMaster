import { Component, Input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { CardComponent } from '../../../shared/components/card/card.component';
import { DashboardActivity } from '../../../core/models';

@Component({
  selector: 'app-recent-activity-list',
  standalone: true,
  imports: [DatePipe, CardComponent],
  templateUrl: './recent-activity-list.component.html',
  styleUrl: './recent-activity-list.component.css',
})
export class RecentActivityListComponent {
  @Input() activities: DashboardActivity[] = [];
  @Input() loading = false;
}
