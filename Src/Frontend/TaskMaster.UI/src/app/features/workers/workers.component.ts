import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { ContentComponent } from '../../shared/layout/content/content.component';
import {
  BadgeComponent,
  BadgeVariant,
} from '../../shared/components/badge/badge.component';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { Worker, WorkerStatus } from '../../core/models';

@Component({
  selector: 'app-workers',
  standalone: true,
  imports: [
    RouterLink,
    ContentComponent,
    BadgeComponent,
    LoadingComponent,
    EmptyStateComponent,
  ],
  templateUrl: './workers.component.html',
  styleUrl: './workers.component.css',
})
export class WorkersComponent implements OnInit {
  workers: Worker[] = [];
  loading = true;
  activeFilter = 'all';

  constructor(private api: ApiService) {}

  get filterTabs() {
    return [
      { label: 'All', value: 'all', count: this.workers.length },
      {
        label: 'Active',
        value: 'active',
        count: this.workers.filter((w) => w.status === WorkerStatus.Active)
          .length,
      },
      {
        label: 'Inactive',
        value: 'inactive',
        count: this.workers.filter((w) => w.status === WorkerStatus.Inactive)
          .length,
      },
    ];
  }

  get filteredWorkers(): Worker[] {
    if (this.activeFilter === 'all') return this.workers;
    const map: Record<string, WorkerStatus> = {
      active: WorkerStatus.Active,
      inactive: WorkerStatus.Inactive,
    };
    return this.workers.filter((w) => w.status === map[this.activeFilter]);
  }

  ngOnInit() {
    this.api.getWorkers().subscribe((w) => {
      this.workers = w;
      this.loading = false;
    });
  }

  setFilter(value: string) {
    this.activeFilter = value;
  }

  statusVariant(s: WorkerStatus): BadgeVariant {
    return s === WorkerStatus.Active ? 'active' : 'inactive';
  }

  statusLabel(s: WorkerStatus): string {
    return s === WorkerStatus.Active ? 'Active' : 'Inactive';
  }

  heartbeatLabel(w: Worker): string {
    return w.lastHeartbeatTimestamp
      ? new Date(w.lastHeartbeatTimestamp).toLocaleDateString('en-US', {
          month: 'short',
          day: 'numeric',
          hour: 'numeric',
          minute: '2-digit',
        })
      : 'Never';
  }
}
