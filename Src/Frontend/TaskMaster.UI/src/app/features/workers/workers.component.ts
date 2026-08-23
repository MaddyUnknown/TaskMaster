import { Component, OnInit, OnDestroy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { ContentComponent } from '../../shared/layout/content/content.component';
import {
  BadgeComponent,
  BadgeVariant,
} from '../../shared/components/badge/badge.component';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';
import { Worker, WorkerStatus, WorkerCounts } from '../../core/models';

@Component({
  selector: 'app-workers',
  standalone: true,
  imports: [
    RouterLink,
    ContentComponent,
    BadgeComponent,
    LoadingComponent,
    EmptyStateComponent,
    PaginationComponent,
  ],
  templateUrl: './workers.component.html',
  styleUrl: './workers.component.css',
})
export class WorkersComponent implements OnInit, OnDestroy {
  workers: Worker[] = [];
  counts: WorkerCounts = { active: 0, inActive: 0, total: 0 };
  loading = true;
  activeFilter = 'all';
  page = 1;
  pageSize = 20;
  totalCount = 0;
  totalPages = 0;

  private destroy$ = new Subject<void>();

  constructor(private api: ApiService) {}

  get filterTabs() {
    return [
      { label: 'All', value: 'all', count: this.counts.total },
      { label: 'Active', value: 'active', count: this.counts.active },
      { label: 'Inactive', value: 'inactive', count: this.counts.inActive },
    ];
  }

  ngOnInit() {
    this.loadWorkers();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadWorkers() {
    const status =
      this.activeFilter === 'all'
        ? undefined
        : this.mapFilterToStatus(this.activeFilter);
    this.api
      .getWorkersWithCounts({
        page: this.page,
        pageSize: this.pageSize,
        status,
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe(({ page, counts }) => {
        this.workers = page.items;
        this.totalCount = page.totalCount;
        this.totalPages = page.totalPages;
        this.counts = counts;
        this.loading = false;
      });
  }

  private mapFilterToStatus(filter: string): WorkerStatus | undefined {
    const map: Record<string, WorkerStatus> = {
      active: WorkerStatus.Active,
      inactive: WorkerStatus.Inactive,
    };
    return map[filter];
  }

  setFilter(value: string) {
    this.activeFilter = value;
    this.page = 1;
    this.loading = true;
    this.loadWorkers();
  }

  onPageChange(page: number) {
    this.page = page;
    this.loading = true;
    this.loadWorkers();
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
