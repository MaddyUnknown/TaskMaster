import { Component, OnInit, OnDestroy, Inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { LucidePlus } from '@lucide/angular';
import { ApiService } from '../../core/services/api.service';
import { ContentComponent } from '../../shared/layout/content/content.component';
import {
  BadgeComponent,
  BadgeVariant,
} from '../../shared/components/badge/badge.component';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';
import { Job, JobStatus, CreateJobRequest, JobCounts } from '../../core/models';
import { CreateJobDialogComponent } from './create-job-dialog/create-job-dialog.component';
import { AUTH_SERVICE, AuthService } from '../../core/auth/auth.service';
import { UserPermission } from '../../core/auth/auth.models';

@Component({
  selector: 'app-jobs',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    ContentComponent,
    BadgeComponent,
    LoadingComponent,
    EmptyStateComponent,
    ButtonComponent,
    PaginationComponent,
    CreateJobDialogComponent,
    LucidePlus,
  ],
  templateUrl: './jobs.component.html',
  styleUrl: './jobs.component.css',
})
export class JobsComponent implements OnInit, OnDestroy {
  jobs: Job[] = [];
  counts: JobCounts = {
    queued: 0,
    inProgress: 0,
    completed: 0,
    failed: 0,
    total: 0,
  };
  loading = true;
  activeFilter = 'all';
  page = 1;
  pageSize = 20;
  totalCount = 0;
  totalPages = 0;
  showCreateDialog = false;
  submitting = false;
  canCreateJobs = false;

  private destroy$ = new Subject<void>();

  constructor(
    private api: ApiService,
    @Inject(AUTH_SERVICE) private authService: AuthService,
  ) {}

  get filterTabs() {
    return [
      { label: 'All', value: 'all', count: this.counts.total },
      { label: 'Queued', value: 'queued', count: this.counts.queued },
      {
        label: 'In Progress',
        value: 'inProgress',
        count: this.counts.inProgress,
      },
      { label: 'Completed', value: 'completed', count: this.counts.completed },
      { label: 'Failed', value: 'failed', count: this.counts.failed },
    ];
  }

  ngOnInit() {
    this.authService.permissions$
      .pipe(takeUntil(this.destroy$))
      .subscribe(
        () =>
          (this.canCreateJobs = this.authService.hasPermission(
            UserPermission.createJob,
          )),
      );
    this.loadJobs();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadJobs() {
    const status =
      this.activeFilter === 'all'
        ? undefined
        : this.mapFilterToStatus(this.activeFilter);
    this.api
      .getJobsWithCounts({ page: this.page, pageSize: this.pageSize, status })
      .pipe(takeUntil(this.destroy$))
      .subscribe(({ page, counts }) => {
        this.jobs = page.items;
        this.totalCount = page.totalCount;
        this.totalPages = page.totalPages;
        this.counts = counts;
        this.loading = false;
      });
  }

  private mapFilterToStatus(filter: string): JobStatus | undefined {
    const map: Record<string, JobStatus> = {
      queued: JobStatus.Queued,
      inProgress: JobStatus.InProgress,
      completed: JobStatus.Completed,
      failed: JobStatus.Failed,
    };
    return map[filter];
  }

  setFilter(value: string) {
    this.activeFilter = value;
    this.page = 1;
    this.loading = true;
    this.loadJobs();
  }

  onPageChange(page: number) {
    this.page = page;
    this.loading = true;
    this.loadJobs();
  }

  openCreateDialog() {
    this.showCreateDialog = true;
  }

  closeCreateDialog() {
    this.showCreateDialog = false;
  }

  createJob(req: CreateJobRequest) {
    this.submitting = true;
    this.api.createJob(req).subscribe({
      next: () => {
        this.submitting = false;
        this.showCreateDialog = false;
        this.loading = true;
        this.loadJobs();
      },
      error: () => {
        this.submitting = false;
      },
    });
  }

  statusLabel(s: JobStatus): string {
    switch (s) {
      case JobStatus.Completed:
        return 'Completed';
      case JobStatus.InProgress:
        return 'In Progress';
      case JobStatus.Queued:
        return 'Queued';
      case JobStatus.Failed:
        return 'Failed';
      default:
        return '';
    }
  }

  statusVariant(s: JobStatus): BadgeVariant {
    switch (s) {
      case JobStatus.Completed:
        return 'success';
      case JobStatus.InProgress:
        return 'running';
      case JobStatus.Queued:
        return 'queued';
      case JobStatus.Failed:
        return 'error';
      default:
        return 'default';
    }
  }
}
