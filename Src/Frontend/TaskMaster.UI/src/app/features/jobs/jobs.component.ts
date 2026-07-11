import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
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
import { Job, JobStatus, CreateJobRequest } from '../../core/models';
import { CreateJobDialogComponent } from './create-job-dialog/create-job-dialog.component';

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
    CreateJobDialogComponent,
    LucidePlus,
  ],
  templateUrl: './jobs.component.html',
  styleUrl: './jobs.component.css',
})
export class JobsComponent implements OnInit {
  jobs: Job[] = [];
  loading = true;
  activeFilter = 'all';
  showCreateDialog = false;
  submitting = false;

  constructor(private api: ApiService) {}

  get filterTabs() {
    return [
      { label: 'All', value: 'all', count: this.jobs.length },
      {
        label: 'Queued',
        value: 'queued',
        count: this.jobs.filter((x) => x.status === JobStatus.Queued).length,
      },
      {
        label: 'In Progress',
        value: 'inProgress',
        count: this.jobs.filter((x) => x.status === JobStatus.InProgress)
          .length,
      },
      {
        label: 'Completed',
        value: 'completed',
        count: this.jobs.filter((x) => x.status === JobStatus.Completed).length,
      },
      {
        label: 'Failed',
        value: 'failed',
        count: this.jobs.filter((x) => x.status === JobStatus.Failed).length,
      },
    ];
  }

  get filteredJobs(): Job[] {
    if (this.activeFilter === 'all') return this.jobs;
    const map: Record<string, JobStatus> = {
      queued: JobStatus.Queued,
      inProgress: JobStatus.InProgress,
      completed: JobStatus.Completed,
      failed: JobStatus.Failed,
    };
    return this.jobs.filter((x) => x.status === map[this.activeFilter]);
  }

  ngOnInit() {
    this.loadJobs();
  }

  private loadJobs() {
    this.api.getJobs().subscribe((j) => {
      this.jobs = j;
      this.loading = false;
    });
  }

  setFilter(value: string) {
    this.activeFilter = value;
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
