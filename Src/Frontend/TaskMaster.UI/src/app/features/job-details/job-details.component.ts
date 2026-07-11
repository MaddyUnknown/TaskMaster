import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { NavigationService } from '../../core/services/navigation.service';
import { ContentComponent } from '../../shared/layout/content/content.component';
import { CardComponent } from '../../shared/components/card/card.component';
import {
  BadgeComponent,
  BadgeVariant,
} from '../../shared/components/badge/badge.component';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { Job, JobStatus } from '../../core/models';

@Component({
  selector: 'app-job-details',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    ContentComponent,
    CardComponent,
    BadgeComponent,
    LoadingComponent,
  ],
  templateUrl: './job-details.component.html',
  styleUrl: './job-details.component.css',
})
export class JobDetailsComponent implements OnInit {
  nav = inject(NavigationService);

  job: Job | undefined = undefined;
  notFound = false;
  JobStatus = JobStatus;

  constructor(
    private route: ActivatedRoute,
    private api: ApiService,
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.api.getJob(id).subscribe((j) => {
        if (j) {
          this.job = j;
        } else {
          this.notFound = true;
        }
      });
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

  labelText(s: JobStatus): string {
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
}
